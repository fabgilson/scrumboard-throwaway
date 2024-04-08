using System;
using System.Text.Json;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Tests.Util.LiveUpdating;

/// <summary>
/// Represents an instance of some live update method being invoked being registered.
/// </summary>
/// <remarks>
/// Should not be used for tracking registrations of live update handlers, for that case
/// use <see cref="LiveUpdateHandlerRegistration"/>.
/// </remarks>
public class LiveUpdateEventInvocation(
    Type entityType,
    long entityId,
    long editingUserId,
    string serializedEntityValue,
    string connectionErrorText,
    LiveUpdateEventType eventType,
    string connectionId
) {
    public Type EntityType { get; set; } = entityType;
    public long EntityId { get; set; } = entityId;
    public long EditingUserId { get; } = editingUserId;
    private string SerializedEntityValue { get; set; } = serializedEntityValue;
    public string ConnectionErrorText { get; } = connectionErrorText;
    public LiveUpdateEventType EventType { get; set; } = eventType;
    public string ConnectionId { get; set; } = connectionId;
    
    public TEntity GetDeserializedEntityValue<TEntity>() where TEntity : IId
    {
        return JsonSerializer.Deserialize<TEntity>(SerializedEntityValue);
    }
}