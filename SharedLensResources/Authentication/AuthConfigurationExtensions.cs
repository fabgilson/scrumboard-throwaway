using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLensResources.Blazor.StateManagement;
using SharedLensResources.Extensions;

namespace SharedLensResources.Authentication;

public static class AuthConfigurationExtensions
{
    public static void AddAuthInterceptedGrpcClient<TClient>(this IServiceCollection services, IConfiguration configuration) 
        where TClient : class 
    {
        services.AddGrpcClient<TClient>(o =>
        {
            o.Address = new Uri(configuration.GetIdentityProviderUrl());
            o.ChannelOptionsActions.Add(options =>
            {
                options.UnsafeUseInsecureChannelCallCredentials = true;
            });
        }).AddCallCredentials(async (context, metadata, serviceProvider) =>
        {
            var bearerToken = await serviceProvider.GetService<IStateStorageService>()!.GetBearerTokenAsync();
            if(bearerToken is null) return;
            metadata.Add("Authorization", $"Bearer {bearerToken}");
        });
    }
}