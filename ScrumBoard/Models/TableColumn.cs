using System;

namespace ScrumBoard.Models
{
    public enum TableColumn {
        Occurred,
        Created,
        StoryName,
        TaskName,
        OriginalEstimate,
        CurrentEstimate,
        TimeSpent,
        TimeRemaining,
        TotalTimeSpent,
        Description,
        Assignees,
        TaskTags, 
        WorklogTags,
        IssueTags
    };

    public static class TableColumnExtensions
    {
        public static string GetName(this TableColumn column)
        {
            return column switch {
                TableColumn.StoryName         => "Story Name",
                TableColumn.TaskName          => "Task Name",
                TableColumn.OriginalEstimate  => "Original Estimate",
                TableColumn.CurrentEstimate   => "Current Estimate",                
                TableColumn.TimeSpent         => "Time Spent",
                TableColumn.TimeRemaining     => "Time Remaining",
                TableColumn.TotalTimeSpent    => "Total Time Spent",
                TableColumn.Assignees         => "Assignees",
                TableColumn.TaskTags          => "Task Tags",         
                TableColumn.Occurred          => "Date Occurred", 
                TableColumn.Created           => "Date Created",
                TableColumn.WorklogTags       => "Worklog Tags",
                TableColumn.Description       => "Description",
                TableColumn.IssueTags         => "Issue Tags",
                _ => throw new ArgumentException($"Invalid enum value {column} for column", nameof(column)),
            };
        }

        public static bool IsOrderable(this TableColumn column)
        {
            return column is not TableColumn.TaskTags and not TableColumn.WorklogTags and not TableColumn.IssueTags;
        }
        
        /// <summary>
        /// Whether or not the table column should only be visible when viewing the worklog table in marking mode
        /// </summary>
        /// <param name="column">Column for which to determine whether it should only be shown for marking mode</param>
        /// <returns>True if the column should only be shown in marking mode, false otherwise</returns>
        public static bool IsForMarkingTableOnly(this TableColumn column)
        {
            return column switch
            {
                TableColumn.IssueTags => true,
                _ => false
            };
        }
    }
}