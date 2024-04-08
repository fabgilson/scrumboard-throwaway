using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.LiveUpdating;

public interface IEntityLiveUpdateService
{
    /// <summary>
    /// Broadcasts a new value for a specific entity ID to all connected clients in a project group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity being updated.</param>
    /// <param name="projectId">The ID of the project to which this entity belongs.</param>
    /// <param name="newValue">The new value of the entity.</param>
    /// <param name="idOfEditingUser">The ID of the user who is making the edit.</param>
    Task BroadcastNewValueForEntityToProjectAsync<T>(long id, long projectId, T newValue, long idOfEditingUser) where T : IId;

    /// <summary>
    /// Broadcasts an indication that the state of some entity has changed in a project group, and clients may want to refresh it.
    /// </summary>
    /// <param name="id">ID of entity that has changed.</param>
    /// <param name="projectId">ID of project to which the entity belongs.</param>
    /// <typeparam name="T">Type of entity that has changed.</typeparam>
    Task BroadcastChangeHasOccuredForEntityToProjectAsync<T>(long id, long projectId) where T : IId;
    
    /// <summary>
    /// Broadcasts a notification indicating that an update has started on a specific entity in a project group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity on which the update has started.</param>
    /// <param name="projectId">The ID of the project to which this entity belongs.</param>
    /// <param name="idOfEditingUser">The ID of the user who started the update.</param>
    Task BroadcastUpdateStartedOnEntityToProject<T>(long id, long projectId, long idOfEditingUser) where T : IId;
    
    /// <summary>
    /// Broadcasts a notification indicating that an update has ended on a specific entity in a project group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity on which the update has ended.</param>
    /// <param name="projectId">The ID of the project to which this entity belongs.</param>
    /// <param name="idOfEditingUser">The ID of the user who ended the update.</param>
    Task BroadcastUpdateEndedOnEntityToProject<T>(long id, long projectId, long idOfEditingUser) where T : IId;

    /// <summary>
    /// Broadcasts a new value for a specific entity ID to all connected clients in a user group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity being updated.</param>
    /// <param name="userId">The ID of the user group to which the message should be sent.</param>
    /// <param name="newValue">The new value of the entity.</param>
    /// <param name="idOfEditingUser">The ID of the user who is making the edit.</param>
    Task BroadcastNewValueForEntityToUserAsync<T>(long id, long userId, T newValue, long idOfEditingUser) where T : IId;

    /// <summary>
    /// Broadcasts an indication that the state of some entity has changed in a user group, and clients may want to refresh it.
    /// </summary>
    /// <param name="id">ID of entity that has changed.</param>
    /// <param name="userId">ID of user group to which the entity belongs.</param>
    /// <typeparam name="T">Type of entity that has changed.</typeparam>
    Task BroadcastChangeHasOccuredForEntityToUserAsync<T>(long id, long userId) where T : IId;
    
    /// <summary>
    /// Broadcasts a notification indicating that an update has started on a specific entity in a user group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity on which the update has started.</param>
    /// <param name="userId">The ID of the user group to which the message should be sent.</param>
    /// <param name="idOfEditingUser">The ID of the user who started the update.</param>
    Task BroadcastUpdateStartedOnEntityToUser<T>(long id, long userId, long idOfEditingUser) where T : IId;
    
    /// <summary>
    /// Broadcasts a notification indicating that an update has ended on a specific entity in a user group.
    /// </summary>
    /// <typeparam name="T">The type of the entity being updated.</typeparam>
    /// <param name="id">The ID of the entity on which the update has ended.</param>
    /// <param name="userId">The ID of the user group to which the message should be sent.</param>
    /// <param name="idOfEditingUser">The ID of the user who ended the update.</param>
    Task BroadcastUpdateEndedOnEntityToUser<T>(long id, long userId, long idOfEditingUser) where T : IId;
}

public class EntityLiveUpdateService : IEntityLiveUpdateService
{
    private readonly IHubContext<EntityUpdateHub> _entityUpdateHub;

    public EntityLiveUpdateService(IHubContext<EntityUpdateHub> entityUpdateHub)
    {
        _entityUpdateHub = entityUpdateHub;
    }
    
    public async Task BroadcastNewValueForEntityToProjectAsync<T>(long id, long projectId, T newValue, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"Project_{projectId}").SendAsync(
            "ReceiveEntityUpdate",
            typeof(T).AssemblyQualifiedName,
            id,
            JsonSerializer.Serialize(newValue),
            idOfEditingUser
        );
    }

    public async Task BroadcastChangeHasOccuredForEntityToProjectAsync<T>(long id, long projectId) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"Project_{projectId}").SendAsync(
            "EntityHasChanged",
            typeof(T).AssemblyQualifiedName,
            id
        );
    }

    public async Task BroadcastUpdateStartedOnEntityToProject<T>(long id, long projectId, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"Project_{projectId}").SendAsync(
            "StartedUpdatingEntity",
            typeof(T).AssemblyQualifiedName,
            id,
            idOfEditingUser
        );
    }

    public async Task BroadcastUpdateEndedOnEntityToProject<T>(long id, long projectId, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"Project_{projectId}").SendAsync(
            "StoppedUpdatingEntity",
            typeof(T).AssemblyQualifiedName,
            id,
            idOfEditingUser
        );
    }

    public async Task BroadcastNewValueForEntityToUserAsync<T>(long id, long userId, T newValue, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"User_{userId}").SendAsync(
            "ReceiveEntityUpdate",
            typeof(T).AssemblyQualifiedName,
            id,
            JsonSerializer.Serialize(newValue),
            idOfEditingUser
        );
    }

    public async Task BroadcastChangeHasOccuredForEntityToUserAsync<T>(long id, long userId) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"User_{userId}").SendAsync(
            "EntityHasChanged",
            typeof(T).AssemblyQualifiedName,
            id
        );
    }

    public async Task BroadcastUpdateStartedOnEntityToUser<T>(long id, long userId, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"User_{userId}").SendAsync(
            "StartedUpdatingEntity",
            typeof(T).AssemblyQualifiedName,
            id,
            idOfEditingUser
        );
    }

    public async Task BroadcastUpdateEndedOnEntityToUser<T>(long id, long userId, long idOfEditingUser) where T : IId
    {
        await _entityUpdateHub.Clients.Group($"User_{userId}").SendAsync(
            "StoppedUpdatingEntity",
            typeof(T).AssemblyQualifiedName,
            id,
            idOfEditingUser
        );
    }
}