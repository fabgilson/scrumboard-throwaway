using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.Modals;

public partial class ManageReviewersModal : BaseProjectScopedComponent
{
    [Inject] private IProjectMembershipService ProjectMembershipService { get; set; }

    private ResultModal<bool> _resultModal;
    private List<Project> _allProjects = [];

    private IEnumerable<Project> FilteredProjects => _allProjects.Where(x => x.Name.Contains(SearchQuery.Trim(), StringComparison.OrdinalIgnoreCase));

    private Project _selectedProject, _revieweeProject;

    private string _searchQuery = "";

    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            _resultModal.Refresh();
        }
    }

    /// <summary>
    /// Given some current project, prompts the user to select a different project that will act 
    /// as sprint reviewers for the current project. 
    /// </summary>
    /// <param name="revieweeProject">The project under review, for which we are selecting reviewers</param>
    /// <returns>The project chosen to review the current project, null if no choice made</returns>
    public virtual async Task<bool> Show(Project revieweeProject)
    {
        _selectedProject = null;
        _revieweeProject = revieweeProject;

        await UpdateReviewModalProjects();

        return await _resultModal.Show();
    }

    /// <summary>
    /// Updates the projects available to add reviewers from in the review modal.
    /// </summary>
    private async Task UpdateReviewModalProjects()
    {
        // All members of the current project that can perform a review (Reviewer, Developer, or Leader)
        var revieweeProjectMemberIds = _revieweeProject.MemberAssociations
            .Where(membership => membership.Role != ProjectRole.Guest)
            .Select(membership => membership.UserId)
            .ToList();

        // Fetch all projects except this one
        var projects = await ProjectRepository.GetAllAsync(
            query => query.Where(project => project.Id != _revieweeProject.Id), ProjectIncludes.Member);

        // Projects that have at least one developer that is not currently able to review the current project
        projects = projects.Where(project =>
                project.MemberAssociations.Where(membership => membership.Role == ProjectRole.Developer)
                    .Any(membership => !revieweeProjectMemberIds.Contains(membership.UserId)))
            .ToList();

        _allProjects.Clear();
        _allProjects.AddRange(projects);
    }

    /// <summary>
    /// If selected project is equal to the given project, set selectedProject to null. 
    /// Otherwise set to project. Refreshes the modal.
    /// </summary>
    /// <param name="project">Project to compare selectedProject to.</param>
    private void SelectProject(Project project)
    {
        _selectedProject = _selectedProject == project ? null : project;
        _resultModal.Refresh();
    }

    /// <summary>
    /// Adds the developers from the given project as reviewers to the current project then
    /// updates the modal.
    /// </summary>
    /// <param name="reviewerProject">The project to add reviewers from</param>
    private async Task AddMembersOfProjectAsReviewers(Project reviewerProject)
    {
        await ProjectMembershipService.AddMembersOfProjectAsReviewers(Self, reviewerProject, _revieweeProject);

        await UpdateReviewModalProjects();
        _resultModal.Refresh();
    }

    /// <summary>
    /// Removes all reviewers from the current project then updates the modal.
    /// </summary>
    private async Task RemoveAllReviewersFromProject()
    {
        await ProjectMembershipService.RemoveAllReviewersFromProject(Self, _revieweeProject);

        await UpdateReviewModalProjects();
        _resultModal.Refresh();
    }
}