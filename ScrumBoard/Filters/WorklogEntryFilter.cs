using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Filters
{
    public class WorklogEntryFilter
    {
        public ICollection<UserStoryTaskTag> TaskTagsFilter { get; set; } = new List<UserStoryTaskTag>();

        public ICollection<WorklogTag> WorklogTagsFilter { get; set; } = new List<WorklogTag>();

        public ICollection<User> AssigneeFilter { get; set; } = new List<User>();

        public bool FilterEnabled => TaskTagsFilter.Any() || WorklogTagsFilter.Any() || AssigneeFilter.Any() || DateRangeFilterEnabled;

        public bool AssigneeFilterEnabled => AssigneeFilter.Any();

        public bool DateRangeFilterEnabled = false;

        public bool IncludePairAssigneeEnabled = false;

        public DateOnly DateRangeStart = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));

        public DateOnly DateRangeEnd = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// A function passed into worklog table as a predicate. If for the given worklog entry returns true, 
        /// the worklog entry will be included in the table, based on the filter contents. 
        /// Otherwise, the entry will not be included in the table.
        /// </summary>
        /// <param name="entry">Instance of worklog table entry to be filtered or not</param>
        /// <returns>true if the parameter passes the filters, otherwise false</returns>
        public Expression<Func<WorklogEntry, bool>> Predicate {
            get
            {
                var taskTagIds = TaskTagsFilter.Select(tag => tag.Id).ToList();
                var worklogTagIds = WorklogTagsFilter.Select(tag => tag.Id).ToList(); 
                var startTime = DateRangeStart.ToDateTime(TimeOnly.MinValue);
                var endTime = DateRangeEnd.ToDateTime(TimeOnly.MaxValue);          
                return entry => 
                    (!AssigneeFilter.Any() || AssigneeFilter.Contains(entry.User) || (IncludePairAssigneeEnabled && AssigneeFilter.Contains(entry.PairUser))) && 
                    (!taskTagIds.Any() || entry.Task.Tags.Any(tag => taskTagIds.Contains(tag.Id))) && 
                    (!worklogTagIds.Any() || entry.TaggedWorkInstances.Any(tag => worklogTagIds.Contains(tag.WorklogTagId))) &&
                    (!DateRangeFilterEnabled || (entry.Occurred >= startTime && entry.Occurred <= endTime));
                }
        }

        public void ClearFilters() {
            AssigneeFilter.Clear();
            TaskTagsFilter.Clear();
            WorklogTagsFilter.Clear();
            DateRangeFilterEnabled = false;
        }

        public void AssigneeFilterChanged(ICollection<User> updatedFilter)
        {
            if (!updatedFilter.Any())
            {
                IncludePairAssigneeEnabled = false;
            }
            AssigneeFilter = updatedFilter;
        }
    }
}