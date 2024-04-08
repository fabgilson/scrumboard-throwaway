using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>>;
    public interface IUserStoryTaskRepository : IRepository<UserStoryTask>
    {
        Task<List<UserStoryTask>> GetByStoryGroup(StoryGroup group, params TransformType[] transforms);
        Task<List<UserStoryTask>> GetByProject(Project project, params TransformType[] transforms);
        Task<List<UserStoryTask>> GetByStory(UserStory story, params TransformType[] transforms);
        Task<UserStoryTask> GetByIdAsync(long id, params TransformType[] transforms);
        Task UpdateTaskAssociationsAndTags(UserStoryTask task, List<UserTaskAssociation> associations, List<UserStoryTaskTag> tags);
        Task UpdateAssociations(UserStoryTask task);
        Task<List<UserTaskAssociation>> GetAssigneesAndReviewers(UserStoryTask task);
        Task<int> GetCountByProject(Project project, params TransformType[] transforms);
        Task<int> GetCountByStoryGroup(StoryGroup storyGroup, params TransformType[] transforms);
        Task<TimeSpan> GetEstimateByStoryGroup(StoryGroup group, params TransformType[] transforms);
        Task<ICollection<UserStoryTask>> GetTransformedAsync(params TransformType[] transforms);
    }

    public static class UserStoryTaskIncludes
    {
        /// <summary>
        /// Includes all user associations for UserStoryTask
        /// </summary>
        public static readonly TransformType Users = query => query.Include(task => task.UserAssociations).ThenInclude(association => association.User);
        /// <summary>
        /// Includes UserStoryTask.Story
        /// </summary>
        public static readonly TransformType Story = query => query.Include(task => task.UserStory);
        /// <summary>
        /// Includes UserStoryTask.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(task => task.Creator);
        /// <summary>
        /// Includes UserStoryTask.StoryGroup
        /// </summary>
        public static readonly TransformType StoryGroup = query => query.Include(task => task.UserStory).ThenInclude(story => story.StoryGroup);

        public static readonly TransformType Worklogs = query => query.Include(task => task.Worklog);
    }
    
    public static class UserStoryTaskFilters
    {
        /// <summary>
        /// Where the task is not marked as done or deferred
        /// </summary>
        public static readonly TransformType NotDoneOrDeferred = query 
            => query.Where(x => x.Stage != Stage.Done && x.Stage != Stage.Deferred);
        
        /// <summary>
        /// Where the task is not marked as done or deferred
        /// </summary>
        public static readonly TransformType DoneOrDeferred = query 
            => query.Where(x => x.Stage == Stage.Done || x.Stage == Stage.Deferred);
        
        /// <summary>
        /// Ignores any tasks whose ID exists in some collection of IDs
        /// </summary>
        /// <param name="ids">IDs of tasks to ignore</param>
        public static TransformType IdIsNotIn(IEnumerable<long> ids) => query
            => query.Where(x => !ids.Contains(x.Id));
        
        /// <summary>
        /// Where the task is not marked as done or deferred
        /// </summary>
        public static TransformType WithAssignedUser(User user) => query 
            => query.Where(x => x.UserAssociations.Any(a => a.UserId == user.Id && a.Role == TaskRole.Assigned));
        
        /// <summary>
        /// Where the task is not marked as done or deferred
        /// </summary>
        public static TransformType InBacklogOfSprint(Sprint sprint) => query 
            => query.Where(x => x.UserStory.StoryGroupId == sprint.Id);

        /// <summary>
        /// Where the task has been worked on by some user since some time, does not include work logged solely as review
        /// </summary>
        /// <param name="startDate">Date after which work must have been logged</param>
        /// <param name="user">User by whom the work must have been performed</param>
        public static TransformType WasWorkedOnSinceTimeByUser(DateTime startDate, User user) => query
            => query.Where(x => x.Worklog.Any(w => 
                w.Occurred >= startDate 
                && w.UserId == user.Id
                && (!w.GetWorkedTags().Any() || w.GetWorkedTags().Any(t => t.Name != "Review"))
            ));
    }

    /// <summary>
    /// Repository for UserStoryTask
    /// </summary>
    public class UserStoryTaskRepository : Repository<UserStoryTask>, IUserStoryTaskRepository
    {
        public UserStoryTaskRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<UserStoryTaskRepository> logger) : base(dbContextFactory, logger)
        {
        }

        /// <summary>
        /// Gets a user story task by its Id, returns null if no user story task with the id exists
        /// </summary>
        /// <param name="id">User story task key to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>User story task with the given Id if it exists, otherwise null</returns>
        public async Task<UserStoryTask> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets all the user story tasks contained within the project
        /// </summary>
        /// <param name="project">Project to find user story tasks for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of user story tasks within the project</returns>
        public async Task<List<UserStoryTask>> GetByProject(Project project, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.UserStory.ProjectId.Equals(project.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Finds all the user story tasks for a given user story
        /// </summary>
        /// <param name="story">User story to find tasks for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>All user story tasks within the story</returns>
        public async Task<List<UserStoryTask>> GetByStory(UserStory story, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.UserStoryId == story.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Finds all the user story tasks contained within a story group
        /// </summary>
        /// <param name="group">Story group to find tasks for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>all user story tasks contained within the story group</returns>
        public async Task<List<UserStoryTask>> GetByStoryGroup(StoryGroup group, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.UserStory.StoryGroup == group)
                .ToListAsync();
        }

        protected override async Task AddAsync(DatabaseContext context, UserStoryTask task)
        {
            // Ensure that the task tags do not try to add themselves
            foreach (var tag in task.Tags ?? new List<UserStoryTaskTag>()) context.Entry(tag).State = EntityState.Unchanged;
            await base.AddAsync(context, task);
        }

        /// <summary>
        /// Updates a user story task, its user associations and tags
        /// </summary>
        /// <param name="task">User story task to update</param>
        /// <param name="associations">Updated list of user associations</param>
        /// <param name="tags">Updated list of tags</param>
        /// <exception cref="InvalidOperationException">If task.UserAssociations or task.Tags is not null</exception>
        public async Task UpdateTaskAssociationsAndTags(UserStoryTask task, List<UserTaskAssociation> associations, List<UserStoryTaskTag> tags)
        {
            if (task.UserAssociations != null) throw new InvalidOperationException("task.UserAssociations should be null");
            if (task.Tags != null) throw new InvalidOperationException("task.Tags should be null");

            await using var context = await _dbContextFactory.CreateDbContextAsync();  
            await context.Database.BeginTransactionAsync();
            
            context.Update(task);
            await UpdateTags(context, task, tags);
            await UpdateAssociations(context, task, associations);
            await context.SaveChangesAsync();
            await UpdateRowVersion(context, task.Id);
            context.Database.CommitTransaction();           
            
            var updatedTask = await GetByIdAsync(task.Id);
            task.RowVersion = updatedTask.RowVersion;
        }

        /// <summary>
        /// Updates just the user associations for this user story task
        /// </summary>
        /// <param name="task">Task to update user associations for</param>
        public async Task UpdateAssociations(UserStoryTask task)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();  
            await UpdateAssociations(context, task, task.UserAssociations.ToList());
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates just the tags of a user story task using the provided database context 
        /// </summary>
        /// <param name="context">Database context to use</param>
        /// <param name="task">Task to update tags for</param>
        /// <param name="tags">Updated tag list</param>
        private async Task UpdateTags(DatabaseContext context, UserStoryTask task, List<UserStoryTaskTag> tags)
        {
            var databaseTagJoins = await context.UserStoryTaskTagJoins
                .Where(x => x.TaskId == task.Id)
                .ToListAsync();
            foreach(var tagJoin in databaseTagJoins)
            {
                if (!tags.Any(t => t.Id == tagJoin.TagId)) {
                    context.Remove(tagJoin);
                }
            }
            foreach (var tag in tags)
            {
                var tagJoin = databaseTagJoins.Where(join => join.TagId == tag.Id).FirstOrDefault();
                if (tagJoin == null) {
                    context.Add(new UserStoryTaskTagJoin(task, tag));
                }
            } 
        }

        /// <summary>
        /// Updates just the user associations for a user story task using the provided database context
        /// </summary>
        /// <param name="context">Database context to use</param>
        /// <param name="task">User story task to update</param>
        /// <param name="associations">Updated user associations</param>
        private async Task UpdateAssociations(DatabaseContext context, UserStoryTask task, List<UserTaskAssociation> associations)
        {
            associations = associations
                .Select(association => new UserTaskAssociation() { 
                    UserId = association.UserId, 
                    TaskId = association.TaskId,
                    Role = association.Role,
                })
                .ToList();

            var databaseAssociations = await context.UserTaskAssociations.Where(x => x.TaskId == task.Id).ToListAsync();       
            foreach(var association in databaseAssociations)
            {
                if (!associations.Any(a => a.UserId == association.UserId)) {
                    context.Remove(association);
                }
            }
            foreach (var association in associations)
            {
                var databaseAssociation = databaseAssociations.Where(a => a.UserId == association.UserId).FirstOrDefault();
                if (databaseAssociation == null) {
                    context.Add(association);
                } else {
                    context.Entry(databaseAssociation).CurrentValues.SetValues(association);
                    context.Update(databaseAssociation);
                }
            }  
        }

        /// <summary>
        /// Gets all the assignees and reviewers for the given user story task
        /// </summary>
        /// <param name="task">User story task to get assignees and reviewers for</param>
        /// <returns>List of assignees and reviewers for the given user story task</returns>
        public async Task<List<UserTaskAssociation>> GetAssigneesAndReviewers(UserStoryTask task)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.UserTaskAssociations
                                .Include(assoc => assoc.User)
                                .Where(assoc => assoc.TaskId == task.Id)                               
                                .ToListAsync();
        }

        /// <summary>
        /// Gets the number of user story tasks contained within the given project
        /// </summary>
        /// <param name="project">Project to count tasks within</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Number of tasks contained within the project</returns>
        public async Task<int> GetCountByProject(Project project, params TransformType[] transforms)
        {
            var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.UserStory.ProjectId == project.Id)
                .CountAsync();
        }

        /// <summary>
        /// Gets the number of user story tasks contained within the given story group
        /// </summary>
        /// <param name="storyGroup">Story group to count tasks within</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Number of tasks contained within the story group</returns>
        public async Task<int> GetCountByStoryGroup(StoryGroup storyGroup, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(task => task.UserStory.StoryGroup == storyGroup)
                .CountAsync();
        }

        /// <summary>
        /// Gets the sum of all the task estimates within the given story group
        /// </summary>
        /// <param name="group">Story group to request total task estimate for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. filters</param>
        /// <returns>Total task estimate within this story group</returns>
        public async Task<TimeSpan> GetEstimateByStoryGroup(StoryGroup group, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return TimeSpan.FromTicks(await GetBaseQuery(context, transforms)
                .Where(task => task.UserStory.StoryGroupId == group.Id)
                .SumAsync(task => task.EstimateTicks));
        }

        /// <summary>
        /// General method to return a collection of tasks according to some provided filters and transforms
        /// </summary>
        /// <param name="transforms">Filters, sorts, and other transforms</param>
        /// <returns>Tasks remaining after all transforms applied</returns>
        public async Task<ICollection<UserStoryTask>> GetTransformedAsync(params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms).ToListAsync();
        }
    }
}