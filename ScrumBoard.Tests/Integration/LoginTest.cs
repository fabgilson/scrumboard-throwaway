using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.Rendering;
using Bunit.TestDoubles;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Integration.Infrastructure;
using SharedLensResources;
using SharedLensResources.Authentication;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration;

/// <summary>
/// Make a local class that inherits Login so we can access its protected fields
/// </summary>
internal class LoginUnderTest : Login
{
    public LoginUnderTest(
        IAuthenticationService authenticationService, 
        ILogger<LoginUnderTest> logger, 
        IUserRepository userRepository,
        NavigationManager navigationManager,
        IScrumBoardStateStorageService stateStorageService,
        ISeedDataService seedDataService,
        IConfigurationService configurationService,
        IUsageDataService usageDataService
    ) {
        AuthenticationService = authenticationService;
        Logger = logger;
        UserRepository = userRepository;
        NavigationManager = navigationManager;
        StateStorageService = stateStorageService;
        SeedDataService = seedDataService;
        ConfigurationService = configurationService;
        UsageDataService = usageDataService;
    }

    public void SetUsername(string username)
    {
        LoginForm.Username = username;
    }

    public void SetPassword(string password)
    {
        LoginForm.Password = password;
    }

    public async Task TriggerLoginAttempt()
    {
        await AttemptLogin();
    }
}

public class LoginTest : BaseIntegrationTestFixture
{
    private const string ValidUserName = "GoodUser";
    private const string ValidPassword = "GoodPassword";
    private const string FirstNameOfValidUser = "Good";
    private const string LastNameOfValidUser = "Gooderson";
    private const long IdOfValidUser = 10;
    private const string EmailOfValidUser = "gug123@uclive.ac.nz";

    private const string InvalidUserName = "BadUser";
    private const string InvalidPassword = "BadPassword";

    // Login object under test
    private readonly LoginUnderTest _loginUnderTest;
    private readonly IDbContextFactory<DatabaseContext> _databaseContextFactory;
    private readonly IDbContextFactory<UsageDataDbContext> _usageDataDbContextFactory;

    public LoginTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _loginUnderTest = ActivatorUtilities.CreateInstance<LoginUnderTest>(ServiceProvider);
        _databaseContextFactory = GetDbContextFactory();
        _usageDataDbContextFactory = GetUsageDataDbContextFactory();
    }

    protected override void ConfigureScopedServices(IServiceCollection services)
    {
        var authenticationServiceMock = new Mock<IAuthenticationService>();
        authenticationServiceMock
            .Setup(mock => mock.AttemptLogin(It.Is<LensAuthenticateRequest>(r => r.Username == ValidUserName && r.Password == ValidPassword)))
            .ReturnsAsync(new LensAuthenticationReply { 
                Success = true, 
                UserResponse = new UserResponse {
                    FirstName = FirstNameOfValidUser, 
                    LastName = LastNameOfValidUser, 
                    Id = IdOfValidUser, 
                    Email = EmailOfValidUser,
                    Created = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
                }
            });
        authenticationServiceMock
            .Setup(mock => mock.AttemptLogin(It.Is<LensAuthenticateRequest>(r => r.Username == InvalidUserName || r.Password == InvalidPassword)))
            .ReturnsAsync(new LensAuthenticationReply { Success = false, Message = "Failed to login" });
        services.AddScoped(typeof(IAuthenticationService), _ => authenticationServiceMock.Object);
        services.AddScoped<NavigationManager>(_ => new FakeNavigationManager(new TestRenderer(new RenderedComponentActivator(ServiceProvider), new TestServiceProvider(), new LoggerFactory())));
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        // Remove the default user
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task UserAttemptsToLogIn_InvalidCredentials_NoUserAddedToDatabase()
    {
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            context.Users.Any().Should().BeFalse();
        }            

        _loginUnderTest.SetUsername(InvalidUserName);
        _loginUnderTest.SetPassword(InvalidPassword);
        // We expect it will throw invalid operation exception, as StateHasChanged is called, but no renderer is in use
        await _loginUnderTest.Invoking(async lut => await lut.TriggerLoginAttempt()).Should().ThrowAsync<InvalidOperationException>();
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            context.Users.Any().Should().BeFalse();
        }
    }

    [Fact]
    public async Task UserAttemptsToLogIn_LoginSucceedsAndIsFirstTimeLogin_AddsUserToDatabase()
    {
        // Ensure that there are no users to begin with
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            context.Users.Any().Should().BeFalse();
        }

        _loginUnderTest.SetUsername(ValidUserName);
        _loginUnderTest.SetPassword(ValidPassword);
        await _loginUnderTest.TriggerLoginAttempt();
        await WaitForUsageEventToAppear(await _usageDataDbContextFactory.CreateDbContextAsync(), 1);

        // Assert there is now a user, and that it is the user that just logged in
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            var matchingUsers = await context.Users.Where(x => x.FirstName == FirstNameOfValidUser && x.LastName == LastNameOfValidUser).ToListAsync();
            matchingUsers.Should().HaveCount(1);
            matchingUsers.First().Id.Should().Be(IdOfValidUser);
        }
    }

    [Fact]
    public async Task UserAttemptsToLogIn_LoginSucceedsButHasAlreadyLoggedInPreviously_UserUpdatedInDatabase()
    {
        // Ensure that a user already exists in the database, as if they had logged in previously
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User() { 
                Id = IdOfValidUser, 
                FirstName = "Initial First Name", 
                LastName = "Initial Last Name",
                Email = "initialEmail@example.com",
                LDAPUsername = "user123"
            });
            await context.SaveChangesAsync();

            context.Users.Count(x => x.Id == IdOfValidUser).Should().Be(1);
        }

        _loginUnderTest.SetUsername(ValidUserName);
        _loginUnderTest.SetPassword(ValidPassword);
        await _loginUnderTest.TriggerLoginAttempt();
        await WaitForUsageEventToAppear(await _usageDataDbContextFactory.CreateDbContextAsync(), 1);

        // Assert there is still only 1 user, and that it is the user we expect (with fields update from loginAttempt)
        await using var dbContext = await _databaseContextFactory.CreateDbContextAsync();            
        var matchingUsers = await dbContext.Users.Where(x => x.Id == IdOfValidUser).ToListAsync();            
        matchingUsers.Should().HaveCount(1);
        matchingUsers.First().Id.Should().Be(IdOfValidUser);
        matchingUsers.First().FirstName.Should().Be(FirstNameOfValidUser);
        matchingUsers.First().LastName.Should().Be(LastNameOfValidUser);
        matchingUsers.First().Email.Should().Be(EmailOfValidUser);
    }

    [Fact]
    public async Task UserAttemptsToLogIn_LoginSucceeds_UsageEventAddedToDatabase()
    {
        await using var context = await _usageDataDbContextFactory.CreateDbContextAsync();
        (await context.UsageDataEvents.ToListAsync()).Should().BeEmpty();

        _loginUnderTest.SetUsername(ValidUserName);
        _loginUnderTest.SetPassword(ValidPassword);
        await _loginUnderTest.TriggerLoginAttempt();

        await WaitForUsageEventToAppear(context, 1);
        context.UsageDataEvents.First().Should().Match<AuthenticationUsageEvent>(e => 
            (e.Occurred > DateTime.Now.AddSeconds(-30)) &&  // Give 30 seconds of grace for time, race conditions are bad
            (e.UserId == IdOfValidUser) &&
            (e.AuthenticationEventType == AuthenticationUsageEventType.LogIn)
        );
    }
        
    [Fact]
    public async Task UserAttemptsToLogIn_LoginFails_NoUsageEventsAreAddedToDatabase()
    {
        await using var context = await _usageDataDbContextFactory.CreateDbContextAsync();
        context.UsageDataEvents.Should().BeEmpty();

        _loginUnderTest.SetUsername(ValidUserName);
        _loginUnderTest.SetPassword(InvalidPassword);

        // We expect it will throw invalid operation exception, as StateHasChanged is called, but no renderer is in use
        await _loginUnderTest.Invoking(async lut => await lut.TriggerLoginAttempt()).Should().ThrowAsync<InvalidOperationException>();

        // Give some time for fire-and-forget method to save (if it was going to, incorrectly...)
        await WaitForUsageEventToAppear(context, 0);

        context.UsageDataEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Forces the application to wait until a fire-and-forget usage event has been created.
    /// This prevents race conditions causing tests to fail on speedy computers, where the database is modified
    /// between test runs - runs that expect the DB to be empty at the start of each test
    /// </summary>
    /// <param name="context">Usage data DB context</param>
    /// <param name="expectedCount">Expected number of usage events to find, if set to zero, enforces full waiting period</param>
    private static async Task WaitForUsageEventToAppear(UsageDataDbContext context, int expectedCount)
    {
        // Give some time for fire-and-forget method to save
        var timeoutCounter = 0;
        var maxAttempts = 5;
        while ((expectedCount == 0 || await context.UsageDataEvents.CountAsync() != expectedCount) && timeoutCounter < maxAttempts)
        {
            await Task.Delay(50);
            timeoutCounter++;
        }

        context.UsageDataEvents.Should().HaveCount(expectedCount);
    }
}