using System;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Utils;
using SharedLensResources;
using SharedLensResources.Authentication;
using SharedLensResources.Blazor.StateManagement;
using SharedLensResources.Extensions;
using SharedLensResources.Options;

namespace ScrumBoard;

public class Program
{
    public static async Task Main(string[] args)
    {
        var webAppBuilder = WebApplication.CreateBuilder(args);
        webAppBuilder.Configuration.AddEnvironmentVariables();
        
        ConfigureServices(webAppBuilder.Services, webAppBuilder.Configuration);
        ConfigureDataAccess(webAppBuilder.Services, webAppBuilder.Configuration);
        
        var app = webAppBuilder.Build();
        
        ConfigureWebHost(app);
        await PrepareDatabase(app.Services);

        await app.RunAsync();
    }

    private static void ConfigureWebHost(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        var configurationService = app.Services.GetRequiredService<IConfiguration>();
        var basePath = configurationService.GetAppBasePath();
        
        if (!string.IsNullOrEmpty(basePath))
        {
            app.UsePathBase(basePath);
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<EntityUpdateHub>(EntityUpdateHub.Url);
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    private static async Task PrepareDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        // Perform migration for ScrumBoard db context as needed
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            if (context.Database.IsRelational())
            {
                logger.LogInformation("Relational database detected for main ScrumBoard data, beginning migration");
                await context.Database.MigrateAsync();
            }
            else if (await context.Database.GetService<IDatabaseCreator>().CanConnectAsync())
            {
                logger.LogInformation(
                    "Non-relational database detected for main ScrumBoard data, attempting to generate schema without migrations");
                await context.Database.EnsureCreatedAsync();
            }
        }

        // Perform migration for usage-data db context as needed
        var usageDataContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UsageDataDbContext>>();
        await using (var context = await usageDataContextFactory.CreateDbContextAsync())
        {
            if (context.Database.IsRelational())
            {
                logger.LogInformation("Relational database detected for usage data, beginning migration");
                await context.Database.MigrateAsync();
            }
            else if (await context.Database.GetService<IDatabaseCreator>().CanConnectAsync())
            {
                logger.LogInformation(
                    "Non-relational database detected for usage data, attempting to generate schema without migrations");
                await context.Database.EnsureCreatedAsync();
            }
        }

        var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        if (configurationService.SeedDataEnabled)
        {
            // Seed our initial test users upon launch. 
            // Users will be only in this database. I.e. They are not present in IdentityProvider so you cannot sign in as them.
            await seedDataService.SeedInitialDataAsync();
        }
        else
        {
            // Seed only tags/sessions upon launch.
            await seedDataService.CreateTagsAndSessions();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddRazorPages();
        services.AddServerSideBlazor();

        services.AddHttpContextAccessor();
        services.AddHttpClient();

        // This line persists encryption keys (as used in encrypting client data) in the database, so that
        // a new key will not be generated whenever this app restarts, causing all persisted state to become
        // useless (it would be encrypted with a key we no longer know)
        services.AddDataProtection().PersistKeysToDbContext<DatabaseContext>();

        RegisterTypeConverters();
        
        RegisterServices(services, configuration);
    }

    /// <summary>
    /// Similar to ConfigureScopedServices, we pull the data access configuration out to this method to 
    /// allow it to be easily overridden for the sake of testing. Obviously, we want to be able to configure 
    /// a mocked, or temporary data source for our testing that doesn't interfere with normal sources.
    /// </summary>
    /// <param name="services">The IServiceCollection used to configure this application's services</param>
    /// <param name="configuration">Application configuration from host builder</param>
    private static void ConfigureDataAccess(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDbContextFactory<DatabaseContext>(services, configuration, "Database");
        ConfigureDbContextFactory<UsageDataDbContext>(services, configuration, "UsageDataDatabase");
    }

    /// <summary>
    /// Set up the MariaDB connection for DatabaseContext, the DB context for managing ScrumBoard entity persistence
    /// </summary>
    private static void ConfigureDbContextFactory<T>(IServiceCollection services, IConfiguration configuration, string configurationSectionName) where T : DbContext
    {
        var dbOptions = new DatabaseOptions();
        configuration.GetSection(configurationSectionName).Bind(dbOptions);
        if (dbOptions.UseInMemory)
        {
            services.AddDbContextFactory<T>(options => options
                .UseInMemoryDatabase(dbOptions.DatabaseName)
                .ConfigureWarnings(w => w.Log(InMemoryEventId.TransactionIgnoredWarning))
            );
            return;
        }
        
        var connectionString = $"server={dbOptions.Host}; " +
                               $"user={dbOptions.Username}; " +
                               $"password={dbOptions.Password}; " +
                               $"database={dbOptions.DatabaseName}; " +
                               $"port={dbOptions.Port};";

        services.AddDbContextFactory<T>(dbContextOptions => dbContextOptions.UseMySql(
            connectionString, 
            new MariaDbServerVersion(ServerVersion.AutoDetect(connectionString)),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        ));
    }
    
    protected static void RegisterTypeConverters()
    {
        TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(GitlabCredentials), new TypeConverterAttribute(typeof(GitlabCredentialsTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(TaggedWorkInstance), new TypeConverterAttribute(typeof(TaggedWorkInstanceTypeConverter)));
    }
    
    protected static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add gRPC clients using our extension method which configures the interceptor which appends auth token
        services.AddAuthInterceptedGrpcClient<LensAuthenticationService.LensAuthenticationServiceClient>(configuration);
        
        // Now our regular (internal, non-gRPC) services
        services.AddScoped<IChangelogRepository, ChangelogRepository>();
        services.AddScoped<IProjectChangelogRepository, ProjectChangelogRepository>();
        services.AddScoped<IUserStoryTaskChangelogRepository, UserStoryTaskChangelogRepository>();
        services.AddScoped<ISprintChangelogRepository, SprintChangelogRepository>();
        services.AddScoped<IUserStoryChangelogRepository, UserStoryChangelogRepository>();
        services.AddScoped<IWorklogEntryChangelogRepository, WorklogEntryChangelogRepository>();
        services.AddScoped<IOverheadEntryChangelogRepository, OverheadEntryChangelogRepository>();
        services.AddScoped<IStandUpMeetingChangelogRepository, StandUpMeetingChangelogRepository>();
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IBacklogRepository, BacklogRepository>();
        services.AddScoped<IUserStoryRepository, UserStoryRepository>();
        services.AddScoped<IWorklogTagRepository, WorklogTagRepository>();
        services.AddScoped<IUserStoryTaskRepository, UserStoryTaskRepository>();            
        services.AddScoped<IUserStoryTaskTagRepository, UserStoryTaskTagRepository>();
        services.AddScoped<IGitlabCommitRepository, GitlabCommitRepository>();
        services.AddScoped<IFormTemplateRepository, FormTemplateRepository>();
        services.AddScoped<IOverheadEntryRepository, OverheadEntryRepository>();
        services.AddScoped<IOverheadSessionRepository, OverheadSessionRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IProjectFeatureFlagRepository, ProjectFeatureFlagRepository>();
        services.AddScoped<IStandUpMeetingRepository, StandUpMeetingRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IStandUpCalendarService, StandUpCalendarService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<IUserStoryService, UserStoryService>();
        services.AddScoped<IUserStoryTaskService, UserStoryTaskService>();
        services.AddScoped<IFormInstanceService, FormInstanceService>();
        services.AddScoped<IFormTemplateService, FormTemplateService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectMembershipService, ProjectMembershipService>();
        
        services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISeedDataService, SeedDataService>();
        services.AddScoped<IStateStorageService, StateStorageService>();
        services.AddScoped<IScrumBoardStateStorageService, ScrumBoardStateStorageService>();
        services.AddScoped(typeof(ISortableService<>), typeof(SortableService<>));
        services.AddScoped<IProtectedSessionStorageWrapper, ProtectedSessionStorageWrapper>();
        services.AddScoped<IProtectedLocalStorageWrapper, ProtectedLocalStorageWrapper>();          
        services.AddScoped<IJsInteropService, JsInteropService>();
        services.AddScoped<IGitlabService, GitlabService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IProjectFeatureFlagService, ProjectFeatureFlagService>();
        services.AddScoped<IStandUpMeetingService, StandUpMeetingService>();
        services.AddScoped<IStudentGuideService, StudentGuideService>();
        services.AddScoped<IWorklogEntryService, WorklogEntryService>();
        services.AddScoped<IWorklogTagService, WorklogTagService>();
        services.AddScoped<IAcceptanceCriteriaService, AcceptanceCriteriaService>();
        services.AddScoped<IChangelogService, ChangelogService>();
        services.AddScoped<IWeeklyReflectionCheckInService, WeeklyReflectionCheckInService>();
        
        services.AddScoped<IBurndownService, BurndownService>();
        services.AddScoped<IProjectStatsService, ProjectStatsService>();
        services.AddScoped<IUserStatsService, UserStatsService>();
        services.AddScoped<IMarkingStatsService, MarkingStatsService>();
        services.AddScoped<IUserFlagService, UserFlagService>();

        services.AddScoped<IEntityLiveUpdateService, EntityLiveUpdateService>();
        services.AddScoped<IEntityLiveUpdateConnectionBuilder, EntityLiveUpdateConnectionBuilder>();
        
        services.AddScoped<IClock, SystemClock>();

        services.AddTransient<IUsageDataService, UsageDataService>();
        services.AddTransient<IFileSystem, FileSystem>();
    }
}