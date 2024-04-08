using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Unit.Utils;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.LiveUpdating;

[Collection(LiveUpdateIsolationCollection.CollectionName)]
public class EntityUpdateHubTest : BaseIntegrationTestFixture
{
    private HubConnection _hubConnection;
    private User _userNotInProject;
    
    // Tell the base class not to start a default update connection, we're testing it so we'll do some more complex stuff here instead
    public EntityUpdateHubTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    { }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        var project = FakeDataGenerator.CreateFakeProject(projectId: LiveUpdateConnectionProjectId, developers: [DefaultUser]);
        await dbContext.AddAsync(project);

        _userNotInProject = FakeDataGenerator.CreateFakeUser();
        await dbContext.AddAsync(_userNotInProject);
        await dbContext.SaveChangesAsync();
    }

    private void ConfigureAuthenticationResult(User loggedInUser)
    {
        AuthenticationServiceMock
            .Setup(x => x.GetClaimsIdentityForBearerTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(loggedInUser is null 
                ? new ClaimsIdentity()
                : new ClaimsIdentity(
                    new[] { new Claim(JwtRegisteredClaimNames.NameId, loggedInUser.Id.ToString()) }, 
                    "Bearer", 
                    ClaimsIdentity.DefaultNameClaimType, 
                    ClaimsIdentity.DefaultRoleClaimType
                )
            );
    }

    public override async Task DisposeAsync()
    {
        if(_hubConnection is null) return;
        await _hubConnection.StopAsync();
        await _hubConnection.DisposeAsync();
        await base.DisposeAsync();
    }

    private async Task<LiveUpdateEventInvocation> GetConnectionErrorInvocation()
    {
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.ConnectionError);
        return LiveUpdateEventInvocations.Should()
            .ContainSingle(x => x.EventType == LiveUpdateEventType.ConnectionError)
            .Which;
    }
    
    private async Task<LiveUpdateEventInvocation> GetConnectionSuccessInvocation()
    {
        await WaitForLiveUpdateInvocationsToNotBeEmpty();
        return LiveUpdateEventInvocations.Should()
            .ContainSingle(x => x.EventType == LiveUpdateEventType.ConnectionSuccess)
            .Which;
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_NoProjectIdGiven_ConnectionAbortedWithErrorMessage()
    {
        _hubConnection = CreateTestHubConnection(projectIdString: "");
        
        await _hubConnection.StartAsync();

        (await GetConnectionErrorInvocation()).ConnectionErrorText.Should().Be("No valid project ID given");
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Disconnected));
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_ProjectIdIsNonNumeric_ConnectionAbortedWithErrorMessage()
    {
        _hubConnection = CreateTestHubConnection(projectIdString: "not-a-number");
        
        await _hubConnection.StartAsync();

        (await GetConnectionErrorInvocation()).ConnectionErrorText.Should().Be("No valid project ID given");
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Disconnected));
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_NoBearerTokenGiven_ConnectionAbortedWithErrorMessage()
    {
        _hubConnection = CreateTestHubConnection(skipBearerToken: true);
        
        await _hubConnection.StartAsync();

        (await GetConnectionErrorInvocation()).ConnectionErrorText.Should().Be("No bearer token given");
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Disconnected));
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_InvalidBearerTokenGiven_ConnectionAbortedWithErrorMessage()
    {
        ConfigureAuthenticationResult(null);
        _hubConnection = CreateTestHubConnection();
        
        await _hubConnection.StartAsync();

        (await GetConnectionErrorInvocation()).ConnectionErrorText.Should().Be("Authentication failed");
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Disconnected));
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_UserNotPartOfProject_ConnectionAbortedWithErrorMessage()
    {
        ConfigureAuthenticationResult(_userNotInProject);
        _hubConnection = CreateTestHubConnection();
        
        await _hubConnection.StartAsync();

        (await GetConnectionErrorInvocation()).ConnectionErrorText.Should().Be("User is not authorized to connect to given project");
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Disconnected));
    }
    
    [Fact]
    public async Task LiveUpdateConnectionCreated_UserBelongsToProject_ConnectionSucceeded()
    {
        ConfigureAuthenticationResult(DefaultUser);
        _hubConnection = CreateTestHubConnection();
        
        await _hubConnection.StartAsync();

        var action = async () => await GetConnectionSuccessInvocation();
        await action.Should().NotThrowAsync();
        AssertionHelper.WaitFor(() => _hubConnection.State.Should().Be(HubConnectionState.Connected));
    }
}