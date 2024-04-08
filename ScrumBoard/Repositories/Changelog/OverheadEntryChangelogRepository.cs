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
    using TransformType = Func<IQueryable<OverheadEntryChangelogEntry>, IQueryable<OverheadEntryChangelogEntry>>;
    public interface IOverheadEntryChangelogRepository : IRepository<OverheadEntryChangelogEntry>
    {
        Task<List<OverheadEntryChangelogEntry>> GetByOverheadEntryAsync(OverheadEntry entry, params TransformType[] transforms);
    }

    public static class OverheadEntryChangelogIncludes
    {
        /// <summary>
        /// Transform for including the changelog entry creators
        /// </summary>
        public static readonly TransformType Creator = 
           query => query.Include(changelog => changelog.Creator); 
        
        /// <summary>
        /// Transform for including the changelog entry sessions old and new
        /// </summary>
        public static readonly TransformType Session = query => query
               .Include(changelog => (changelog as OverheadEntrySessionChangelogEntry).OldSession)
               .Include(changelog => (changelog as OverheadEntrySessionChangelogEntry).NewSession);

       /// <summary>
       /// All the includes required to display any overhead entry changelog entry
       /// </summary>
       public static readonly TransformType Display =
           new[] {Creator, Session }
               .Aggregate((a, b) => query => a(b(query)));
    }

    /// <summary>
    /// Repository for OverheadEntryChangelogEntry
    /// </summary>
    public class OverheadEntryChangelogRepository : Repository<OverheadEntryChangelogEntry>, IOverheadEntryChangelogRepository
    {
        public OverheadEntryChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<OverheadEntryChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }    

        /// <summary>
        /// Finds all the changelog entries for the given overhead entry ordered by when the occurred
        /// </summary>
        /// <param name="overheadEntry">Overhead entry to find changes for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given overhead entry</returns>
        public async Task<List<OverheadEntryChangelogEntry>> GetByOverheadEntryAsync(OverheadEntry overheadEntry, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.OverheadEntryChangedId == overheadEntry.Id)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();      
        }
    }
}