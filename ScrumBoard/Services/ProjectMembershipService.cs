using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Services;

public interface IProjectMembershipService
{
    /// <summary>
    /// Adds all developers of some other project to this project with the `Reviewer` role
    /// </summary>
    /// <param name="actingUser">The user performing the action</param>
    /// <param name="reviewerProject">The project from which developers will be added as reviewers</param>
    /// <param name="revieweeProject">The project to add reviewers to</param>
    Task AddMembersOfProjectAsReviewers(User actingUser, Project reviewerProject, Project revieweeProject);

    /// <summary>
    /// Removes all members of the given project that have the 'Reviewer' role
    /// </summary>
    /// <param name="actingUser">The user performing the action</param>
    /// <param name="project">The project to remove reviewers from</param>
    Task RemoveAllReviewersFromProject(User actingUser, Project project);
}

public class ProjectMembershipService(IProjectRepository projectRepository,
    IProjectChangelogRepository projectChangelogRepository) : IProjectMembershipService
{
    /// <inheritdoc/>
    public async Task AddMembersOfProjectAsReviewers(User actingUser, Project reviewerProject, Project revieweeProject)
    {
        if (actingUser == null || reviewerProject == null || revieweeProject == null) return;

        var newMemberships = revieweeProject.MemberAssociations.Concat(reviewerProject.MemberAssociations
                .Where(membership => membership.Role == ProjectRole.Developer) // Only add developers
                .Select(association => new ProjectUserMembership()
                {
                    Project = revieweeProject,
                    ProjectId = association.ProjectId,
                    User = association.User,
                    UserId = association.UserId,
                    Role = ProjectRole.Reviewer,
                }))
            .DistinctBy(membership =>
                membership.UserId) // Remove any duplicate memberships e.g. User is already reviewing project
            .ToList();

        await UpdateProjectMembers(actingUser, revieweeProject, newMemberships);
    }

    /// <inheritdoc/>
    public async Task RemoveAllReviewersFromProject(User actingUser, Project project)
    {
        if (actingUser == null || project == null) return;

        var newMemberships = project.MemberAssociations
            .Where(membership => membership.Role != ProjectRole.Reviewer) // Don't select any Reviewers
            .ToList();
        await UpdateProjectMembers(actingUser, project, newMemberships);
    }

    /// <summary>
    /// Updates the memberships of the given project with the given list of new memberships
    /// </summary>
    /// <param name="actingUser">The user performing the action</param>
    /// <param name="project">The project to change the memberships of</param>
    /// <param name="newMemberships">A list of ProjectUserMemberships</param>
    private async Task UpdateProjectMembers(User actingUser, Project project,
        List<ProjectUserMembership> newMemberships)
    {
        var projectForm = new ProjectEditForm(project, false) { MemberAssociations = newMemberships };
        var memberChanges = projectForm.ApplyMemberChanges(actingUser, project);
        try
        {
            await projectRepository.UpdateMemberships(project);
        }
        catch (DbUpdateConcurrencyException)
        {
            return;
        }
        await projectChangelogRepository.AddAllAsync(memberChanges);
    }
}