using System;
using Grpc.Core;
using IdentityProvider.Tests.Integration.Infrastructure;

namespace IdentityProvider.Tests.Integration.Tests;

/// <summary>
/// Extend this class to gain access to some pre-made gRPC clients with differing levels of authentication
/// and authorization with the Lens IdP.
/// </summary>
/// <typeparam name="TStartup">The type of start-up class used to specify a unique in-mem DB name</typeparam>
/// <typeparam name="TGrpcClient">The type of gRPC client to create</typeparam>
public class BaseAuthenticationGrpcServiceTest<TStartup, TGrpcClient> : BaseTestServerFixture<TStartup> 
    where TStartup : BaseTestStartup where TGrpcClient : ClientBase<TGrpcClient>
{
    protected TGrpcClient UnauthenticatedClient =>
        (TGrpcClient)Activator.CreateInstance(typeof(TGrpcClient), UnauthenticatedGrpcChannel)!;

    protected TGrpcClient RegularUserUcLdapAuthClient =>
        (TGrpcClient)Activator.CreateInstance(typeof(TGrpcClient), RegularUserUcLdapGrpcChannel)!;

    protected TGrpcClient SystemAdminUcLdapAuthClient =>
        (TGrpcClient)Activator.CreateInstance(typeof(TGrpcClient), SystemAdminUcLdapGrpcChannel)!;

    protected TGrpcClient RegularUserLensIdAuthClient =>
        (TGrpcClient)Activator.CreateInstance(typeof(TGrpcClient), RegularUserLensIdGrpcChannel)!;

    protected TGrpcClient SystemAdminLensIdAuthClient =>
        (TGrpcClient)Activator.CreateInstance(typeof(TGrpcClient), SystemAdminLensIdGrpcChannel)!;

    protected BaseAuthenticationGrpcServiceTest(CustomWebApplicationFactory<TStartup> customWebFactory) 
        : base(customWebFactory) { }
}