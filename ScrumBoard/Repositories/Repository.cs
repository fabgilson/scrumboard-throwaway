using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync(params Func<IQueryable<T>, IQueryable<T>>[] transforms);
        Task<PaginatedList<T>> GetAllPaginatedAsync(int pageIndex, int pageSize, params Func<IQueryable<T>, IQueryable<T>>[] transforms);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task AddAllAsync(IEnumerable<T> entities);
        Task UpdateAllAsync(IEnumerable<T> entities);        
    }

    /// <summary>
    /// Base class for all repositories.
    /// Used for creating/updating/getting/deleting entities
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected IDbContextFactory<DatabaseContext> _dbContextFactory;

        protected readonly ILogger<Repository<T>> _logger;

        public Repository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<Repository<T>> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Gets the queryable for this entity type and applies all the transformations provided
        /// </summary>
        /// <param name="context">Database context to get queryable from</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>Queryable for the entity</returns>
        protected IQueryable<T> GetBaseQuery(DatabaseContext context, IEnumerable<Func<IQueryable<T>, IQueryable<T>>> transforms)
        {
            IQueryable<T> query = context.Set<T>();
            return transforms.Aggregate(query, (current, transform) => transform(current));
        }

        /// <summary>
        /// Gets all the entities after applying the queryable transformations
        /// </summary>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of all entities</returns>
        public virtual async Task<List<T>> GetAllAsync(params Func<IQueryable<T>, IQueryable<T>>[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms).ToListAsync();
        }

        /// <summary>
        /// Gets a single page of entities after applying the queryable transformations
        /// </summary>
        /// <param name="pageSize">Maximum number of results per page</param>
        /// <param name="pageIndex">Index of page to fetch (1 indexed)</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>Page of entities</returns>
        public async Task<PaginatedList<T>> GetAllPaginatedAsync(int pageIndex, int pageSize, params Func<IQueryable<T>, IQueryable<T>>[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await PaginatedList<T>.CreateAsync(GetBaseQuery(context, transforms), pageIndex, pageSize);
        }

        /// <summary> 
        /// Variant of Add async where the context is owned by the caller.
        /// May be modified by subclasses to modify the entity before saving.
        /// </summary>
        /// <param name="context">Database context to add entity to</param>
        /// <param name="entity">Entity to add</param>
        protected virtual async Task AddAsync(DatabaseContext context, T entity)
        {
            await context.AddAsync(entity);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds an new entity asynchronously, persisting it to the database
        /// </summary>
        /// <param name="entity">Entity to add</param>
        public async Task AddAsync(T entity)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await AddAsync(context, entity);
        }

        /// <summary>
        /// Adds a collection of new entities asynchronously, persisting them to the database
        /// </summary>
        /// <param name="entities">Entities to add</param>
        public async Task AddAllAsync(IEnumerable<T> entities)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an entity asynchronously, persisting it to the database
        /// </summary>
        /// <param name="entity">Entity to update</param>
        public virtual async Task UpdateAsync(T entity) 
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            context.Update(entity);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates a collection of entities asynchronously, persisting them to the database
        /// </summary>
        /// <param name="entities">Entities to update</param>
        public async Task UpdateAllAsync(IEnumerable<T> entities) 
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            context.UpdateRange(entities);
            await context.SaveChangesAsync();
        }

        /// <summary> 
        /// Updates the row version of the entity (if the database provider is relational)
        /// </summary>
        /// <param name="context">The current database context</param>
        /// <param name="entityId">The Id of the entity to update the rowversion of</param>
        /// <returns>A Task</returns>
        protected async Task UpdateRowVersion(DbContext context, long entityId) {
            // Raw SQL cannot be executed when using the InMemoryDB
            if (context.Database.IsRelational() && !context.Database.IsSqlite()) {
                var tableName = context.Model.FindEntityType(typeof(T))!.GetTableName();
                // Manually update the RowVersion so only changing related (many-to-many) entity changes will trigger a concurrency exception
                await context.Database.ExecuteSqlRawAsync($"UPDATE {tableName} SET RowVersion = CURRENT_TIMESTAMP(6) WHERE Id = {entityId};");
            }
        }
        
        /// <summary>
        /// Delete all children of a parent that are not in the list of remaining children
        /// </summary>
        /// <param name="context">Database context to execute transaction in</param>
        /// <param name="parent">Parent entity to remove children of</param>
        /// <param name="remainingChildren">Children of the parent that should be kept</param>
        /// <param name="parentSelector">Expression for navigating from child to parent</param>
        /// <typeparam name="TChild">Entity type on the many side of the relationship</typeparam>
        /// <typeparam name="TParent">Entity type on the one side of the relationship</typeparam>
        protected void DeleteLeftoverChildren<TChild, TParent>(DatabaseContext context, TParent parent, IEnumerable<TChild> remainingChildren, Expression<Func<TChild, TParent>> parentSelector) where TChild : class, IId
        {
            var allowedIds = remainingChildren.Select(child => child.Id).ToList();
            var dbSet = context.Set<TChild>();
            dbSet.RemoveRange(dbSet.Where(parentSelector.JoinEquals(_ => parent).AndAlso(child => !allowedIds.Contains(child.Id))));
        }
    }
}