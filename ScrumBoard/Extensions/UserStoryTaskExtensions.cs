using System.Collections.Generic;
using System.Linq;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Extensions
{
    public static class UserStoryTaskExtensions
    {
        /// <summary>
        /// Retrieves all of the given task's assigned users
        /// </summary>
        /// <returns>All of the task's UserStoryTask instances in their AssignedAssociations attribute</returns>
        public static List<User> GetAssignedUsers(this UserStoryTask task)
        {
            return task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Assigned)).Select(a => a.User).ToList();
        }

        /// <summary>
        /// Retrieves all of the given task's users that are reviewing it
        /// </summary>
        /// <returns>All of the task's UserStoryTask instances in their UserAssociations attribute</returns>
        public static List<User> GetReviewingUsers(this UserStoryTask task)
        {
            return task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Reviewer)).Select(a => a.User).ToList();
        }

        ///<summary>Gets the sprint that owns this task, if present otherwise null </summary>
        public static Sprint GetSprint(this UserStoryTask task) {
            if (task.UserStory.StoryGroup is Sprint sprint) return sprint;
            return null;            
        }

        ///<summary>Gets the project that owns this task </summary>
        public static Project GetProject(this UserStoryTask task) => task.UserStory.Project;
    }
}