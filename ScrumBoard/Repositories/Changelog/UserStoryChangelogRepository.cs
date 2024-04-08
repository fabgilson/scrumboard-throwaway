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
    using TransformType = Func<IQueryable<UserStoryChangelogEntry>, IQueryable<UserStoryChangelogEntry>>;
    public interface IUserStoryChangelogRepository : IRepository<UserStoryChangelogEntry>
    {
        Task<List<UserStoryChangelogEntry>> GetByUserStoryAsync(UserStory userStory, params TransformType[] transforms);
    }

    public static class UserStoryChangelogIncludes
    {
        /// <summary>
        /// Includes UserStoryChangelogEntry.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(changelog => changelog.Creator);
        
        /// <summary>
        /// Includes AcceptanceCriteriaChangelogEntry.AcceptanceCriteriaChanged
        /// </summary>
        public static readonly TransformType AcceptanceCriteria = query =>
           query.Include(changelog => (changelog as AcceptanceCriteriaChangelogEntry).AcceptanceCriteriaChanged);
       
       /// <summary>
       /// All the includes required to display any user story changelog entry
       /// </summary>
       public static readonly TransformType Display =
           new[] {Creator, AcceptanceCriteria }
               .Aggregate((a, b) => query => a(b(query)));
    }

    public class UserStoryChangelogRepository : Repository<UserStoryChangelogEntry>, IUserStoryChangelogRepository
    {
        /// <summary>
        /// Repository for UserStoryChangelogEntry
        /// </summary>
        public UserStoryChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<UserStoryChangelogRepository> logger) : base(dbContextFactory, logger)
        {
        }    

        /// <summary>
        /// Finds all the changelog entries for the given user story ordered by when the occurred
        /// </summary>
        /// <param name="userStory">User story to find changes for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>List of changes for the given user story</returns>
        public async Task<List<UserStoryChangelogEntry>> GetByUserStoryAsync(UserStory userStory, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(entry => entry.UserStoryChangedId == userStory.Id)
                .OrderByDescending(entry => entry.Created)
                .ToListAsync();      
        }
        
    }
}