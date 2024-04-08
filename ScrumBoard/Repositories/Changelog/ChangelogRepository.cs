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
    using TransformType = Func<IQueryable<ChangelogEntry>, IQueryable<ChangelogEntry>>;
    public interface IChangelogRepository : IRepository<ChangelogEntry>
    {
        
    }

    /// <summary>
    /// Transformations that can be applied to requests for ChangelogEntries that include an attribute(s) of ChangelogEntry
    /// </summary>
    public static class ChangelogIncludes
    {
        /// <summary>
        /// Transform for including creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(changelog => changelog.Creator);
    }
    
    /// <summary>
    /// Repository for ChangelogEntry
    /// </summary>
    public class ChangelogRepository : Repository<ChangelogEntry>, IChangelogRepository
    {
        public ChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<ChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }
        
    }
}