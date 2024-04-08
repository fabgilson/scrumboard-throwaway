namespace ScrumBoard.Tests.Util.LiveUpdating;

public enum LiveUpdateEventType
{
    ConnectionError,
    ConnectionSuccess,
    EntityUpdated,
    EntityHasChanged,
    EditingBegunOnEntity,
    EditingEndedOnEntity
}