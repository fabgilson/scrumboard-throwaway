using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.LiveUpdating;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class EntityUpdateHubConnectionWrapper
{
    private readonly HubConnection _hubConnection;

    /// <summary>
    /// Parameterless constructor for mocking
    /// </summary>
    public EntityUpdateHubConnectionWrapper() { }

    public EntityUpdateHubConnectionWrapper(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    /// <summary>
    /// Registers a handler for receiving entity updates through a SignalR HubConnection.
    /// This method filters the updates based on the entity type and the specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to listen for updates.</typeparam>
    /// <param name="id">The ID of the entity to listen for updates.</param>
    /// <param name="handler">
    /// The asynchronous function to handle the entity update. The function receives the updated entity of type <typeparamref name="TEntity"/>
    /// and the ID of the editing user. The updated entity value is received by deserializing a JSON string, so do not assume any navigation
    /// properties will be included.
    /// </param>
    /// <param name="callingClassType">The type of class that called this method. Not used here, but allows for it to be captured in tests.</param>
    // ReSharper disable once UnusedParameter.Global
    public virtual IDisposable OnUpdateReceivedForEntityWithId<TEntity>(long id, Func<TEntity, long, Task> handler, Type callingClassType) where TEntity : IId
    {
        return _hubConnection.On<string, long, string, long>("ReceiveEntityUpdate", (typeName, entityId, newEntityValue, editingUserId) =>
        {
            if (typeName != typeof(TEntity).AssemblyQualifiedName || entityId != id) return Task.CompletedTask;
            var deserializedNewValue = JsonSerializer.Deserialize<TEntity>(newEntityValue);
            return handler(deserializedNewValue, editingUserId);
        });
    }

    /// <summary>
    /// Registers a handler for receiving notifications when an update on an entity begins, through a SignalR HubConnection.
    /// Filters the notifications based on the entity type and specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for the beginning of updates.</param>
    /// <param name="onUpdateBegunByUserHandler">
    /// The asynchronous function to handle the beginning of the update.
    /// The function receives the ID of the user who began the update.
    /// </param>
    /// <param name="callingClassType">The type of class that called this method. Not used here, but allows for it to be captured in tests.</param>
    // ReSharper disable once UnusedParameter.Global
    public virtual IDisposable OnUpdateBegunForEntityByUser<TEntity>(long entityId, Func<long, Task> onUpdateBegunByUserHandler, Type callingClassType)
    {
        return _hubConnection.On<string, long, long>("StartedUpdatingEntity", (typeName, id, editingUserId) =>
        {
            if (typeName != typeof(TEntity).AssemblyQualifiedName || entityId != id) return Task.CompletedTask;
            return onUpdateBegunByUserHandler(editingUserId);
        });
    }

    /// <summary>
    /// Registers a handler for receiving notifications when an update on an entity ends, through a SignalR HubConnection.
    /// Filters the notifications based on the entity type and specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for the end of updates.</param>
    /// <param name="onUpdateEndedByUserHandler">
    /// The asynchronous function to handle the end of the update.
    /// The function receives the ID of the user who ended the update.
    /// </param>
    /// <param name="callingClassType">The type of class that called this method. Not used here, but allows for it to be captured in tests.</param>
    // ReSharper disable once UnusedParameter.Global
    public virtual IDisposable OnUpdateEndedForEntityByUser<TEntity>(long entityId, Func<long, Task> onUpdateEndedByUserHandler, Type callingClassType)
    {
        return _hubConnection.On<string, long, long>("StoppedUpdatingEntity", (typeName, id, editingUserId) =>
        {
            if (typeName != typeof(TEntity).AssemblyQualifiedName || entityId != id) return Task.CompletedTask;
            return onUpdateEndedByUserHandler(editingUserId);
        });
    }

    /// <summary>
    /// Registers a handler for receiving notifications that some entity has had its state changed and may need to be refreshed.
    /// </summary>
    /// <param name="entityId">ID of entity that has had its state changed</param>
    /// <param name="onEntityChangedHandler">Asynchronous function to perform when a notification is received of the entity changing state</param>
    /// <param name="callingClassType">The type of class that called this method. Not used here, but allows for it to be captured in tests.</param>
    // ReSharper disable once UnusedParameter.Global
    public virtual IDisposable OnEntityWithIdHasChanged<TEntity>(long entityId, Func<Task> onEntityChangedHandler, Type callingClassType)
    {
        return _hubConnection.On<string, long>("EntityHasChanged", (typeName, id) =>
        {
            if (typeName != typeof(TEntity).AssemblyQualifiedName || entityId != id) return Task.CompletedTask;
            return onEntityChangedHandler();
        });
    }
}