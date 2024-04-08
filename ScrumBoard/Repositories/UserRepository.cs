using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<User>, IQueryable<User>>;
    public interface IUserRepository : IRepository<User>
    {
        Task<List<User>> GetUsersByKeyword(string keyword, params TransformType[] transforms);

        Task<User> GetByIdAsync(long id, params TransformType[] transforms);

        Task<User> GetByUsername(string username, params TransformType[] transforms);

        Task<User> GetByEmail(string email, params TransformType[] transforms);
    }

    public static class UserIncludes
    {
        /// <summary>
        /// Includes all the projects for a user
        /// </summary>
        public static readonly TransformType Project = query => query
                .Include(user => user.ProjectAssociations)
                .ThenInclude(association => association.Project);
    }

    /// <summary>
    /// Repository for User
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        
        public UserRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<Repository<User>> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a user by its Id, returns null if no user with the id exists
        /// </summary>
        /// <param name="id">User key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User with the given Id if it exists, otherwise null</returns>
        public async Task<User> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(user => user.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Finds all the users that match the provided keywords
        /// i.e. the keyword is contained within the user's firstname joined with lastname
        /// </summary>
        /// <param name="keyword">Keyword to search for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, additional filters, sorts</param>
        /// <returns>List of users matching the keyword</returns>
        public async Task<List<User>> GetUsersByKeyword(string keyword, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var result = await GetBaseQuery(context, transforms).Where
            (
                p => (p.FirstName.ToLower() + " " + p.LastName.ToLower()).Contains(keyword.ToLower())
            ).ToListAsync();
            _logger.LogDebug($"Found results from search {keyword}: {result}");
            return result;
        }

        /// <summary>
        /// Finds a user with the given uc username, returns null if no user matches
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User that has the given username, null if no user found</returns>
        public async Task<User> GetByUsername(string username, params TransformType[] transforms) {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(user => user.LDAPUsername == username)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Finds a user with the given email, returns null if no user matches
        /// </summary>
        /// <param name="email">Email to search for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User that has the given email, null if no user found</returns>
        public async Task<User> GetByEmail(string email, params TransformType[] transforms) {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(user => user.Email == email)
                .SingleOrDefaultAsync();
        }
    }
}