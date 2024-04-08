using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Infrastructure;

/// <summary>
/// Extensions of <see cref="BaseIntegrationTestFixture"/> where the server is already set up to have multiple hub connections
/// quickly made to multiple users. This class is useful for tests where we want to not only check that some live update
/// functionality occurs, but where we also want to check that it is not being sent to the wrong connections.
///
/// Note: The default LiveUpdate connection <see cref="BaseIntegrationTestFixture._liveUpdateConnection"/> is not created
/// by this class, and never will be.
/// </summary>
/// <param name="factory">TestWebAppFactory injected from DI</param>
/// <param name="testOutputHelper">TestOutputHelper injected from DI</param>
public abstract class MultipleHubConnectionBaseIntegrationTextFixture(
    TestWebApplicationFactory factory,
    ITestOutputHelper testOutputHelper
) : BaseIntegrationTestFixture(factory, testOutputHelper)
{
    protected readonly Project ProjectA = FakeDataGenerator.CreateFakeProject();
    protected readonly User UserInProjectA = FakeDataGenerator.CreateFakeUser();
    private const string UserInProjectABearerToken = "UserInProjectABearerToken";
    private HubConnection _userInProjectAHubConnection;
    protected const string UserInProjectAConnectionId = "UserInProjectAConnectionId";

    protected readonly Project ProjectB = FakeDataGenerator.CreateFakeProject();
    
    protected readonly User User1InProjectB = FakeDataGenerator.CreateFakeUser();
    private const string User1InProjectBBearerToken = "User1InProjectBBearerToken";
    private HubConnection _user1InProjectBHubConnection;
    protected const string User1InProjectBConnectionId = "User1InProjectBConnectionId";
    
    protected readonly User User2InProjectB = FakeDataGenerator.CreateFakeUser();
    private const string User2InProjectBBearerToken = "User2InProjectBBearerToken";
    private HubConnection _user2InProjectBHubConnection;
    protected const string User2InProjectBConnectionId = "User2InProjectBConnectionId";
    
    protected IEnumerable<LiveUpdateEventInvocation> InvocationsReceivedByUserInProjectA => LiveUpdateEventInvocations
        .Where(x => x.ConnectionId == UserInProjectAConnectionId);

    protected IEnumerable<LiveUpdateEventInvocation> InvocationsReceivedByUser1InProjectB => LiveUpdateEventInvocations
        .Where(x => x.ConnectionId == User1InProjectBConnectionId);
    
    protected IEnumerable<LiveUpdateEventInvocation> InvocationsReceivedByUser2InProjectB => LiveUpdateEventInvocations
        .Where(x => x.ConnectionId == User2InProjectBConnectionId);
    
    protected async Task CreateAndStartHubConnections()
    {
        // Replace base authentication service mock behaviour to return correct user based on bearer token
        AuthenticationServiceMock.Reset();
        AuthenticationServiceMock.Setup(x => x.GetClaimsIdentityForBearerTokenAsync(UserInProjectABearerToken))
            .ReturnsAsync(new ClaimsIdentity(
                    new[] { new Claim(JwtRegisteredClaimNames.NameId, UserInProjectA.Id.ToString()) }, 
                    "Bearer", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType
                )
            );
        AuthenticationServiceMock.Setup(x => x.GetClaimsIdentityForBearerTokenAsync(User1InProjectBBearerToken))
            .ReturnsAsync(new ClaimsIdentity(
                    new[] { new Claim(JwtRegisteredClaimNames.NameId, User1InProjectB.Id.ToString()) }, 
                    "Bearer", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType
                )
            );
        AuthenticationServiceMock.Setup(x => x.GetClaimsIdentityForBearerTokenAsync(User2InProjectBBearerToken))
            .ReturnsAsync(new ClaimsIdentity(
                    new[] { new Claim(JwtRegisteredClaimNames.NameId, User2InProjectB.Id.ToString()) }, 
                    "Bearer", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType
                )
            );
        
        _userInProjectAHubConnection = CreateTestHubConnection(
            projectIdString: ProjectA.Id.ToString(),
            bearerToken: UserInProjectABearerToken,
            connectionId: UserInProjectAConnectionId
        );
        _user1InProjectBHubConnection = CreateTestHubConnection(
            projectIdString: ProjectB.Id.ToString(),
            bearerToken: User1InProjectBBearerToken,
            connectionId: User1InProjectBConnectionId
        );
        _user2InProjectBHubConnection = CreateTestHubConnection(
            projectIdString: ProjectB.Id.ToString(),
            bearerToken: User2InProjectBBearerToken,
            connectionId: User2InProjectBConnectionId
        );

        // Start connections and wait until all three receive connection success notification
        await _userInProjectAHubConnection.StartAsync();
        await _user1InProjectBHubConnection.StartAsync();
        await _user2InProjectBHubConnection.StartAsync();

        await WaitForLiveUpdateInvocationsAssertionToPass(invocations =>
            invocations.Any(x => x.ConnectionId == UserInProjectAConnectionId && x.EventType == LiveUpdateEventType.ConnectionSuccess)
            && invocations.Any(x => x.ConnectionId == User1InProjectBConnectionId && x.EventType == LiveUpdateEventType.ConnectionSuccess)
            && invocations.Any(x => x.ConnectionId == User2InProjectBConnectionId && x.EventType == LiveUpdateEventType.ConnectionSuccess)
        );
    }

    public override async Task DisposeAsync()
    {
        foreach (var connection in new[] { _userInProjectAHubConnection, _user1InProjectBHubConnection, _user2InProjectBHubConnection })
        {
            if (connection is null) continue;
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
        await base.DisposeAsync();
    }
    
    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        await dbContext.AddRangeAsync(UserInProjectA, User1InProjectB, User2InProjectB, ProjectA, ProjectB);
        await dbContext.AddRangeAsync(
            new ProjectUserMembership { ProjectId = ProjectA.Id, UserId = UserInProjectA.Id, Role = ProjectRole.Developer },
            new ProjectUserMembership { ProjectId = ProjectB.Id, UserId = User1InProjectB.Id, Role = ProjectRole.Developer },
            new ProjectUserMembership { ProjectId = ProjectB.Id, UserId = User2InProjectB.Id, Role = ProjectRole.Developer }
        );

        await dbContext.SaveChangesAsync();
    }
}