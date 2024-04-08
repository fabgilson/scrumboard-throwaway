using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<UserStory>, IQueryable<UserStory>>;
    public interface IUserStoryRepository : IRepository<UserStory>
    {
        Task<UserStory> GetByIdAsync(long id, params TransformType[] transforms);
        Task<List<UserStory>> GetByStoryGroupAsync(StoryGroup group, params TransformType[] transforms);
        Task<int> GetCountByStoryGroup(StoryGroup group, params TransformType[] transforms);
        Task<int> GetCountByProject(Project project, params TransformType[] transforms);
        new Task UpdateAsync(UserStory story);
        Task<List<UserStory>> GetAllByIdsAsync(List<long> ids, params TransformType[] transforms);
        Task<int> GetEstimateByStoryGroup(StoryGroup group, params TransformType[] transforms);
    }

    public static class UserStoryIncludes
    {
        /// <summary>
        /// Includes UserStory.AcceptanceCriteria and orders then by their InStoryId
        /// </summary>
        public static readonly TransformType AcceptanceCriteria = query => query.Include(story => story.AcceptanceCriterias.OrderBy(ac => ac.InStoryId));
        /// <summary>
        /// Includes UserStory.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(story => story.Creator);
        /// <summary>
        /// Includes all tasks within the story and their creators
        /// </summary>
        public static readonly TransformType Tasks = query => query.Include(story => story.Tasks).ThenInclude(task => task.Creator);
        /// <summary>
        /// Includes UserStory.StoryGroup
        /// </summary>
        public static readonly TransformType StoryGroup = query => query.Include(story => story.StoryGroup);

        /// <summary>
        /// All the includes required to display a story within a FullStory
        /// </summary>
        public static readonly TransformType Display = new[] {AcceptanceCriteria, Creator, Tasks, StoryGroup}
                .Aggregate((a, b) => query => a(b(query)));
    }
    
    /// <summary>
    /// Repository for UserStory
    /// </summary>
    public class UserStoryRepository : Repository<UserStory>, IUserStoryRepository
    {
        public UserStoryRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<UserStoryRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a user story by its Id, returns null if no user story with the id exists
        /// </summary>
        /// <param name="id">User story key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User story with the given Id if it exists, otherwise null</returns>
        public async Task<UserStory> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(story => story.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets all the user stories where their id is contained within the id list, the result may be in any order
        /// and if a user is not present in the db they are omitted from the results
        /// </summary>
        /// <param name="ids">Ids to search for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of user stories where every story has an Id within the provided id list</returns>
        public async Task<List<UserStory>> GetAllByIdsAsync(List<long> ids, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(story => ids.Contains(story.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Finds all the stories contained within the given StoryGroup in order
        /// </summary>
        /// <param name="group">Story group to find stories within</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, alternative sorts</param>
        /// <returns>List of user stories within the story group</returns>
        public async Task<List<UserStory>> GetByStoryGroupAsync(StoryGroup group, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            TransformType defaultOrdering = query => query.OrderBy(story => story.Order);
            return await GetBaseQuery(context, new []{ defaultOrdering }.Concat(transforms))
                .Where(story => story.StoryGroupId == group.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Finds the number of stories within the given project
        /// </summary>
        /// <param name="project">Project to request number of stories for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Number of stories within the project</returns>
        public async Task<int> GetCountByProject(Project project, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(story => story.ProjectId == project.Id)
                .CountAsync();
        }

        /// <summary>
        /// Finds the number of stories within the given story group
        /// </summary>
        /// <param name="group">Story group to request number of stories for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Number of stories within the story group</returns>
        public async Task<int> GetCountByStoryGroup(StoryGroup group, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(story => story.StoryGroupId == group.Id)
                .CountAsync();
        }

        /// <summary>
        /// Finds the total number of story points within the given story group
        /// </summary>
        /// <param name="group">Story group to request total point estimate for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Total point estimate within this story group</returns>
        public async Task<int> GetEstimateByStoryGroup(StoryGroup group, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(story => story.StoryGroupId == group.Id)
                .SumAsync(story => story.Estimate);
        }

        public override async Task UpdateAsync(UserStory story)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.BeginTransactionAsync();
            context.Update(story);
            if (story.AcceptanceCriterias != null)
                DeleteLeftoverChildren(context, story, story.AcceptanceCriterias, ac => ac.UserStory);
            await context.SaveChangesAsync();
            await context.Database.CommitTransactionAsync();
            
            await UpdateRowVersion(context, story.Id);
            var updatedStory = await GetByIdAsync(story.Id);
            story.RowVersion = updatedStory.RowVersion;
        }
    }
}