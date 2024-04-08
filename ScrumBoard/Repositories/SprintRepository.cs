using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<Sprint>, IQueryable<Sprint>>;
    public interface ISprintRepository : IRepository<Sprint>
    {
        Task<Sprint> GetByIdAsync(long id, params TransformType[] transforms);
        Task<List<Sprint>> GetByProjectAsync(Project project, params TransformType[] transforms);
        Task<List<Sprint>> GetByProjectIdAsync(long projectId, params TransformType[] transforms);
        Task<Sprint> GetByWorklogEntry(WorklogEntry entry, params TransformType[] transforms);
        Task<bool> ProjectHasCurrentSprintAsync(long projectId);
        new Task UpdateAsync(Sprint sprint);
    }

    public static class SprintIncludes
    {
        /// <summary>
        /// Includes the stories for sprints in order
        /// </summary>
        public static readonly TransformType Story = query => query
            .Include(sprint => sprint.Stories.OrderBy(story => story.Order));

        /// <summary>
        /// Includes stories in order for sprints and then the tasks for those stories 
        /// </summary>
        public static readonly TransformType Tasks = query => query
            .Include(sprint => sprint.Stories.OrderBy(story => story.Order))
            .ThenInclude(story => story.Tasks);
        
        /// <summary>
        /// Includes Sprint.Worklog
        /// </summary>
        public static readonly TransformType Worklog = query => query
            .Include(sprint => sprint.Stories.OrderBy(story => story.Order))
            .ThenInclude(story => story.Tasks)
            .ThenInclude(task => task.Worklog);
        
        /// <summary>
        /// Includes Sprint.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(sprint => sprint.Creator);
    }

    /// <summary>
    /// Repository for Sprint
    /// </summary>
    public class SprintRepository : Repository<Sprint>, ISprintRepository
    {
        public SprintRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<Repository<Sprint>> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a sprint by its Id, returns null if no sprint with the id exists
        /// </summary>
        /// <param name="id">Sprint key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Sprint with the given Id if it exists, otherwise null</returns>
        public async Task<Sprint> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(sprint => sprint.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Finds all the sprints for the given project
        /// </summary>
        /// <param name="project">Project to get sprints for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of sprints for the given project</returns>
        public async Task<List<Sprint>> GetByProjectAsync(Project project, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(sprint => sprint.Project == project)
                .ToListAsync();
        }

        /// <summary>
        /// Finds all the sprints for the project with the provided Id
        /// </summary>
        /// <param name="projectId">Id of project to find sprints for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of sprints for the project</returns>
        public async Task<List<Sprint>> GetByProjectIdAsync(long projectId, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(sprint => sprint.SprintProjectId == projectId)
                .ToListAsync();
        }

        /// <summary>
        /// Finds the sprint for the given WorklogEntry
        /// </summary>
        /// <param name="entry">Worklog entry to find sprint for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Sprint for the worklog entry, or null if no sprint found</returns>
        public async Task<Sprint> GetByWorklogEntry(WorklogEntry entry, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(sprint => sprint.Stories.SelectMany(story => story.Tasks).SelectMany(task => task.Worklog).Contains(entry))
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Determines whether the project with the provided Id has a project that can be considered "current" (either
        /// in SprintStage.Created or SprintStage.Started)
        /// </summary>
        /// <param name="projectId">Project id to check for</param>
        /// <returns>True if project id has current sprint, otherwise false</returns>
        public async Task<bool> ProjectHasCurrentSprintAsync(long projectId)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Sprints
                .Where(sprint => sprint.SprintProjectId == projectId 
                        && 
                        (sprint.Stage == SprintStage.Created || sprint.Stage == SprintStage.Started))
                .AnyAsync();
        }

        public override async Task UpdateAsync(Sprint sprint) 
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.BeginTransactionAsync();
            context.Update(sprint);
            await context.SaveChangesAsync();
            await UpdateRowVersion(context, sprint.Id);
            context.Database.CommitTransaction();         
            
            var updatedSprint = await GetByIdAsync(sprint.Id);
            sprint.RowVersion = updatedSprint.RowVersion;
        }
    }
}