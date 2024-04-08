using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Tests.Integration.LiveUpdating;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using ScrumBoard.Utils;
using SharedLensResources.Authentication;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Infrastructure;

public abstract class BaseIntegrationTestFixture: IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    /// <summary>
    /// The ProjectId to which the live update connection is registered to listen. All tests should ensure that a project with this ID
    /// is the owner of any entities being live updated, if they wish the test hub connection to receive the updates.
    /// </summary>
    protected readonly long LiveUpdateConnectionProjectId;
    
    /// <summary>
    /// Service provider that is exposed to inheriting classes so that they (test classes) may
    /// request services via dependency injection as required.
    /// </summary>
    private readonly TestWebApplicationFactory _factory;
    
    protected IServiceProvider ServiceProvider { get; set; }
    private TestServer TestServer { get; set; }
    protected Mock<IAuthenticationService> AuthenticationServiceMock { get; }

    protected Mock<IScrumBoardStateStorageService> ScrumBoardStateStorageServiceMock { get; }
    protected Mock<IJsInteropService> JsInteropServiceMock { get; }
    protected Mock<IClock> ClockMock { get; }

    protected IDbContextFactory<DatabaseContext> GetDbContextFactory() => ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
    protected IDbContextFactory<UsageDataDbContext> GetUsageDataDbContextFactory() => ServiceProvider.GetRequiredService<IDbContextFactory<UsageDataDbContext>>();

    protected HttpClient HttpClient { get; }
    protected User DefaultUser { get; private set; }
    private HubConnection _liveUpdateConnection;
    private readonly bool _startLiveUpdateConnection;
    protected ConcurrentBag<LiveUpdateEventInvocation> LiveUpdateEventInvocations { get; } = [];
    
    /// <summary>
    /// DB Connection that is managed by this underlying base class to ensure that the SQLite in-memory DB persists until the end of a test run.
    /// An SQLite DB will automatically dispose when all connections are closed.
    /// </summary>
    private readonly DbConnection _dbConnection;
    private readonly DbConnection _usageDataDbConnection;

    /// <summary>
    /// Base class for all integration tests. Automatically configures databases, service providers, etc, in addition to providing a number
    /// of helpful methods for inheriting test classes to use for seeding data and configuring behaviour of the few mocks used.
    /// </summary>
    /// <param name="factory">Test web app factory injected by xunit</param>
    /// <param name="testOutputHelper"></param>
    /// <param name="startLiveUpdateConnection">If true, will establish a SignalR connection to test server before each test</param>
    /// <param name="liveUpdateConnectionProjectId">The ID of the project for which the default SignalR live update connection is made</param>
    protected BaseIntegrationTestFixture(
        TestWebApplicationFactory factory, 
        ITestOutputHelper testOutputHelper,
        bool startLiveUpdateConnection = false, 
        long? liveUpdateConnectionProjectId=null
    ) {
        XunitContext.Register(testOutputHelper);
        
        _factory = factory;
        _startLiveUpdateConnection = startLiveUpdateConnection || XunitContext.Context.Test.TestCase.Traits.ContainsKey("StartHubConnection");
        LiveUpdateConnectionProjectId = liveUpdateConnectionProjectId ?? FakeDataGenerator.NextId;

        ScrumBoardStateStorageServiceMock = new Mock<IScrumBoardStateStorageService>();
        JsInteropServiceMock = new Mock<IJsInteropService>();
        
        ClockMock = new Mock<IClock>();
        ClockMock.Setup(x => x.Now).Returns(DateTime.Now);

        AuthenticationServiceMock = new Mock<IAuthenticationService>();
        AuthenticationServiceMock
            .Setup(x => x.GetClaimsIdentityForBearerTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new ClaimsIdentity(
                    new[] { new Claim(JwtRegisteredClaimNames.NameId, FakeDataGenerator.DefaultUserId.ToString()) }, 
                    "Bearer", 
                    ClaimsIdentity.DefaultNameClaimType, 
                    ClaimsIdentity.DefaultRoleClaimType
                )
            );
        
        var webAppFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => ConfigureStateStorageService(ScrumBoardStateStorageServiceMock).Object);
                services.AddScoped(_ => ConfigureJsInterop(JsInteropServiceMock).Object);
                services.AddScoped(_ => ClockMock.Object);
                services.AddScoped(_ => AuthenticationServiceMock.Object);
                ConfigureScopedServices(services);
            });
        });
        ServiceProvider = webAppFactory.Server.Services;
        TestServer = webAppFactory.Server;

        _dbConnection = GetDbContextFactory().CreateDbContext().Database.GetDbConnection();
        _dbConnection.Open();
        _usageDataDbConnection = GetUsageDataDbContextFactory().CreateDbContext().Database.GetDbConnection();
        _usageDataDbConnection.Open();
        
        HttpClient = webAppFactory.CreateClient();
    }

    /// <summary>
    /// Entry point for customising general service implementations, e.g. to replace a concrete implementation with a mock.
    /// If mocking IJsInteropService or IScrumBoardStateStorageService, don't use this method, but instead use
    /// <see cref="ConfigureJsInterop(Mock{IJsInteropService})"/> or <see cref="ConfigureStateStorageService(Mock{IScrumBoardStateStorageService})"/> respectively.
    /// </summary>
    /// <param name="services"></param>
    protected virtual void ConfigureScopedServices(IServiceCollection services) { }

    /// <summary>
    /// Configure the class-level mock behaviour of IScrumBoardStateStorageService. Test case specific configuration can be made by
    /// modifying the mock object <see cref="ScrumBoardStateStorageServiceMock"/> directly.
    /// </summary>
    /// <param name="mock">Mock of IScrumBoardStateStorageService.</param>
    /// <returns>Configured mock of IScrumBoardStateStorageService.</returns>
    protected virtual Mock<IScrumBoardStateStorageService> ConfigureStateStorageService(Mock<IScrumBoardStateStorageService> mock)
    {
        return mock;
    }

    /// <summary>
    /// Configure the class-level mock behaviour of IJsInteropService. Test case specific configuration can be made by
    /// modifying the mock object <see cref="JsInteropServiceMock"/> directly.
    /// </summary>
    /// <param name="mock">Mock of IJsInteropService.</param>
    /// <returns>Configured mock of IJsInteropService.</returns>
    protected virtual Mock<IJsInteropService> ConfigureJsInterop(Mock<IJsInteropService> mock)
    {
        return mock;
    }

    /// <summary>
    /// Seed the database with sample data. Override this method to populate the database with data for all tests in the class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task SeedSampleDataAsync(DatabaseContext dbContext) { return Task.CompletedTask; }
    
    public virtual async Task InitializeAsync()
    {
        if (_liveUpdateConnection is not null)
            throw new InvalidAsynchronousStateException("Live update connection should not exist yet, but it is not null");
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await using var usageContext = await GetUsageDataDbContextFactory().CreateDbContextAsync();
        await usageContext.Database.EnsureDeletedAsync();
        await usageContext.Database.EnsureCreatedAsync();

        DefaultUser = FakeDataGenerator.CreateFakeUser();
        DefaultUser.Id = FakeDataGenerator.DefaultUserId;
        await context.Users.AddAsync(DefaultUser);
        await context.SaveChangesAsync();
        
        await SeedSampleDataAsync(context);

        if (!_startLiveUpdateConnection) return;
        _liveUpdateConnection = CreateTestHubConnection();
        await _liveUpdateConnection.StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (_startLiveUpdateConnection)
        {
            await _liveUpdateConnection.StopAsync();
            await _liveUpdateConnection.DisposeAsync();
        }
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        
        await using var usageDbContext = await GetUsageDataDbContextFactory().CreateDbContextAsync();
        await usageDbContext.Database.EnsureDeletedAsync();
        
        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();
        
        await _usageDataDbConnection.CloseAsync();
        await _usageDataDbConnection.DisposeAsync();

        HttpClient.Dispose();
        TestServer.Dispose();
        await _factory.DisposeAsync();
    }
    
    protected async Task SaveEntries<T>(IEnumerable<T> dbEntries) where T : class
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.Set<T>().AddRangeAsync(dbEntries);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Wait until some assertion regarding the currently received live update invocations returns true.
    /// </summary>
    /// <param name="assertion">Assertion for which we wait until it passes</param>
    /// <param name="timeoutInMilliseconds">Milliseconds to wait before throwing an exception if invocations still empty</param>
    /// <param name="checkIntervalInMilliseconds">Milliseconds between checks</param>
    /// <exception cref="TimeoutException">Thrown if assertion does not pass within configured timeout</exception>
    protected async Task WaitForLiveUpdateInvocationsAssertionToPass(
        Func<IReadOnlyCollection<LiveUpdateEventInvocation>, bool> assertion,
        int timeoutInMilliseconds=5000, 
        int checkIntervalInMilliseconds=100
    ) {
        var timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (assertion(LiveUpdateEventInvocations)) return;
            await Task.Delay(checkIntervalInMilliseconds);
        }

        throw new TimeoutException("LiveUpdateInvocations did not pass given assertion before timeout reached.");
    }
    
    /// <summary>
    /// Wait for invocations to not be empty, optionally specifying the type of invocation to consider.
    /// </summary>
    /// <param name="eventType">Optional, if given will only consider this event type</param>
    /// <param name="timeoutInMilliseconds">Milliseconds to wait before throwing an exception if invocations still empty</param>
    /// <param name="checkIntervalInMilliseconds">Milliseconds between checks</param>
    /// <exception cref="TimeoutException">Thrown if no invocations are found within configured timeout</exception>
    protected async Task WaitForLiveUpdateInvocationsToNotBeEmpty(
        LiveUpdateEventType? eventType = null, 
        int timeoutInMilliseconds=5000, 
        int checkIntervalInMilliseconds=100
    ) {
        try
        {
            await WaitForLiveUpdateInvocationsAssertionToPass(
                invocations => invocations.Any(x => eventType == null || x.EventType == eventType),
                timeoutInMilliseconds,
                checkIntervalInMilliseconds
            );
        }
        catch (TimeoutException)
        {
            throw new TimeoutException(
                "LiveUpdateInvocations did not contain any invocations before timeout reached. " +
                "Are you sure that your test entities involved in live updating are owned by a project with " +
                $"ID equal to whatever is in BaseIntegrationTestFixture.{nameof(LiveUpdateConnectionProjectId)} " +
                $"(in this case their project ID should be {LiveUpdateConnectionProjectId}). \n\n" +
                
                "If you are certain the project ID is correct, and this exception is being thrown seemingly " +
                "randomly, then you may be experiencing a race condition where live update connections are " +
                "being disrupted by other tests. In this case, ensure that all tests occur within the scope " +
                $"of the isolated test collection [{nameof(LiveUpdateIsolationCollection)}]. This ensures that " +
                "all such tests are run sequentially, and should prevent such race conditions from occurring."
            );
        }
    }

    /// <summary>
    /// Creates a hub connection to the test server with given settings.
    /// </summary>
    /// <param name="projectIdString">Optional, if given will override default project ID to which connection is made</param>
    /// <param name="bearerToken">Optional, if given will override the bearer token passed to Authorization header</param>
    /// <param name="skipBearerToken">If true, will skip sending a bearer token. Used for testing scenarios where connection is not authenticated</param>
    /// <param name="connectionId">Optional, used to distinguish events received by different connections when multiple exist at once</param>
    /// <returns>A new hub connection configured to connect to the test server with given settings.</returns>
    protected HubConnection CreateTestHubConnection(
        string projectIdString = null, 
        string bearerToken = "some-token", 
        bool skipBearerToken = false,
        string connectionId = ""
    ) {
        var client = TestServer.CreateClient();
        var connection = new HubConnectionBuilder()
            .WithUrl(client.BaseAddress! + EntityUpdateHub.Url.TrimStart('/'), options => {
                options.HttpMessageHandlerFactory = _ => TestServer.CreateHandler();
                if(!skipBearerToken) options.Headers.Add("Authorization", bearerToken);
                options.Headers.Add("ProjectId", projectIdString ?? LiveUpdateConnectionProjectId.ToString());
            })
            .Build();

        connection.On<string, long, string, long>("ReceiveEntityUpdate",
            (typeName, entityId, newEntityValue, editingUserId) =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    Type.GetType(typeName),
                    entityId, 
                    editingUserId, 
                    newEntityValue,
                    null,
                    LiveUpdateEventType.EntityUpdated,
                    connectionId
                ));
            });
        
        connection.On<string, long>("EntityHasChanged",
            (typeName, entityId) =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    Type.GetType(typeName),
                    entityId, 
                    default, 
                    null,
                    null,
                    LiveUpdateEventType.EntityHasChanged,
                    connectionId
                ));
            });
        
        connection.On<string, long, long>("StartedUpdatingEntity",
            (typeName, entityId, editingUserId) =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    Type.GetType(typeName),
                    entityId, 
                    editingUserId, 
                    null,
                    null,
                    LiveUpdateEventType.EditingBegunOnEntity,
                    connectionId
                ));
            });
        
        connection.On<string, long, long>("StoppedUpdatingEntity",
            (typeName, entityId, editingUserId) =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    Type.GetType(typeName),
                    entityId, 
                    editingUserId, 
                    null,
                    null,
                    LiveUpdateEventType.EditingEndedOnEntity,
                    connectionId
                ));
            });
        
        connection.On<string>("HandleConnectionError",
            errorMessage =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    null,
                    default,
                    default,
                    null,
                    errorMessage,
                    LiveUpdateEventType.ConnectionError,
                    connectionId
                ));
            });
        
        connection.On("HandleConnectionSuccess",
            () =>
            {
                LiveUpdateEventInvocations.Add(new LiveUpdateEventInvocation(
                    null,
                    default,
                    default,
                    null,
                    null,
                    LiveUpdateEventType.ConnectionSuccess,
                    connectionId
                ));
            });

        return connection;
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .ConfigureTestServices(services =>
                {
                    // Add in all services from main application
                    TestProgram.RegisterDefaultScrumBoardServices(services, null);

                    var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<DatabaseContext>));
                    services.Remove(dbContextDescriptor);

                    var usageDataDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<UsageDataDbContext>));
                    services.Remove(usageDataDbContextDescriptor);

                    services.AddDbContextFactory<DatabaseContext>(options =>
                        options.UseSqlite($"DataSource=file:{Guid.NewGuid()};mode=memory;cache=shared")
                            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                            .EnableDetailedErrors()
                            .EnableSensitiveDataLogging()
                    );
                    services.AddDbContextFactory<UsageDataDbContext>(options =>
                        options.UseSqlite($"DataSource=file:{Guid.NewGuid()};mode=memory;cache=shared")
                            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                            .EnableDetailedErrors()
                            .EnableSensitiveDataLogging()
                    );

                    services.AddSignalR();
                    
                    services.AddControllers().AddApplicationPart(typeof(Program).Assembly);
                })
                .Configure(app =>
                {
                    app.UseStaticFiles();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapHub<EntityUpdateHub>(EntityUpdateHub.Url);
                    });
                })
                .UseTestServer()
            );
    }
}

public class TestProgram : Program
{
    public static void RegisterDefaultScrumBoardServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterTypeConverters();
        RegisterServices(services, configuration);
    }
}