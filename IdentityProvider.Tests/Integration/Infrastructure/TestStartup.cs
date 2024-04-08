using System;
using HttpContextMoq;
using IdentityProvider.DataAccess;
using IdentityProvider.Services.External;
using IdentityProvider.Services.Internal;
using IdentityProvider.Tests.Integration.Infrastructure.Ldap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharedLensResources;

namespace IdentityProvider.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Inherits from the IdentityProvider startup class to allow for starting our own instance
    /// where we can mock various services to be pulled down from dependency injection
    /// </summary>
    public class BaseTestStartup : Startup
    {
        private readonly string _databaseName;

        public BaseTestStartup(IConfiguration configuration, IWebHostEnvironment webHost, string databaseName) : base(configuration, webHost)
        {
            _databaseName = databaseName;
            // Configure mock admin account to be automatically recognised as admin by authentication service
            Configuration["admin_usercodes"] = $"{FakeLdapConnectionService.AdminUsername},{FakeLdapConnectionService.GetSammieSysadmin_UcLdap().UserName}";
        }
        
        public override void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContextFactory<DatabaseContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .UseLazyLoadingProxies()
            );
        }

        public override void ConfigureScopedServices(IServiceCollection services)
        {
            base.ConfigureScopedServices(services);
            services.AddScoped<IHttpContextAccessor, MockHttpContextAccessor>();
            // Allow us to directly inject AuthenticationService so that we can skip the need for gRPC when testing it
            services.AddScoped<LensAuthenticationService.LensAuthenticationServiceBase, AuthenticationGrpcService>();
            // Replace the LDAP service with our fake one
            services.Remove(new ServiceDescriptor(typeof(ILdapConnectionService),
                typeof(LdapConnectionService), ServiceLifetime.Scoped));
            services.AddScoped<ILdapConnectionService, FakeLdapConnectionService>();
        }
    }

    internal class MockHttpContextAccessor : IHttpContextAccessor
    {
        private readonly HttpContext _instance = MakeNewHttpContextMock();
        public HttpContext? HttpContext
        {
            get => _instance;
            set { } // Don't allow anything to touch the context, or it will be disposed prematurely
        }

        private static HttpContext MakeNewHttpContextMock()
        {
            var mock = new HttpContextMock();
            var mockAuthService = new Mock<IAuthenticationService>();
            mock.RequestServicesMock.Mock
                .Setup(x => x.GetService(It.Is<Type>(x => x == typeof(IAuthenticationService))))
                .Returns(mockAuthService.Object);
            return mock;
        }
    }
}
