using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Repositories;

namespace ScrumBoard.Services
{
    public interface IProjectStatsService
    {
        /// <summary>
        /// Gets the total time logged for each user in the project, for the entire project.
        /// </summary>
        /// <param name="projectId">The ID of the project to get time logged for each user from</param>
        /// <param name="sprintId">Optional, if given will filter to just the time logged in this sprint</param>
        /// <returns>List of datasets, with name of user and total time logged as data</returns>
        Task<StatsBar> GetTimePerUser(long projectId, long? sprintId=null);
        
        /// <summary>
        /// Gets the number of stories worked on for each member of the given project in the whole project, optionally
        /// filtered to a single sprint.
        /// </summary>
        /// <param name="projectId">The ID of the project from which to get counts of stories worked on by users</param>
        /// <param name="sprintId">Optional, if given will limit stories worked on to just this sprint.</param>
        /// <returns>A list of datasets with the name of each user and the number of stories worked on in the project</returns>
        Task<StatsBar> GetStoriesWorkedPerUser(long projectId, long? sprintId=null);
        
        /// <summary>
        /// Gets the number of tasks worked on for each member of the given project in the whole project, optionally
        /// filtered to a single sprint.
        /// </summary>
        /// <param name="projectId">The ID of the project from which to get counts of tasks worked on by users</param>
        /// <param name="sprintId">Optional, if given will limit stories worked on to just this sprint.</param>
        /// <returns>A list of datasets with the name of each user and the number of tasks worked on in the project</returns>
        Task<StatsBar> GetTasksWorkedOnPerUser(long projectId, long? sprintId=null);
        
        Task<StatsBar> GetStatsBarForStoriesWithReviewedTaskPerUser(long projectId, long? sprintId=null);

        Task<ICollection<UserStoryTask>> GetTasksReviewedInProject(long projectId, long? userId=null, long? sprintId=null);
        Task<ICollection<UserStory>> GetUserStoriesReviewedInProject(long projectId, long? userId=null, long? sprintId=null);
    }

    public class ProjectStatsService : IProjectStatsService
    {
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

        public ProjectStatsService(IDbContextFactory<DatabaseContext> dbContextFactory) 
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Gets the unique index number of a user within a project. 
        /// This is stored in the stat segment to identify each user on the stats page.
        /// </summary>
        /// <param name="userId">ID of a user</param>
        /// <param name="project">Project the given user belongs to</param>
        /// <returns>The index number of the user given by the ID, within the project</returns>
        private static long GetInProjectId(long userId, Project project)
        {
            return project.GetWorkingMembers().OrderBy(u => u.Id).ToList().FindIndex(m => m.Id.Equals(userId));
        }
        
        public async Task<ICollection<UserStoryTask>> GetTasksReviewedInProject(long projectId, long? userId=null, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var entries = await context.WorklogEntries
                .Where(w => w.Task.UserStory.ProjectId == projectId)
                .Where(w => sprintId == null || w.Task.UserStory.StoryGroupId == sprintId)
                .Where(w => userId == null || w.UserId == userId)
                .Include(w => w.TaggedWorkInstances)
                .Include(w => w.Task).ThenInclude(t => t.UserStory)
                .ToListAsync();
            return entries
                .Where(w => w.GetWorkedTags().Any(t => t.Name == "Review"))
                .Select(w => w.Task).DistinctBy(t => t.Id).ToList();
        }
        
        public async Task<ICollection<UserStory>> GetUserStoriesReviewedInProject(long projectId, long? userId=null, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var tasksReviewed = await GetTasksReviewedInProject(projectId, userId, sprintId);
            
            return await context.UserStories
                .Where(s => tasksReviewed.Select(x => x.UserStoryId).Contains(s.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Gets the number of stories for each member of the given project in the whole project, 
        /// where each story related to a user contains at least one task that is reviewed by that user. 
        /// </summary>
        /// <param name="projectId">The Id of the project to retrieve both users and data from</param>
        /// <param name="sprintId">Optional, if present will limit filter to only stories with given sprint</param>
        /// <returns>
        /// A list of datasets with the name, ID of each user and the number of stories each member 
        /// reviewed a task from.
        /// </returns>
        public async Task<StatsBar> GetStatsBarForStoriesWithReviewedTaskPerUser(long projectId, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var project = await context.Projects.FirstAsync(x => x.Id == projectId);
            
            List<ProgressBarChartSegment<double>> datasets = new();
            
            foreach (var user in project.GetWorkingMembers())
            {
                var storiesReviewed = await GetUserStoriesReviewedInProject(projectId, user.Id, sprintId);
                var dataset = new ProgressBarChartSegment<double>(GetInProjectId(user.Id, project), user.GetFullName(), storiesReviewed.Count);
                datasets.Add(dataset);
            }

            var allReviewedStories = await GetUserStoriesReviewedInProject(projectId, sprintId: sprintId);
            return new StatsBar(datasets.OrderBy(d => d.Data).ToList(), allReviewedStories.Count);
        }
        
        public async Task<StatsBar> GetStoriesWorkedPerUser(long projectId, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var project = await context.Projects.FirstAsync(x => x.Id == projectId);
            
            var allTaggedWorkInstances = await context.TaggedWorkInstances
                .Where(x => x.WorklogEntry.Task.UserStory.ProjectId == projectId)
                .Where(x => sprintId == null || x.WorklogEntry.Task.UserStory.StoryGroupId == sprintId)
                .Include(twi => twi.WorklogEntry)
                .ThenInclude(we => we.Task)
                .ThenInclude(ust => ust.UserStory)
                .ToListAsync();

            var storyCountsPerUser = allTaggedWorkInstances
                .GroupBy(x => x.WorklogEntry.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    StoryCount = group.Select(x => x.WorklogEntry.Task.UserStory).Distinct().Count()
                });

            var datasets = project.GetWorkingMembers().Select(user =>
                new ProgressBarChartSegment<double>(
                    GetInProjectId(user.Id, project),
                    user.GetFullName(),
                    storyCountsPerUser.FirstOrDefault(x => x.UserId == user.Id)?.StoryCount ?? 0))
                .ToList();
            
            var total = datasets.Sum(dataset => dataset.Data);
            return new StatsBar(datasets.OrderBy(d => d.Data).ToList(), total);
        }
        
        public async Task<StatsBar> GetTasksWorkedOnPerUser(long projectId, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var project = await context.Projects.FirstAsync(x => x.Id == projectId);
            
            var allTaggedWorkInstances = await context.TaggedWorkInstances
                .Where(x => x.WorklogEntry.Task.UserStory.ProjectId == project.Id)
                .Where(x => sprintId == null || x.WorklogEntry.Task.UserStory.StoryGroupId == sprintId)
                .Include(taggedWorkInstance => taggedWorkInstance.WorklogEntry)
                .ToListAsync();

            var storyCountsPerUser = allTaggedWorkInstances
                .GroupBy(x => x.WorklogEntry.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    TaskCount = group.Select(x => x.WorklogEntry.Task).Distinct().Count()
                });
            
            var datasets = project.GetWorkingMembers().Select(user =>
                    new ProgressBarChartSegment<double>(
                        GetInProjectId(user.Id, project),
                        user.GetFullName(),
                        storyCountsPerUser.FirstOrDefault(x => x.UserId == user.Id)?.TaskCount ?? 0))
                .ToList();
            
            var total = datasets.Sum(dataset => dataset.Data);
            return new StatsBar(datasets.OrderBy(d => d.Data).ToList(), total);
        }

        public async Task<StatsBar> GetTimePerUser(long projectId, long? sprintId=null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var project = await context.Projects.FirstAsync(x => x.Id == projectId);
            var taggedWorkInstances = await context.TaggedWorkInstances
                .Where(x => x.WorklogEntry.Task.UserStory.ProjectId == project.Id)
                .Where(x => sprintId == null || x.WorklogEntry.Task.UserStory.StoryGroupId == sprintId)
                .Include(taggedWorkInstance => taggedWorkInstance.WorklogEntry)
                .ToListAsync();
            
            var taggedWorkInstancesPerUser = taggedWorkInstances
                .GroupBy(x => x.WorklogEntry.UserId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var datasets = project.GetWorkingMembers().Select(user =>
            {
                var taggedWorkByUser = taggedWorkInstancesPerUser.GetValueOrDefault(user.Id) ?? new List<TaggedWorkInstance>();
                var totalTimeByUser = taggedWorkByUser.Sum(twi => twi.Duration);
                return new ProgressBarChartSegment<double>(GetInProjectId(user.Id, project), user.GetFullName(), totalTimeByUser.TotalHours ); 
            }).ToList();
            
            var total = datasets.Sum(dataset => dataset.Data);
            return new StatsBar(datasets.OrderBy(d => d.Data).ToList(), total);
        }
    }
}