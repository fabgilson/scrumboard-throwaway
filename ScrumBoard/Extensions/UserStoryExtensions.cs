using System;
using System.Linq;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Extensions
{
    public static class UserStoryExtensions
    {
        ///<summary>Gets the total time estimate for the story</summary>
        ///<returns>Estimated time</returns>
        public static TimeSpan GetDurationEstimate(this UserStory story) => story.Tasks.Sum(task => task.Estimate);
        
        /// <summary>
        /// Gets the number of completed tasks in the story 
        /// </summary>
        /// <returns>Returns the number of completed tasks in the story</returns>
        public static int GetCompletedTasksCount(this UserStory story) {
            return story.Tasks.Where(task => task.Stage == Stage.Done).Count();
        }

        /// <summary>
        /// Gets the number of tasks not completed in the story. Ignores deferred tasks.
        /// </summary>
        /// <returns>Returns the number of completed tasks in the story</returns>
        public static int GetNotCompletedTasksCount(this UserStory story) {
            return story.Tasks.Where(task => task.Stage != Stage.Done && task.Stage != Stage.Deferred).Count();
        }

        /// <summary>
        /// Gets the number of deferred tasks in the story.
        /// </summary>
        /// <returns>Returns the number of deferred tasks in the story</returns>
        public static int GetDeferredTasksCount(this UserStory story) {
            return story.Tasks.Where(task => task.Stage == Stage.Deferred).Count();
        }

        /// <summary>
        /// Gets the story completion rate. Defined as the ratio between the total estimated time for all tasks
        /// versus the total estimated time for completed tasks. Ignores deferred tasks.
        /// </summary>
        /// <returns>Returns the story completion rate as a percentage</returns>
        public static double GetStoryCompletionRate(this UserStory story) {
            TimeSpan totalEstimate = story.Tasks.Where(task => task.Stage != Stage.Deferred).Sum(task => task.Estimate);
            TimeSpan totalCompletedEstimate = story.Tasks.Where(task => task.Stage == Stage.Done).Sum(task => task.Estimate);
            if (totalEstimate != TimeSpan.Zero) {
                double completionRate = (totalCompletedEstimate / totalEstimate) * 100;            
                return Math.Round(completionRate);
            } else {
                return 0;
            }            
        }
        
        /// <summary>
        /// Gets the sprint that owns this story, if present otherwise null
        /// </summary>
        public static Sprint GetSprint(this UserStory story) {
            if (story.StoryGroup is Sprint sprint) return sprint;
            return null;            
        }
    }
}