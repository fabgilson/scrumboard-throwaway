using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Grpc.Core.Testing;
using IdentityProvider.DataAccess;
using IdentityProvider.Models.Entities;
using IdentityProvider.Tests.Integration.Infrastructure.Ldap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLensResources;

namespace IdentityProvider.Tests.Integration.Infrastructure
{
    /// <summary>
    /// A base class that test classes may extend. This class configures test class to allow dependency injecting services,
    /// which we can also choose to mock, either partially or fully.
    /// </summary>
    public class BaseTestServerFixture<TStartup> : IClassFixture<CustomWebApplicationFactory<TStartup>>  where TStartup : BaseTestStartup
    {
        private readonly CustomWebApplicationFactory<TStartup> _customWebFactory;
        protected DatabaseContext GetDatabaseContext() => _customWebFactory.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>().CreateDbContext();
        private IServiceProvider ScopedServiceProvider => _customWebFactory.Server.Services.CreateScope().ServiceProvider;
        protected T GetScopedService<T>() where T : notnull => ScopedServiceProvider.GetRequiredService<T>();

        protected GrpcChannel? UnauthenticatedGrpcChannel { get; private set; }
        protected GrpcChannel? RegularUserUcLdapGrpcChannel { get; private set; }
        protected GrpcChannel? SystemAdminUcLdapGrpcChannel { get; private set; }
        protected GrpcChannel? RegularUserLensIdGrpcChannel { get; private set; }
        protected GrpcChannel? SystemAdminLensIdGrpcChannel { get; private set; }
        
        protected async Task<User> FindReggieRegular_UcLdap_Async() => await GetScopedService<UserManager<User>>().FindByNameAsync(FakeLdapConnectionService.GetReggieRegular_UcLdap().UserName);
        protected async Task<User> FindBennieRegular_LensId_Async() => await GetScopedService<UserManager<User>>().FindByNameAsync(SampleDataHelper.GetBennieRegular_LensId().UserName);
        
        protected async Task<User> FindUserByIdAsync(long userId) => await GetScopedService<UserManager<User>>().FindByIdAsync(userId.ToString());


        protected BaseTestServerFixture(CustomWebApplicationFactory<TStartup> customWebFactory)
        {
            _customWebFactory = customWebFactory;
            ConfigureTestUsers();
        }

        /// <summary>
        /// We need a delegating handler, which handles the response before it reaches the client, to set the HTTP version of the response.
        /// gRPC requires HTTP/2 to function. Due to a known issue with the TestServer, the default version of the response is set to 1.1.
        /// Hence, with the delegating handler, we set the version number of the response back to 2.0 (same as the request)
        /// </summary>
        private class ResponseVersionHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                var response = await base.SendAsync(request, cancellationToken);
                response.Version = request.Version;
                return response;
            }
        }

        /// <summary>
        /// Given some DB context, add some sample users of different roles to use for tests
        /// </summary>
        private void ConfigureTestUsers()
        {
            var authenticationService = _customWebFactory.Services.GetRequiredService<LensAuthenticationService.LensAuthenticationServiceBase>();

            var context = GetDatabaseContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            var userManager = GetScopedService<UserManager<User>>();
            
            // Add some LDAP users
            userManager.CreateAsync(FakeLdapConnectionService.GetReggieRegular_UcLdap(), "Reggie_is_c00l").GetAwaiter().GetResult();
            userManager.CreateAsync(FakeLdapConnectionService.GetSammieSysadmin_UcLdap(), "Sammie_is_c00ler").GetAwaiter().GetResult();

            // Add some LENS users
            userManager.CreateAsync(SampleDataHelper.GetBennieRegular_LensId(), SampleDataHelper.BennieRegularPassword).GetAwaiter().GetResult();
            userManager.CreateAsync(SampleDataHelper.GetSallySysadmin_LensId(), SampleDataHelper.SallySysAdminPassword).GetAwaiter().GetResult();
            
            var client = _customWebFactory.CreateDefaultClient(new ResponseVersionHandler());
            UnauthenticatedGrpcChannel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions { HttpClient = client });

            var emptyCallContext = TestServerCallContext.Create("", "", DateTime.Now, null, 
                CancellationToken.None, "", null, null, null, null, null);
            
            var regularLdapUserClient = _customWebFactory.CreateDefaultClient(new ResponseVersionHandler());
            var regularLdapJwt = authenticationService.Authenticate(new LensAuthenticateRequest {
                Username = FakeLdapConnectionService.GetReggieRegular_UcLdap().UserName,
                Password = "this-string-is-not-used",
                KeepLoggedIn = true
            }, emptyCallContext).GetAwaiter().GetResult().Token;
            regularLdapUserClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {regularLdapJwt}");
            RegularUserUcLdapGrpcChannel = GrpcChannel.ForAddress(regularLdapUserClient.BaseAddress!, new GrpcChannelOptions { HttpClient = regularLdapUserClient });
            
            var adminLdapClient = _customWebFactory.CreateDefaultClient(new ResponseVersionHandler());
            var adminLdapJwt = authenticationService.Authenticate(new LensAuthenticateRequest {
                Username = FakeLdapConnectionService.GetSammieSysadmin_UcLdap().UserName,
                Password = "this-string-is-not-used",
                KeepLoggedIn = true
            }, emptyCallContext).GetAwaiter().GetResult().Token;
            adminLdapClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {adminLdapJwt}");
            SystemAdminUcLdapGrpcChannel = GrpcChannel.ForAddress(adminLdapClient.BaseAddress!, new GrpcChannelOptions { HttpClient = adminLdapClient });
            
            var regularLensUserClient = _customWebFactory.CreateDefaultClient(new ResponseVersionHandler());
            var regularLensJwt = authenticationService.Authenticate(new LensAuthenticateRequest {
                Username = SampleDataHelper.GetBennieRegular_LensId().UserName,
                Password = SampleDataHelper.BennieRegularPassword,
                KeepLoggedIn = true
            }, emptyCallContext).GetAwaiter().GetResult().Token;
            regularLensUserClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {regularLensJwt}");
            RegularUserLensIdGrpcChannel = GrpcChannel.ForAddress(regularLensUserClient.BaseAddress!, new GrpcChannelOptions { HttpClient = regularLensUserClient });
            
            var adminLensClient = _customWebFactory.CreateDefaultClient(new ResponseVersionHandler());
            var adminLensJwt = authenticationService.Authenticate(new LensAuthenticateRequest {
                Username = SampleDataHelper.GetSallySysadmin_LensId().UserName,
                Password = SampleDataHelper.SallySysAdminPassword,
                KeepLoggedIn = true
            }, emptyCallContext).GetAwaiter().GetResult().Token;
            adminLensClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {adminLensJwt}");
            SystemAdminLensIdGrpcChannel = GrpcChannel.ForAddress(adminLensClient.BaseAddress!, new GrpcChannelOptions { HttpClient = adminLensClient });
        }
    }

    /// <summary>
    /// As we are intercepting the usual host builder with out custom startup file, here we provide our own host builder by
    /// extending WebApplicationFactory and implementing the CreateHostBuilder method using our custom startup file.
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(x =>
                {
                    x.UseStartup<TStartup>()
                        .UseTestServer()
                        .ConfigureLogging(logging => 
                        {
                            logging.AddConsole().AddDebug();
                        });
                })
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["SigningKey"] = "idp-integration-test-signing-key",
                    });
                });
            return builder;
        }
    }
}