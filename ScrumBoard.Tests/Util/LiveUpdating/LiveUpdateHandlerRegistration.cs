using System;
using System.Threading.Tasks;
using Moq;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Tests.Util.LiveUpdating;

/// <summary>
/// Represents an instance of some live update handler being registered.
/// </summary>
/// <remarks>
/// Should not be used for tracking actual invocation of live update events, for that case
/// use <see cref="LiveUpdateEventInvocation"/>.
/// </remarks>
/// <param name="entityType">CLR type of entity for which to track updates, this should be the concrete type.</param>
/// <param name="entityId">ID of entity to track updates for.</param>
/// <param name="handler">Handler function to invoke on updates received.</param>
/// <param name="typeOfCallingClass">CLR type of class that is registering a listener.</param>
/// <param name="eventType">The type of update event occuring, see <see cref="LiveUpdateEventType"/></param>
public class LiveUpdateHandlerRegistration(
    Type entityType,
    long entityId,
    Delegate handler,
    Type typeOfCallingClass,
    LiveUpdateEventType eventType
) {
    public Type EntityType { get; } = entityType;
    public long EntityId { get; } = entityId;
    public Type CallingClassType { get; } = typeOfCallingClass;
    public LiveUpdateEventType EventType { get; } = eventType;
    
    public Func<TEntity, long, Task> GetTypedEntityUpdateHandler<TEntity>() where TEntity : IId
    {
        if (EventType is not LiveUpdateEventType.EntityUpdated) 
            throw new NotSupportedException();
        
        if (handler is Func<TEntity, long, Task> typedHandler) return typedHandler;
        throw new InvalidOperationException($"Handler type mismatch. Expected: Func<{typeof(TEntity).Name}, long, Task>.");
    }
    
    public Func<long, Task> GetEditingStatusChangedHandler()
    {
        if (EventType is not LiveUpdateEventType.EditingBegunOnEntity and not LiveUpdateEventType.EditingEndedOnEntity) 
            throw new NotSupportedException();
        
        if (handler is Func<long, Task> typedHandler) return typedHandler;
        throw new InvalidOperationException($"Handler type mismatch. Expected: Func<long, Task>.");
    }
    
    public Func<Task> GetEntityHasChangedHandler()
    {
        if (EventType is not LiveUpdateEventType.EntityHasChanged) 
            throw new NotSupportedException();
        
        if (handler is Func<Task> typedHandler) return typedHandler;
        throw new InvalidOperationException($"Handler type mismatch. Expected: Func<Task>.");
    }

    public static LiveUpdateHandlerRegistration FromInvocation(IInvocation invocation, LiveUpdateEventType eventType)
    {
        var entityType = invocation.Method.GetGenericArguments()[0];
        var entityId = (long)invocation.Arguments[0];
        var handler = (Delegate)invocation.Arguments[1];
        var callingClassType = (Type)invocation.Arguments[2];

        Delegate wrappedHandler = eventType switch
        {
            LiveUpdateEventType.EntityUpdated => (IId entity, long userId) => (Task)handler.DynamicInvoke(entity, userId),
            LiveUpdateEventType.EditingBegunOnEntity => (long editingUserId) => (Task)handler.DynamicInvoke(editingUserId),
            LiveUpdateEventType.EditingEndedOnEntity => (long editingUserId) => (Task)handler.DynamicInvoke(editingUserId),
            LiveUpdateEventType.EntityHasChanged => () => (Task)handler.DynamicInvoke(),
            _ => throw new InvalidOperationException("Unknown LiveUpdateRegistrationType.")
        };

        return new LiveUpdateHandlerRegistration(
            entityType,
            entityId,
            wrappedHandler,
            callingClassType,
            eventType
        );
    }
}
