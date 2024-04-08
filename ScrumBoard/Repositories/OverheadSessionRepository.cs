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
    using TransformType = Func<IQueryable<OverheadSession>, IQueryable<OverheadSession>>;
    public interface IOverheadSessionRepository : IRepository<OverheadSession>
    {
        Task<OverheadSession> GetByIdAsync(long id, params TransformType[] transforms);
    }

    /// <summary>
    /// Repository for OverheadSession
    /// </summary>
    public class OverheadSessionRepository : Repository<OverheadSession>, IOverheadSessionRepository
    {
        public OverheadSessionRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<OverheadSessionRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a overhead session by its Id, returns null if no overhead session with the id exists
        /// </summary>
        /// <param name="id">Overhead session key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Overhead session with the given Id if it exists, otherwise null</returns>
        public async Task<OverheadSession> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.Id == id)
                .SingleOrDefaultAsync();
        }
    }
}