using System;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Statistics;

public enum ReportType
{
    BurnDown = 1, 
    FlowDiagram = 2,
    WorkLog = 3, 
    ProjectStatistics = 4, 
    MyStatistics = 5,
    MyWeeklyReflections = 6,
    MarkingStats = 7,
}

public static class ReportTypeUtils
{
    public static ReportType[] GetAllowedReportTypesForRole(ProjectRole role)
    {
        return role switch
        {
            ProjectRole.Guest => new[] {ReportType.BurnDown, ReportType.FlowDiagram, ReportType.WorkLog},
            ProjectRole.Reviewer => new[] {ReportType.BurnDown, ReportType.FlowDiagram, ReportType.WorkLog},
            ProjectRole.Developer => new[] {ReportType.BurnDown, ReportType.FlowDiagram, ReportType.WorkLog, ReportType.MyStatistics, ReportType.MyWeeklyReflections},
            ProjectRole.Leader => Enum.GetValues<ReportType>(), // Leaders can look at all reports
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, $"Allowed report types not defined for {role}")
        };
    }
}
