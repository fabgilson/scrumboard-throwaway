namespace ScrumBoard.Models.Entities.UsageData
{
    public class ProjectViewLoadedUsageEvent : ViewLoadedUsageEvent
    {
        public long ProjectId { get; set; }

        public long? ResourceId { get; set; }

        public ProjectViewLoadedUsageEvent(long userId, ViewLoadedUsageEventType eventType) : base(userId, eventType) { }

        /// <summary>
        /// ViewLoadedUsageEvent that has occurred within a particular project.
        /// </summary>
        /// <param name="userId">Id of user that has performed action generating the usage event</param>
        /// <param name="eventType">The type of the usage event in question</param>
        /// <param name="projectId">The project id this event pertains to</param>
        /// <param name="resourceId">The ID of the resource being viewed, if any (e.g UserStory.Id, WorklogEntry.Id, etc.)</param>
        public ProjectViewLoadedUsageEvent(long userId, ViewLoadedUsageEventType eventType, long projectId, long? resourceId=null) : this(userId, projectId, resourceId) 
        { 
            LoadedViewUsageEventType = eventType;
        }

        // Private constructor to resolve EF Core issues with binding navigation properties from a constructor
        private ProjectViewLoadedUsageEvent(long userId, long projectId, long? resourceId=null) : base(userId)
        {
            ProjectId = projectId;
            ResourceId = resourceId; 
        }
    }
}