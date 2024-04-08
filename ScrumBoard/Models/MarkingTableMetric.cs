using System.ComponentModel;
using EnumsNET;

namespace ScrumBoard.Models;

public enum MarkingTableMetric
{
    [Description("Overhead")]
    Overhead,
    [Description("Story hours")]
    StoryHours,
    [Description("Test hours")]
    TestHours,
    [Description("Average work log duration")]
    AvgLogDuration,
    [Description("Shortest work log duration")]
    ShortestWorklogDuration
}

public static class MarkingTableMetricExtensions
{
    public static string GetName(this MarkingTableMetric metric)
    {
        return metric.AsString(EnumFormat.Description);
    }
}
