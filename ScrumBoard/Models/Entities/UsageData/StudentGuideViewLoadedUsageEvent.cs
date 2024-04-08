namespace ScrumBoard.Models.Entities.UsageData;

public class StudentGuideViewLoadedUsageEvent : ViewLoadedUsageEvent
{
    public string ContentFileName { get; set; }
    
    public StudentGuideViewLoadedUsageEvent(long userId, ViewLoadedUsageEventType eventType) : base(userId, eventType) { }
    
    /// <summary>
    /// Usage data event for viewing some file within the student guide. This student guide content is dynamic, and so
    /// we can only include the string of the filename that was requested.
    /// </summary>
    /// <param name="userId">ID of user who has viewed some student guide page</param>
    /// <param name="eventType">Corresponding event type enum</param>
    /// <param name="contentFileName">The name of the file that was requested</param>
    public StudentGuideViewLoadedUsageEvent(long userId, ViewLoadedUsageEventType eventType, string contentFileName) : this(userId, contentFileName)
    {
        LoadedViewUsageEventType = eventType;
    }
    
    // Private constructor to resolve EF Core issues with binding navigation properties from a constructor
    private StudentGuideViewLoadedUsageEvent(long userId, string contentFileName) : base(userId)
    {
        ContentFileName = contentFileName;
    }
}