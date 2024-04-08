using System;
using System.Collections.Generic;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models
{
    public class WorklogTableEntry
    {
        public long WorklogId { get; set; }

        public long StoryId { get; set; }

        public long TaskId { get; set; }

        public string StoryName { get; set; }

        public string TaskName { get; set; }

        public TimeSpan OriginalEstimate { get; set; }

        public TimeSpan CurrentEstimate { get; set; }

        public TimeSpan TimeSpent { get; set; }

        public TimeSpan TimeRemaining { get; set; }

        public TimeSpan TotalTimeSpent { get; set; }

        public List<User> Assignees { get; set; }

        public List<UserStoryTaskTag> TaskTags { get; set; }

        public List<WorklogTag> WorklogTags { get; set; }

        public List<IssueTag> IssueTags { get; set; }
        
        public DateTime Occurred { get; set; }

        public DateTime Created { get; set; }
        
        public string Description { get; set; }
    }
}