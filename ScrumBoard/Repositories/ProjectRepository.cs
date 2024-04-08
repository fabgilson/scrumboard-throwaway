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
    using TransformType = Func<IQueryable<Project>, IQueryable<Project>>;
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project> GetByIdAsync(long id, params TransformType[] transforms);
        Task<List<Project>> GetByUserAsync(User user);
        Task UpdateMemberships(Project project);
        Task UpdateProjectAndMemberships(Project project, List<ProjectUserMembership> memberships);
        Task<ProjectRole?> GetRole(long projectId, long userId);
        Task<bool> ProjectWithIdExistsAsync(long projectId);
        Task<Project> GetByStandUpMeetingAsync(StandUpMeeting standUpMeeting, params TransformType[] transforms);
    }

    public static class ProjectIncludes
    {
        /// <summary>
        /// Includes all the members of a project
        /// </summary>
        public static readonly TransformType Member = query => query.Include(project => project.MemberAssociations).ThenInclude(membership => membership.User);
        
        /// <summary>
        /// Includes Project.Creator
        /// </summary>
        public static readonly TransformType Creator = query => query.Include(project => project.Creator);
        
        /// <summary>
        /// Includes Project.Backlog
        /// </summary>
        public static readonly TransformType Backlog = query => query.Include(project => project.Backlog);
        
        /// <summary>
        /// Includes sprints within the project ordered by their start dates
        /// </summary>
        public static readonly TransformType Sprints = query => query
            .Include(project => project.Sprints.OrderByDescending(sprint => sprint.StartDate));
    }

    /// <summary>
    /// Repository for Project
    /// </summary>
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<ProjectRepository> logger) : base(dbContextFactory, logger)
        {
        }

        public async Task<Project> GetByIdAsync(long id, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(project => project.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Finds all the projects where the given user is a member of
        /// </summary>
        /// <param name="user">User to find projects for</param>
        /// <returns>List of projects that the user is a part of</returns>
        public async Task<List<Project>> GetByUserAsync(User user)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.ProjectUserMemberships
                    .Include(association => association.Project)
                    .ThenInclude(project => project.MemberAssociations)
                    .ThenInclude(association => association.User)
                    .Where(association => association.UserId == user.Id)
                    .Select(association => association.Project)
                    .ToListAsync();
        }

        /// <summary>
        /// Updates a project and all the memberships associated with it and persists it to the database
        /// </summary>
        /// <param name="project">Project to update, project.MemberAssociations should be null</param>
        /// <param name="memberships">Updated list of memberships to apply</param>
        /// <exception cref="InvalidOperationException">If project.MemberAssociations is not null</exception>
        public async Task UpdateProjectAndMemberships(Project project, List<ProjectUserMembership> memberships)
        {
            if (project.MemberAssociations != null)
                throw new InvalidOperationException("project.MemberAssociations should be null");
            
            await using var context = await _dbContextFactory.CreateDbContextAsync();  
            await context.Database.BeginTransactionAsync();
            if (project.GitlabCredentials == null) {
                project.GitlabCredentials = new();
            }
            context.Update(project);
            await UpdateMemberships(context, project, memberships);                        
            await context.SaveChangesAsync();
            await UpdateRowVersion(context, project.Id);
            context.Database.CommitTransaction();
            
            var updatedProject = await GetByIdAsync(project.Id);
            project.RowVersion = updatedProject.RowVersion;
        }

        /// <summary>
        /// Updates the memberships of a project, not any of the other attributes
        /// </summary>
        /// <param name="project">Project to update memberships of</param>
        /// <exception cref="DbUpdateConcurrencyException">If this project has been updated while the memberships were modified</exception>
        public async Task UpdateMemberships(Project project)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            await using var transaction = await context.Database.BeginTransactionAsync();
            var beforeRowVersion = project.RowVersion;
            var afterRowVersion = (await GetByIdAsync(project.Id)).RowVersion;

            if (!beforeRowVersion.SequenceEqual(afterRowVersion))
            {
                throw new DbUpdateConcurrencyException("Project updated by another user");
            }
            await UpdateMemberships(context, project, project.MemberAssociations.ToList());
            await context.SaveChangesAsync();
            await UpdateRowVersion(context, project.Id);
            await transaction.CommitAsync();

            var newRowVersion = (await GetByIdAsync(project.Id)).RowVersion;
            project.RowVersion = newRowVersion;
        }

        /// <summary>
        /// Updates only memberships of a project using the provided database context
        /// </summary>
        /// <param name="context">Database context to make changes with</param>
        /// <param name="project">Project to update memberships of</param>
        /// <param name="memberships">Updated project memberships</param>
        private async Task UpdateMemberships(DatabaseContext context, Project project,
            List<ProjectUserMembership> memberships)
        {
            memberships = memberships.Select(membership => new ProjectUserMembership()
            {
                ProjectId = membership.Project?.Id ?? membership.ProjectId,
                UserId = membership.User?.Id ?? membership.UserId,
                Role = membership.Role,
            }).ToList();
            
            var databaseMemberships = await context.ProjectUserMemberships.Where(x => x.ProjectId == project.Id).ToListAsync();           
            foreach(var membership in databaseMemberships)
            {
                if (memberships.All(a => a.UserId != membership.UserId)) {
                    context.Remove(membership);
                }
            }
            foreach (var membership in memberships)
            {
                var databaseMembership = databaseMemberships.FirstOrDefault(m => m.UserId == membership.UserId);
                if (databaseMembership == null) {
                    context.Add(membership);
                } else {
                    context.Entry(databaseMembership).CurrentValues.SetValues(membership);
                    context.Update(databaseMembership);
                }
            }
        }

        /// <summary>
        /// Gets the role of a user within a project, or null if they're not a member
        /// </summary>
        /// <param name="projectId">Id of project to find user role</param>
        /// <param name="userId">Id of user to find out role of</param>
        /// <returns>ProjectRole of the user in the project, if they are not a member then null</returns>
        public async Task<ProjectRole?> GetRole(long projectId, long userId)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.ProjectUserMemberships
                .Where(membership => membership.UserId == userId && membership.ProjectId == projectId)
                .Select(membership => membership.Role as ProjectRole?)
                .SingleOrDefaultAsync();
        }
        
        /// <summary>
        /// Check whether a project exists with the given project Id
        /// </summary>
        /// <param name="projectId">ID of project for which to check existence</param>
        /// <returns>True if a project exists with the given ID, false otherwise</returns>
        public async Task<bool> ProjectWithIdExistsAsync(long projectId)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Projects.AnyAsync(x => x.Id == projectId);
        }

        /// <summary>
        /// Gets the project that is the owner of a given stand-up meeting.
        /// </summary>
        /// <param name="standUpMeeting">StandUpMeeting for which to get owning project</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>Project that owns given StandUpMeeting</returns>
        public async Task<Project> GetByStandUpMeetingAsync(StandUpMeeting standUpMeeting, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var id = await context.StandUpMeetings
                .Where(x => x.Id == standUpMeeting.Id)
                .Select(x => x.Sprint.Project.Id)
                .SingleOrDefaultAsync();
            return await GetByIdAsync(id, transforms);
        }
    }
}