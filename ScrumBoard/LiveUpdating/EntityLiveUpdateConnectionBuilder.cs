using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services;
using SharedLensResources.Blazor.StateManagement;

namespace ScrumBoard.LiveUpdating;

/// <summary>
/// For managing the creation of a hubConnection in some blazor component via dependency injection.
/// </summary>
public interface IEntityLiveUpdateConnectionBuilder
{
    Task<HubConnection>  CreateHubConnectionForProjectAsync(long projectId);
}

public class EntityLiveUpdateConnectionBuilder : IEntityLiveUpdateConnectionBuilder
{
    private readonly NavigationManager _navigationManager;
    private readonly IConfigurationService _configuration;
    private readonly IStateStorageService _stateStorageService;
    private readonly ILogger<EntityLiveUpdateConnectionBuilder> _logger;

    public EntityLiveUpdateConnectionBuilder(
        NavigationManager navigationManager, 
        IConfigurationService configuration, 
        IStateStorageService stateStorageService, 
        ILogger<EntityLiveUpdateConnectionBuilder> logger
    ) {
        _navigationManager = navigationManager;
        _configuration = configuration;
        _stateStorageService = stateStorageService;
        _logger = logger;
    }

    public async Task<HubConnection> CreateHubConnectionForProjectAsync(long projectId)
    {
        var token = await _stateStorageService.GetBearerTokenAsync();
        var appBasePath = _configuration.GetBaseUrl();
        var url = _navigationManager.ToAbsoluteUri(appBasePath + EntityUpdateHub.Url);
        
        _logger.LogInformation("Creating hub connection for url: {Url}", url);
        
        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.Headers["ProjectId"] = projectId.ToString();
                if (!string.IsNullOrEmpty(token))
                {
                    options.Headers["Authorization"] = $"Bearer {token}";
                }
                options.HttpMessageHandlerFactory = message =>
                {
                    // If configured to skip ssl validation, always return true for certificate validation
                    if (message is HttpClientHandler clientHandler && _configuration.LiveUpdatesSkipSslValidation)
                        clientHandler.ServerCertificateCustomValidationCallback += (_, _, _, _) => true;
                    return message;
                };
            })
            .Build();
    }
}