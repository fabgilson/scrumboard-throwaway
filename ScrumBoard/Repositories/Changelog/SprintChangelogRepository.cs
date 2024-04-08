using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.Repositories.Changelog
{
    using TransformType = Func<IQueryable<SprintChangelogEntry>, IQueryable<SprintChangelogEntry>>;
    public interface ISprintChangelogRepository : IRepository<SprintChangelogEntry>
    {
        Task<List<SprintChangelogEntry>> GetBySprintAsync(Sprint sprint, params TransformType[] transforms);
    }

    public static class SprintChangelogIncludes
    {
        /// <summary>
        /// Includes SprintChangelogEntry.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(changelog => changelog.Creator); 
        
        /// <summary>
        /// Includes SprintChangelogEntry.UserStory
        /// </summary>
        public static readonly TransformType UserStory = query => query.Include(changelog => (changelog as SprintStoryAssociationChangelogEntry).UserStoryChanged);   
    }

    /// <summary>
    /// Repository for SprintChangelogEntry
    /// </summary>
    public class SprintChangelogRepository : Repository<SprintChangelogEntry>, ISprintChangelogRepository
    {
        public SprintChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<SprintChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }    

        /// <summary>
        /// Finds all the changelog entries for the given sprint ordered by when the occurred
        /// </summary>
        /// <param name="sprint">Sprint to find changes for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given sprint</returns>
        public async Task<List<SprintChangelogEntry>> GetBySprintAsync(Sprint sprint, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.SprintChangedId == sprint.Id)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();      
        }
        
    }
}