using ScrumBoard.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Extensions
{
    public static class StoryGroupExtensions
    {
        /// <summary>
        /// Adds a story to a group conserving order.
        /// </summary>
        public static void AddStory(this StoryGroup group, UserStory story)
        { 
            story.Order = group.Stories.Select(s => s.Order).DefaultIfEmpty(0).Max() + 1;
            group.Stories.Add(story);            
        }

        /// <summary>
        /// Replaces the current set of stories with a new set in order.
        /// </summary>
        public static void ReplaceStories(this StoryGroup group, List<UserStory> stories) {
            group.Stories.Clear();
            int i = 1;
            foreach (UserStory story in stories) {
                story.Order = i++;             
                group.Stories.Add(story);
            }
        }

        /// <summary>
        /// Removes a story from a group conserving order.
        /// </summary>
        public static void RemoveStory(this StoryGroup group, UserStory story)
        {            
            group.Stories.Remove(story);            
        }        

        /// <summary>
        /// Removes all given stories from a group conserving order.
        /// </summary>
        public static void RemoveStories(this StoryGroup group, List<UserStory> stories)
        {            
            foreach (UserStory story in stories) {
                group.Stories.Remove(story);
            }        
        }     

        /// <summary>
        /// Checks if the given StoryGroup is a Sprint.
        /// </summary>
        /// <returns>A boolean: true if the given group is a sprint, false if not</returns>
        public static bool IsSprint(this StoryGroup group) 
        {
            return typeof(Sprint).IsInstanceOfType(group);
        }
        
        public static IEnumerable<WorklogEntry> GetWorklogEntries(this StoryGroup group)
        {
            return group.Stories.SelectMany(s => s.Tasks).SelectMany(t => t.Worklog);
        }
    }
}