namespace ScrumBoard.Models.Entities.UsageData
{
    public enum ViewLoadedUsageEventType : int
    {
        RootHomepage = 100,
        StudentGuide = 110,

        // Projects
        ProjectHomepage = 200,
        EditProject = 210,
        ProjectChangelog = 220,

        // Other pages
        SprintBoard = 300,
        Backlog = 400,
        Ceremonies = 500, // ResourceId = Sprint.Id
        SprintReview = 600, // ResourceId = Sprint.Id

        // Reports
        ProjectStatistics = 700, // ResourceId = Sprint.Id, or -1 for whole project
        CumulativeFlowDiagram = 710, // ResourceId = Sprint.Id, or -1 for whole project
        WorklogReport = 720, // ResourceId = Sprint.Id, or -1 for whole project
        BurndownChart = 730, // ResourceId = Sprint.Id
        MyStatistics = 740, // ResourceId = Sprint.Id, or -1 for whole project
        MyWeeklyReflectionCheckIns = 750, // ResourceId = Sprint.Id
        MarkingSummary = 760, // ResourceId = Sprint.Id or -1 for whole project 

        // Scrum artefacts
        UserStory = 800, // ResourceId = Story.Id
        UserStoryEditForm = 801, // ResourceId = Story.Id, or -1 if creating a new story
        UserStoryTask = 810, // ResourceId = Task.Id, or -1 if creating a new task
        WorklogEntryView = 820, // ResourceId = Worklog.Id
        WorklogEntryEditForm = 821, // ResourceId = Worklog.Id, or -1 if creating a new worklog entry
        
        // Stand-up views
        StandUpSchedule = 900,
        UpcomingStandUp = 902, // ResourceId = StandUpMeeting.Id, or -1 if there is no upcoming DSM found
        
        // Forms
        FormsPage = 1000, // Viewing the FillForms page
        FormInstance = 1001, // Viewing a single form instance. ResourceId = FormInstanceId
        
        // Weekly reflections
        PerformWeeklyReflectionsPage = 1100, // ResourceId = CheckIn.Id, or -1 if viewing an un-started check-in
    }

    /// <summary>
    /// Usage event for any time a view is loaded by a user. 
    /// 
    /// If the loaded view pertains to a particular resource within the system, the relevant 
    /// sub-class of this class should be used. E.g if a user story is viewed, the usage event 
    /// generated should use UserStoryViewLoadedUsageEvent instead.
    /// 
    /// A 'view' is typically one of the following:
    ///   * A page (e.g project home)
    ///   * A visualisation (e.g burn-down, flow diagrams, my statistics)
    ///   * A modal (e.g user story, task, worklog)
    /// </summary>
    public class ViewLoadedUsageEvent : BaseUsageDataEvent
    {
        public ViewLoadedUsageEventType LoadedViewUsageEventType { get; set; }

        public ViewLoadedUsageEvent(long userId) : base(userId) { }

        /// <summary>
        /// Constructor for creating a ViewLoadedUsageEvent for some user, with a given eventType,
        /// that may (or may not) pertain to a particular project.
        /// </summary>
        /// <param name="userId">Id of user that has performed action generating the usage event</param>
        /// <param name="eventType">The type of the usage event in question</param>
        /// <returns></returns>
        public ViewLoadedUsageEvent(long userId, ViewLoadedUsageEventType eventType) : base(userId)
        {
            LoadedViewUsageEventType = eventType;
        }
    }
}