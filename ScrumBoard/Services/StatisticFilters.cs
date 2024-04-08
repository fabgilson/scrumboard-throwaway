using System;
using System.Linq;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Services
{
    public static class StatisticFilters
    {   
        /// <summary>
        /// Filters for stories where the user has logged work on a task (excluding review and document only work-logs)
        /// Note that this is somewhat brittle as the only way to filter for tags in a meaningful way is by name,
        /// which is set in the seed data service.
        /// </summary>
        /// <param name="user">The user to filter by</param>
        /// <returns>The given query with transforms applied</returns>
        public static Func<IQueryable<UserStory>, IQueryable<UserStory>> StoriesWorkedByUser(User user)
        {
            return filter => filter.Where(story => story.Tasks.Any(
                task => task.Worklog.Any(
                    entry => entry.UserId == user.Id 
                        && entry.TaggedWorkInstances.Any(x => x.WorklogTag.Name != "Review" && x.WorklogTag.Name != "Document")
                    )
                )
            );
        }

        public static Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>> TasksWorkedByUser(User user)
        {
            return filter => filter.Where(task => task.Worklog.Any(entry => entry.UserId == user.Id));
        }

        public static Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>> TasksReviewedByUser(User user)
        {
            return filter => filter
                .Where(task => task.Worklog.Any(w => w.UserId == user.Id && w.GetWorkedTags().Any(tag => tag.Name == "Review")));
        }

        public static Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>> TasksCommitted(Project project)
        {
            return filter => filter.Where(task => task.UserStory.StoryGroupId != project.Id);
        }

        public static Func<IQueryable<UserStory>, IQueryable<UserStory>> StoriesCommitted(Project project)
        {
            return filter => filter.Where(story => story.StoryGroupId != project.Id);
        }
    }
}