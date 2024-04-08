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
    using TransformType = Func<IQueryable<WorklogEntryChangelogEntry>, IQueryable<WorklogEntryChangelogEntry>>;
    public interface IWorklogEntryChangelogRepository : IRepository<WorklogEntryChangelogEntry>
    {
        Task<List<WorklogEntryChangelogEntry>> GetByWorklogEntryAsync(WorklogEntry worklogEntry, params TransformType[] transforms);
    }

    public static class WorklogEntryChangelogIncludes
    {
        /// <summary>
        /// Includes WorklogEntryChangelogEntry.Creator
        /// </summary>
        public static readonly TransformType Creator = 
           query => query.Include(changelog => changelog.Creator);
        
        /// <summary>
        /// Includes WorklogEntryUserAssociationChangelogEntry.PairUserChanged
        /// </summary>
        public static readonly TransformType Partner = query =>
           query.Include(changelog => (changelog as WorklogEntryUserAssociationChangelogEntry).PairUserChanged);
        
        /// <summary>
        /// Includes WorklogEntryTagChangelogEntry.TagChanged
        /// </summary>
        public static readonly TransformType Tag = query =>
           query.Include(changelog => (changelog as TaggedWorkInstanceChangelogEntry).WorklogTag);

        /// <summary>
        /// Includes WorklogEntryCommitChangelogEntry.CommitChanged
        /// </summary>
        public static readonly TransformType Commit = query => 
            query.Include(changelog => (changelog as WorklogEntryCommitChangelogEntry).CommitChanged);

       /// <summary>
       /// All the includes required to display any worklog changelog entry
       /// </summary>
       public static readonly TransformType Display =
           new[] {Creator, Partner, Tag, Commit}
               .Aggregate((a, b) => query => a(b(query)));
    }

    /// <summary>
    /// Repository for WorklogEntryChangelogEntry
    /// </summary>
    public class WorklogEntryChangelogRepository : Repository<WorklogEntryChangelogEntry>, IWorklogEntryChangelogRepository
    {
        public WorklogEntryChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<WorklogEntryChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }    

        /// <summary>
        /// Finds all the changelog entries for the given worklog entry ordered by when the occurred
        /// </summary>
        /// <param name="worklogEntry">Worklog entry to find changes for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given worklog entry</returns>
        public async Task<List<WorklogEntryChangelogEntry>> GetByWorklogEntryAsync(WorklogEntry worklogEntry, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.WorklogEntryChangedId == worklogEntry.Id)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();      
        }
    }
}