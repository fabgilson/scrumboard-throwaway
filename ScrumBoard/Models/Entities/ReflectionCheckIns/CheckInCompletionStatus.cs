using System.ComponentModel;

namespace ScrumBoard.Models.Entities.ReflectionCheckIns;

public enum CheckInCompletionStatus
{
    [Description("not yet started")]
    NotYetStarted = 0,
    [Description("is in progress")]
    Incomplete = 1,
    [Description("has been completed")]
    Completed = 2
}