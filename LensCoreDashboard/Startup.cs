using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using SharedLensResources;
using SharedLensResources.Authentication;
using SharedLensResources.Blazor.StateManagement;
using SharedLensResources.Extensions;

namespace LensCoreDashboard;

public class Startup
{
    private static IConfiguration Configuration { get; set; } = null!;
    private IWebHostEnvironment Environment { get; }
    
    public Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        Environment = env;
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();

        services.AddHttpContextAccessor();

        services.AddHttpClient();

        ConfigureScopedServices(services);
    }

    
    /// <summary>
    /// Registers services for dependency injection.
    /// </summary>
    /// <param name="services">The IServiceCollection used to configure this application's services</param>
    private static void ConfigureScopedServices(IServiceCollection services)
    {
        services.AddBlazorise(o => { o.Immediate = true; })
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        // Add gRPC clients using our extension method which configures the interceptor which appends auth token
        services.AddAuthInterceptedGrpcClient<LensAuthenticationService.LensAuthenticationServiceClient>(Configuration);
        services.AddAuthInterceptedGrpcClient<LensUserService.LensUserServiceClient>(Configuration);
        
        services.AddScoped<IProtectedLocalStorageWrapper, ProtectedLocalStorageWrapper>();
        services.AddScoped<IProtectedSessionStorageWrapper, ProtectedSessionStorageWrapper>();
        services.AddScoped<IStateStorageService, StateStorageService>();
        
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        if (Environment.IsDevelopment())
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
        
        if (Configuration.GetAppBasePath() is not null) {
            app.UsePathBase(Configuration.GetAppBasePath());
        }           

        app.UseStaticFiles();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}