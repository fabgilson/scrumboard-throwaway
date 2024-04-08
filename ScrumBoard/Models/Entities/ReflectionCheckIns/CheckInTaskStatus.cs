using System.ComponentModel;

namespace ScrumBoard.Models.Entities.ReflectionCheckIns;

public enum CheckInTaskStatus
{
    None = 0,
    
    [Description("complete, and all changes have been merged")]
    Completed = 1,
    
    [Description("complete but not merged, because it is yet to be reviewed")]
    CompletedPendingReview = 2,
    
    [Description("not complete, but I will keep working on it")]
    UnfinishedContinueWorking = 3,
    
    [Description("not complete, but I will keep working on it but with someone to help")]
    UnfinishedAskForHelp = 4,
    
    [Description("not complete, it is too big and should be broken down into smaller tasks")]
    UnfinishedBreakDownToSmallerTasks = 5,
    
    [Description("not complete, and I will give it to someone else")]
    UnfinishedGiveAwayTask = 6
}