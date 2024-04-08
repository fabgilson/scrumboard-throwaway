using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.Repositories.Changelog
{
    using TransformType = Func<IQueryable<UserStoryTaskChangelogEntry>, IQueryable<UserStoryTaskChangelogEntry>>;
    public interface IUserStoryTaskChangelogRepository : IRepository<UserStoryTaskChangelogEntry>
    {
        Task<List<UserStoryTaskChangelogEntry>> GetByUserStoryTaskAsync(UserStoryTask id, params TransformType[] transforms);

        Task<List<UserStoryTaskChangelogEntry>> GetByUserStoryTaskAndFieldAsync(UserStoryTask task, string field, params TransformType[] transforms);
        Task<UserStoryTaskChangelogEntry> GetByIdAsync(long id, params TransformType[] transforms);
    }

    public static class UserStoryTaskChangelogIncludes
    {
        /// <summary>
        /// Includes UserStoryTaskChangelogEntry.Creator
        /// </summary>
        public static readonly TransformType Creator = 
            query => query.Include(change => change.Creator);
        
        /// <summary>
        /// Includes UserStoryTaskChangelogEntry.UserStoryTaskChanged
        /// </summary>
        public static readonly TransformType TaskChanged = 
            query => query.Include(change => change.UserStoryTaskChanged);
        
        /// <summary>
        /// Includes UserTaskAssociationChangelogEntry.UserChanged
        /// </summary>
        public static readonly TransformType UserChanged = 
            query => query.Include(c => (c as UserTaskAssociationChangelogEntry).UserChanged);
        
        /// <summary>
        /// Includes UserStoryTaskTagChangelogEntry.UserStoryTaskTagChanged
        /// </summary>
        public static readonly TransformType TagChanged = query =>
            query.Include(c => (c as UserStoryTaskTagChangelogEntry).UserStoryTaskTagChanged);

        /// <summary>
        /// All the includes required to display any changelog entry
        /// </summary>
        public static readonly TransformType Display =
            new[] {Creator, UserChanged, TagChanged}
                .Aggregate((a, b) => query => a(b(query)));
    }

    /// <summary>
    /// Repository for UserStoryTaskChangelogEntry
    /// </summary>
    public class UserStoryTaskChangelogRepository : Repository<UserStoryTaskChangelogEntry>, IUserStoryTaskChangelogRepository
    {
        public UserStoryTaskChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<UserStoryTaskChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }
       
        /// <summary>
        /// Finds all the changelog entries for the given user story task ordered by when the occurred
        /// </summary>
        /// <param name="task">User story task to find changes for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given user story task</returns>
        public async Task<List<UserStoryTaskChangelogEntry>> GetByUserStoryTaskAsync(UserStoryTask task, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();            
            return await GetBaseQuery(context, transforms)                    
                .Where(entry => entry.UserStoryTaskChangedId == task.Id)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();
        }

        /// <summary>
        /// Finds all the changelog entries for the given field on the given user story task ordered by when the occurred
        /// </summary>
        /// <param name="task">User story task to find changes for</param>
        /// <param name="field">User story task field to filter changes by</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given user story task and field</returns>
        public async Task<List<UserStoryTaskChangelogEntry>> GetByUserStoryTaskAndFieldAsync(UserStoryTask task, string field, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.UserStoryTaskChangelogEntries
                .Where(entry => entry.UserStoryTaskChangedId == task.Id && entry.FieldChanged == field)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a user story task changelog entry by its Id, returns null if no sprint with the id exists
        /// </summary>
        /// <param name="id">User story task changelog entry key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User story task changelog entry with the given Id if it exists, otherwise null</returns>
        public async Task<UserStoryTaskChangelogEntry> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(change => change.Id == id)
                .SingleOrDefaultAsync();
        }
    }
}