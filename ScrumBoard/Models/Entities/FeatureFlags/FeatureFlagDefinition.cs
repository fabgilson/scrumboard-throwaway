using System.ComponentModel;

namespace ScrumBoard.Models.Entities.FeatureFlags;

public enum FeatureFlagDefinition
{
    None = 0,
    
    [Description("Allows team to keep a schedule of Daily Scrums, including reminders for upcoming meetings.")]
    StandUpMeetingSchedule = 1,
    
    [Description("Enables a weekly reflection check-in prompt, where students are prompted to reflect on their work " +
                 "within a given week.")]
    WeeklyReflectionCheckIn = 2,
    
    [Description("Enables an additional reporting view where users can view statistics about their own weekly reflections.")]
    WeeklyReflectionCheckInReportPage = 3,
    
    [Description("Extends weekly reflections by including the tasks a user has worked on during the week, and asking " +
                 "the user to select a difficulty and completion status for the task. Also adds some additional " +
                 "statistics to the 'My Reflections' report for users to track their difficulty distribution")]
    WeeklyReflectionTaskCheckIns = 4,
}