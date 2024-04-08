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
    using TransformType = Func<IQueryable<WorklogTag>, IQueryable<WorklogTag>>;
    public interface IWorklogTagRepository : IRepository<WorklogTag>
    {
        Task<WorklogTag> GetByIdAsync(long id, params TransformType[] transforms);
        Task<WorklogTag> GetByNameAsync(string name, params TransformType[] transforms);
        Task<List<WorklogTag>> GetByWorklogEntryAsync(WorklogEntry entry, params TransformType[] transforms);
    }

    /// <summary>
    /// Repository for WorklogTag
    /// </summary>
    public class WorklogTagRepository : Repository<WorklogTag>, IWorklogTagRepository
    {
        public WorklogTagRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<WorklogTagRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a worklog tag by its Id, returns null if no worklog tag with the id exists
        /// </summary>
        /// <param name="id">Worklog tag key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Worklog tag with the given Id if it exists, otherwise null</returns>
        public async Task<WorklogTag> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(tag => tag.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets a worklog tag by its Name (case sensitive), returns null if no worklog tag with the name exists
        /// </summary>
        /// <param name="name">Worklog tag name to find (case sensitive)</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Worklog tag with the given Name if it exists, otherwise null</returns>
        public async Task<WorklogTag> GetByNameAsync(string name, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(tag => tag.Name == name)
                .SingleOrDefaultAsync();
        }
        
        /// <summary>
        /// Finds all the tags that are associated with the given worklog entry
        /// </summary>
        /// <param name="entry">Worklog entry to find tags for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of tags for the given worklog entry</returns>
        public async Task<List<WorklogTag>> GetByWorklogEntryAsync(WorklogEntry entry, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.WorklogEntries.Where(x => x.Id == entry.Id)
                .SelectMany(x => x.TaggedWorkInstances.Select(twi => twi.WorklogTag))
                .ToListAsync();
        }
    }
}