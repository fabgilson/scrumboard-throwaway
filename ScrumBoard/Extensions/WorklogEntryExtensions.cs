using System;
using System.Linq;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Extensions
{
    public static class WorklogEntryExtensions
    {
        /// <summary>
        /// Orderes the worklog entries by the given order column either ascending or descending
        /// <param name="sprint">The sprint to get worklogs for</param>
        /// <param name="sortColumn">The table column to sort by</param>
        /// <param name="descending">Boolean for whether to order by descending. If false orders by ascending</param>
        /// </summary>
        public static IQueryable<WorklogEntry> OrderWorklogEntries(this IQueryable<WorklogEntry> worklogEntries, TableColumn sortColumn, bool descending)
        {
            var ordered = sortColumn switch
            {
                TableColumn.StoryName =>
                    worklogEntries.OrderByWithDirection(w => w.Task.UserStory.Name, descending),
                TableColumn.TaskName => worklogEntries.OrderByWithDirection(w => w.Task.Name, descending),
                TableColumn.OriginalEstimate => worklogEntries.OrderByWithDirection(w => w.Task.OriginalEstimateTicks,
                    descending),
                TableColumn.CurrentEstimate => worklogEntries.OrderByWithDirection(w => w.Task.UserStory.Estimate,
                    descending),
                TableColumn.TimeSpent => worklogEntries.OrderByWithDirection(w => w.GetTotalTimeSpent(), descending),
                TableColumn.TimeRemaining => worklogEntries.OrderByWithDirection(
                    w => w.Task.EstimateTicks - w.Task.Worklog.Sum(x => x.GetTotalTimeSpent().Ticks), descending),
                TableColumn.TotalTimeSpent => worklogEntries.OrderByWithDirection(
                    w => w.Task.Worklog.Select(log => log.GetTotalTimeSpent()).Sum(), descending),
                TableColumn.Assignees => worklogEntries.OrderByWithDirection(w => w.User.FirstName, descending)
                    .ThenOrderByWithDirection(w => w.User.LastName, descending),
                TableColumn.Occurred => worklogEntries.OrderByWithDirection(w => w.Occurred, descending),
                TableColumn.Created => worklogEntries.OrderByWithDirection(w => w.Created, descending),
                TableColumn.Description => worklogEntries.OrderByWithDirection(w => w.Description, descending),
                _ => throw new InvalidOperationException($"Unknown table column: {sortColumn}")
            };

            // This is a workaround for some paginated requests not including many-to-many entities in ef core.
            // When ef core generates queries for many-to-many entities it performs multiple queries, one per each
            // joined entity. These sub-queries require knowing the entities returned by the main query. This is done by
            // including a simplified main query in all sub-queries, in this simplified query step there is a difference
            // in the generated "order by" clause that results in a different order of main entities in sub-queries.
            // Which due to pagination, results in sub-entities for different main entities being returned.
            // To fix this, all uncertainty in the specified order is removed which propagates into the sub-queries.
            return ordered.ThenBy(worklog => worklog.Id);
        }
    }
}