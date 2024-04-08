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
    using TransformType = Func<IQueryable<ProjectChangelogEntry>, IQueryable<ProjectChangelogEntry>>;
    public interface IProjectChangelogRepository : IRepository<ProjectChangelogEntry>
    {
        Task<IEnumerable<ProjectChangelogEntry>> GetOrderedProjectChangelogsByProjectAsync(Project project, params TransformType[] transforms);
    }

    public static class ProjectChangelogIncludes
    {
        /// <summary>
        /// Includes ProjectChangelogEntry.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(change => change.Creator);
        
        /// <summary>
        /// Includes ProjectUserMembershipChangelogEntry.RelatedUser
        /// </summary>
        public static readonly TransformType RelatedUser = query => query.Include(c => (c as ProjectUserMembershipChangelogEntry).RelatedUser);
    }

    /// <summary>
    /// Repository for ProjectChangelogEntry
    /// </summary>
    public class ProjectChangelogRepository : Repository<ProjectChangelogEntry>, IProjectChangelogRepository
    {
        public ProjectChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<ProjectChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Get all project changelog entry by given project ordered by when they occurred. The project must exist.
        /// </summary>
        /// <param name="project">Project for which to get corresponding entries.</param>
        /// <returns>A list of found project changelog entries</returns>
        public async Task<IEnumerable<ProjectChangelogEntry>> GetOrderedProjectChangelogsByProjectAsync(Project project, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(x => x.ProjectChanged == project)
                .OrderByDescending(c => c.Created)
                .ToListAsync();
        }
        
    }
}