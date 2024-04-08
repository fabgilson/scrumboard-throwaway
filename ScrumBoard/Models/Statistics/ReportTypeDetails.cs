using System.Collections.Generic;

namespace ScrumBoard.Models.Statistics
{
    public static class ReportTypeDetails
    {
        public static readonly Dictionary<ReportType, string> ReportTypeDescriptions = new()
        {
            [ReportType.BurnDown]           = "Burn-down chart",
            [ReportType.FlowDiagram]        = "Flow Diagram",
            [ReportType.MyStatistics]       = "My Statistics",
            [ReportType.ProjectStatistics]  = "Project Statistics",
            [ReportType.WorkLog]            = "Work Log",
            [ReportType.MyWeeklyReflections]  = "My Reflections",
            [ReportType.MarkingStats]       = "Marking Summary"
        };

        public static readonly Dictionary<ReportType, string> ReportTypeIcons = new()
        {
            [ReportType.BurnDown]           = "bi bi-graph-down me-3",
            [ReportType.FlowDiagram]        = "bi bi-graph-up me-3",
            [ReportType.MyStatistics]       = "bi bi-person-lines-fill me-3",
            [ReportType.ProjectStatistics]  = "bi bi-bar-chart-line me-3",
            [ReportType.WorkLog]            = "bi bi-table me-3",
            [ReportType.MyWeeklyReflections]  = "bi bi-calendar-week me-3",
            [ReportType.MarkingStats]       = "bi bi-list-check me-3"
        };
    }
}