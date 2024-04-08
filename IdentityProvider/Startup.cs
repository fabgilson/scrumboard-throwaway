using System.IdentityModel.Tokens.Jwt;
using IdentityProvider.DataAccess;
using IdentityProvider.Extensions;
using IdentityProvider.Models.Entities;
using IdentityProvider.Services.External;
using IdentityProvider.Services.Internal;
using IdentityProvider.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedLensResources.Options;

namespace IdentityProvider
{
    public class Startup
    {
        protected IConfiguration Configuration { get; }
        private IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            
            ConfigureDatabase(services);
            ConfigureScopedServices(services);

            services.AddIdentityCore<User>(PasswordValidationOptions.DefaultPasswordPolicy)
                .AddUserStore<LensUserStore>()
                .AddSignInManager();

            services.AddAuthorizationCore();
            services.AddAuthentication(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<AuthenticationGrpcService>();
                endpoints.MapGrpcService<UserGrpcService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        /// <summary>
        /// Used to configure the database of the application.
        /// Normally this code block would just be included in the above ConfigureServices method,
        /// but by separating it into this (virtual) method we are able to mock it easily for testing.
        /// </summary>
        /// <param name="services">The IServiceCollection used to configure this application's services</param>
        public virtual void ConfigureDatabase(IServiceCollection services)
        {
            var dbOptions = new DatabaseOptions();
            Configuration.GetSection("Database").Bind(dbOptions);
            if (dbOptions.UseInMemory)
            {
                services.AddDbContextFactory<DatabaseContext>(options => options
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

            services.AddDbContextFactory<DatabaseContext>(dbContextOptions => dbContextOptions.UseMySql(
                connectionString, 
                new MariaDbServerVersion(ServerVersion.AutoDetect(connectionString)),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            ));
        }

        /// <summary>
        /// Used to configure the scoped services of this application.
        /// Normally this code block would just be included in the above ConfigureServices method,
        /// but by separating it into this (virtual) method we are able to mock it easily for testing.
        /// </summary>
        /// <param name="services">The IServiceCollection used to configure this application's services</param>
        public virtual void ConfigureScopedServices(IServiceCollection services)
        {
            services.AddScoped<ILdapConnectionService, LdapConnectionService>();
            if (Environment.IsDevelopment() || Configuration.GetValue<bool>("SeedSampleUserAccounts"))
            {
                services.AddScoped<ISeedUserService, SeedUserService>();
            }
        }
    }
}