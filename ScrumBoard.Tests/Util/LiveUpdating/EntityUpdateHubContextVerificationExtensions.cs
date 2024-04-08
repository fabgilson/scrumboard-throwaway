using System;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScrumBoard.LiveUpdating;

namespace ScrumBoard.Tests.Util.LiveUpdating;

public static class EntityUpdateHubContextVerificationExtensions
{
    /// <summary>
    /// Verifies that a broadcast for a new entity value has been sent correctly through the EntityUpdateHub.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being broadcast.</typeparam>
    /// <param name="hub">The mock hub context used for the EntityUpdateHub.</param>
    /// <param name="expectedEntityId">The expected ID of the entity involved in the broadcast.</param>
    /// <param name="expectedProjectId">The expected ProjectId of the entity involved in the broadcast.</param>
    /// <param name="expectedEditingUserId">The expected user ID of the user who edited the entity.</param>
    /// <param name="entityValueValidator">
    /// A function to validate the deserialized entity value.
    /// It should return true if the value meets the expected criteria.
    /// </param>
    /// <param name="times">
    /// Optional. Specifies how many times the broadcast is expected to have occurred.
    /// If not provided, the default is at least once.
    /// </param>
    /// <remarks>
    /// This extension method is used to verify that the EntityUpdateHub has sent a broadcast with the correct parameters.
    /// The 'entityValueValidator' function allows for custom validation logic to be applied to the deserialized entity object.
    /// </remarks>
    public static void VerifyCorrectBroadcastForNewEntityValue<TEntity>(
        this Mock<IHubContext<EntityUpdateHub>> hub,
        long expectedEntityId,
        long expectedProjectId,
        long expectedEditingUserId,
        Func<TEntity, bool> entityValueValidator,
        Func<Times> times=null
    ) {
        hub.Verify(x => x.Clients.All.SendCoreAsync(
            "ReceiveEntityUpdate",
            It.Is<object[]>(objects =>
                (string)objects[0] == typeof(TEntity).AssemblyQualifiedName
                && (long)objects[1] == expectedEntityId
                && entityValueValidator(
                    JsonSerializer.Deserialize<TEntity>(
                        (string)objects[2], 
                        (JsonSerializerOptions)null
                    ))
                && (long)objects[3] == expectedEditingUserId
            ),
            It.IsAny<CancellationToken>()
        ), times: times ?? Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that the 'StartedUpdatingEntity' broadcast has been sent correctly through the EntityUpdateHub.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="hub">The mock hub context used for the EntityUpdateHub.</param>
    /// <param name="expectedEntityId">The expected ID of the entity being updated.</param>
    /// <param name="expectedProjectId">The expected ProjectId of the entity being updated.</param>
    /// <param name="expectedEditingUserId">The expected user ID of the user who started updating the entity.</param>
    /// <param name="times">Optional. Specifies how many times the broadcast is expected to have occurred. If not provided, the default is at least once.</param>
    /// <remarks>
    /// This method is used to verify that the EntityUpdateHub has sent a 'StartedUpdatingEntity' broadcast with the correct parameters.
    /// </remarks>
    public static void VerifyBroadcastUpdateStartedOnEntity<TEntity>(
        this Mock<IHubContext<EntityUpdateHub>> hub,
        long expectedEntityId,
        long expectedProjectId,
        long expectedEditingUserId,
        Func<Times> times = null
    ) {
        hub.Verify(x => x.Clients.Group($"Project_{expectedProjectId}").SendCoreAsync(
            "StartedUpdatingEntity",
            It.Is<object[]>(objects =>
                (string)objects[0] == typeof(TEntity).AssemblyQualifiedName
                && (long)objects[1] == expectedEntityId
                && (long)objects[2] == expectedEditingUserId
            ),
            It.IsAny<CancellationToken>()
        ), times: times ?? Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that the 'StoppedUpdatingEntity' broadcast has been sent correctly through the EntityUpdateHub.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="hub">The mock hub context used for the EntityUpdateHub.</param>
    /// <param name="expectedEntityId">The expected ID of the entity that was being updated.</param>
    /// <param name="expectedProjectId">The expected ProjectId of the entity being updated.</param>
    /// <param name="expectedEditingUserId">The expected user ID of the user who ended updating the entity.</param>
    /// <param name="times">Optional. Specifies how many times the broadcast is expected to have occurred. If not provided, the default is at least once.</param>
    /// <remarks>
    /// This method is used to verify that the EntityUpdateHub has sent a 'StoppedUpdatingEntity' broadcast with the correct parameters.
    /// </remarks>
    public static void VerifyBroadcastUpdateEndedOnEntity<TEntity>(
        this Mock<IHubContext<EntityUpdateHub>> hub,
        long expectedEntityId,
        long expectedProjectId,
        long expectedEditingUserId,
        Func<Times> times = null
    ) {
        hub.Verify(x => x.Clients.Group($"Project_{expectedProjectId}").SendCoreAsync(
            "StoppedUpdatingEntity",
            It.Is<object[]>(objects =>
                (string)objects[0] == typeof(TEntity).AssemblyQualifiedName
                && (long)objects[1] == expectedEntityId
                && (long)objects[2] == expectedEditingUserId
            ),
            It.IsAny<CancellationToken>()
        ), times: times ?? Times.AtLeastOnce);
    }

}