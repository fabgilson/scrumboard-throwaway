using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Marking;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<OverheadEntry>, IQueryable<OverheadEntry>>;
    public interface IOverheadEntryRepository : IRepository<OverheadEntry>
    {
        Task<OverheadEntry> GetByIdAsync(long id, params TransformType[] transforms);
        Task<TimeSpan> GetTotalTimeLogged(params TransformType[] transforms);
    }

    public static class OverheadEntryIncludes
    {
        public static readonly TransformType Session = query => query.Include(entry => entry.Session);
        public static readonly TransformType User = query => query.Include(entry => entry.User);
    }

    public static class OverheadEntryTransforms
    {
        public static TransformType FilterByProject(Project project)
            => query => query.Where(entry => entry.Sprint.SprintProjectId == project.Id);
        
        public static TransformType FilterBySprint(Sprint sprint)
            => query => query.Where(entry => entry.SprintId == sprint.Id);

        public static TransformType FilterByUser(User user)
            => query => query.Where(entry => entry.UserId == user.Id);
    }

    /// <summary>
    /// Repository for OverheadEntry
    /// </summary>
    public class OverheadEntryRepository : Repository<OverheadEntry>, IOverheadEntryRepository
    {
        public OverheadEntryRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<OverheadEntryRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a overhead entry by its Id, returns null if no overhead entry with the id exists
        /// </summary>
        /// <param name="id">Overhead entry key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Overhead entry with the given Id if it exists, otherwise null</returns>
        public async Task<OverheadEntry> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets the total time logged on overhead
        /// </summary>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Duration spent on overhead</returns>
        public async Task<TimeSpan> GetTotalTimeLogged(params TransformType[] transforms) {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var totalTicks = await GetBaseQuery(context, transforms)
                .Select(entry => entry.DurationTicks)
                .SumAsync();
            return TimeSpan.FromTicks(totalTicks);
        }
    }
}