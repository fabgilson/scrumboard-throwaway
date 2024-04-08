using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services;
using SharedLensResources;
using SharedLensResources.Authentication;

namespace ScrumBoard.LiveUpdating;

public class EntityUpdateHub : Hub
{
    private static int _connectionCount;
    private static ConcurrentDictionary<long, int> ConnectionCountPerProjectGroup { get; } = new();
    private static ConcurrentDictionary<long, int> ConnectionCountPerUserGroup { get; } = new();
    
    public const string Url = "/entityUpdateHub";

    private readonly ILogger<EntityUpdateHub> _logger;
    private readonly IProjectService _projectService;
    private readonly IAuthenticationService _authenticationService;

    public EntityUpdateHub(ILogger<EntityUpdateHub> logger, IProjectService projectService, IAuthenticationService authenticationService)
    {
        _logger = logger;
        _projectService = projectService;
        _authenticationService = authenticationService;
    }
    
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var bearerToken = httpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (bearerToken is null)
        {
            await Clients.Caller.SendAsync("HandleConnectionError", "No bearer token given");
            _logger.LogWarning("Connection attempted without bearer token present, aborting connection");
            Context.Abort();
            return;
        }

        var auth = await _authenticationService.GetClaimsIdentityForBearerTokenAsync(overrideBearerToken: bearerToken);
        var principle = new ClaimsPrincipal(auth);
        if (!auth.IsAuthenticated || !long.TryParse(principle.FindFirstValue(JwtRegisteredClaimNames.NameId), out var userIdAsLong))
        {
            await Clients.Caller.SendAsync("HandleConnectionError", "Authentication failed");
            _logger.LogWarning("Connection attempted without proper authentication, aborting connection");
            Context.Abort();
            return;
        }
        Context.Items.Add("UserId", userIdAsLong);
        var isAdmin = principle.IsInRole(nameof(GlobalLensRole.SystemAdmin));
        
        var projectId = Context.GetHttpContext()?.Request.Headers["ProjectId"].FirstOrDefault();
        if (projectId is null || !long.TryParse(projectId, out var projectIdAsLong))
        {
            await Clients.Caller.SendAsync("HandleConnectionError", "No valid project ID given");
            _logger.LogWarning("HubConnection created without a valid ProjectId supplied, aborting connection");
            Context.Abort();
            return;
        }
        Context.Items.Add("ProjectId", projectIdAsLong);
            
        Interlocked.Increment(ref _connectionCount);
        _logger.LogInformation(
            "Creating new live update connection ({TotalConnections} total active connections)",
            _connectionCount
        );

        await AddConnectionToUserGroupAsync(userIdAsLong);
        await AddConnectionToProjectGroupAsync(projectIdAsLong, userIdAsLong, isAdmin);
        await base.OnConnectedAsync();
    }
    
    private async Task AddConnectionToUserGroupAsync(long userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        ConnectionCountPerUserGroup.AddOrUpdate(userId, 1, (_, oldValue) => oldValue + 1);

        _logger.LogInformation(
            "Adding new connection to user group for user with ID={UserId} ({TotalConnections} total connections now in user group)", 
            userId,
            ConnectionCountPerUserGroup[userId]
        );
    }
    
    private async Task AddConnectionToProjectGroupAsync(long projectId, long userId, bool isAdmin)
    {
        var roleInProject = await _projectService.GetUserMembershipInProjectAsync(projectId, userId);
        if (roleInProject is null && !isAdmin)
        {
            await Clients.Caller.SendAsync("HandleConnectionError", "User is not authorized to connect to given project");
            _logger.LogWarning("User is not authorised to view given project (ID={ProjectId}), aborting connection", projectId);
            Context.Abort();
            return;
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        ConnectionCountPerProjectGroup.AddOrUpdate(projectId, 1, (_, oldValue) => oldValue + 1);
        
        await Clients.Caller.SendAsync("HandleConnectionSuccess");
        _logger.LogInformation(
            "Adding new connection to project group for project with ID={ProjectId} ({TotalConnections} total connections now in group)", 
            projectId,
            ConnectionCountPerProjectGroup[projectId]
        );
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await RemoveConnectionFromUserGroupAsync();
        await RemoveConnectionFromProjectGroupAsync();
        
        Interlocked.Decrement(ref _connectionCount);
        _logger.LogInformation(
            "Connection disposed ({TotalConnections} total active connections)",
            _connectionCount
        );
        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task RemoveConnectionFromUserGroupAsync()
    {
        if (Context.Items["UserId"] is not long userId)
        {
            _logger.LogWarning("UserId could not be accessed when attempting to remove connection from user group, skipping group removal");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

        ConnectionCountPerUserGroup.AddOrUpdate(userId, 0, (_, oldValue) => oldValue > 0 ? oldValue - 1 : 0);

        _logger.LogInformation(
            "Removed connection from group for user with ID={UserId} ({TotalConnections} remaining connections in user group)", 
            userId,
            ConnectionCountPerUserGroup[userId]
        );
    }

    private async Task RemoveConnectionFromProjectGroupAsync()
    {
        if (Context.Items["ProjectId"] is not long projectId)
        {
            _logger.LogWarning("ProjectId could not be accessed when attempting to remove connection from group, skipping group removal");
            return;
        }
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        
        ConnectionCountPerProjectGroup.AddOrUpdate(projectId, 0, (_, oldValue) => oldValue > 0 ? oldValue - 1 : 0);
        
        _logger.LogInformation(
            "Removed connection from group for project with ID={ProjectId} ({TotalConnections} remaining connections in group)", 
            projectId,
            ConnectionCountPerProjectGroup[projectId]
        );
    }
}