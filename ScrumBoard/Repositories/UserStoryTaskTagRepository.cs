using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<UserStoryTaskTag>, IQueryable<UserStoryTaskTag>>;
    public interface IUserStoryTaskTagRepository : IRepository<UserStoryTaskTag>
    {
        Task<UserStoryTaskTag> GetByIdAsync(long id, params TransformType[] transforms);
        Task<UserStoryTaskTag> GetByNameAsync(string name, params TransformType[] transforms);
        Task<List<UserStoryTaskTag>> GetByTaskAsync(UserStoryTask task, params TransformType[] transforms);
    }

    /// <summary>
    /// Repository for UserStoryTaskTag
    /// </summary>
    public class UserStoryTaskTagRepository : Repository<UserStoryTaskTag>, IUserStoryTaskTagRepository
    {
        public UserStoryTaskTagRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<UserStoryTaskTagRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a user story task tag by its Id, returns null if no user story task tag with the id exists
        /// </summary>
        /// <param name="id">User story task tag key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Tag with the given Id if it exists, otherwise null</returns>
        public async Task<UserStoryTaskTag> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(tag => tag.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets the user story task tag with the given name (case sensitive)
        /// </summary>
        /// <param name="name">Name of tag to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Tag with the given name if it exists, otherwise null</returns>
        public async Task<UserStoryTaskTag> GetByNameAsync(string name, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(tag => tag.Name == name)
                .SingleOrDefaultAsync();
        }
        
        /// <summary>
        /// Finds all the tags that are associated with the given user story task
        /// </summary>
        /// <param name="task">Task to find tags for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of tags for the given task</returns>
        public async Task<List<UserStoryTaskTag>> GetByTaskAsync(UserStoryTask task, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(tag => tag.Tasks.Contains(task))
                .ToListAsync();
        }
    }
}