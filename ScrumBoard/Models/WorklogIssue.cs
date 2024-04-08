using System.ComponentModel;
using EnumsNET;

namespace ScrumBoard.Models;

public enum WorklogIssue
{
    [Description("Missing commit")]
    MissingCommit,
    [Description("Short description")]
    ShortDescription,
    [Description("Too long")]
    TooLong,
    [Description("Short duration")]
    ShortDuration,
    [Description("Too many tags")]
    TooManyTags,
    [Description("Outside work hours")]
    OutsideWorkHours
}

public static class WorklogIssueExtensions
{
    public static string GetName(this WorklogIssue metric)
    {
        return metric.AsString(EnumFormat.Description);
    }
}