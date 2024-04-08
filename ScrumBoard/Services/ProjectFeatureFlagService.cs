using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Repositories;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services;

public interface IProjectFeatureFlagService
{
    Task<ProjectFeatureFlag> AddProjectFeatureFlagAsync(Project project, User creator, FeatureFlagDefinition featureFlagDefinition);
    Task<bool> ProjectHasFeatureFlagAsync(Project project, FeatureFlagDefinition featureFlagDefinition);
    Task DeleteFeatureFlagFromProjectAsync(Project project, FeatureFlagDefinition featureFlagDefinition);
    Task<PaginatedList<Project>> GetPaginatedProjectsForFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, int pageNum, int pageSize);

    Task<VirtualizationResponse<Project>> GetVirtualizedProjectsWithoutFeatureFlagAsync(
        FeatureFlagDefinition featureFlagDefinition, VirtualizationRequest<Project> request);
}

public class ProjectFeatureFlagService : IProjectFeatureFlagService
{
    private readonly IProjectFeatureFlagRepository _projectFeatureFlagRepository;
    private readonly IProjectRepository _projectRepository;
    
    public ProjectFeatureFlagService(IProjectFeatureFlagRepository projectFeatureFlagRepository, IProjectRepository projectRepository)
    {
        _projectFeatureFlagRepository = projectFeatureFlagRepository;
        _projectRepository = projectRepository;
    }

    /// <summary>
    /// Adds a new feature flag for a project into the system.
    /// </summary>
    /// <param name="project">Project to add feature flag for</param>
    /// <param name="creator">The user creating the feature flag</param>
    /// <param name="featureFlagDefinition">Feature flag to add to project</param>
    /// <exception cref="KeyNotFoundException">Thrown if trying to add a feature flag for a non-existent project</exception>
    /// <exception cref="ArgumentException">Thrown if given feature flag already exists on project</exception>
    public async Task<ProjectFeatureFlag> AddProjectFeatureFlagAsync(Project project, User creator, FeatureFlagDefinition featureFlagDefinition)
    {
        if (!await _projectRepository.ProjectWithIdExistsAsync(project.Id))
        {
            throw new KeyNotFoundException("No such project with given ID exists");
        }

        if (await _projectFeatureFlagRepository.ProjectHasFeatureFlagAsync(project.Id, featureFlagDefinition))
        {
            throw new ArgumentException("Feature flag already exists on project");
        }

        var newProjectFeatureFlag = new ProjectFeatureFlag
        {
            ProjectId = project.Id,
            CreatorId = creator.Id,
            FeatureFlag = featureFlagDefinition,
            Created = DateTime.Now,
        };
        await _projectFeatureFlagRepository.AddAsync(newProjectFeatureFlag);
        return newProjectFeatureFlag;
    }

    /// <summary>
    /// Check whether a particular project has some feature flag.
    /// </summary>
    /// <param name="project">Project to check feature flag existence for</param>
    /// <param name="featureFlagDefinition">Feature flag to look for</param>
    /// <returns></returns>
    public async Task<bool> ProjectHasFeatureFlagAsync(Project project, FeatureFlagDefinition featureFlagDefinition)
    {
        return await _projectFeatureFlagRepository.ProjectHasFeatureFlagAsync(project.Id, featureFlagDefinition);
    }

    /// <summary>
    /// Deletes a given feature flag from a given project.
    /// </summary>
    /// <param name="project">Project to delete feature flag from.</param>
    /// <param name="featureFlagDefinition">Feature flag to be deleted.</param>
    public async Task DeleteFeatureFlagFromProjectAsync(Project project, FeatureFlagDefinition featureFlagDefinition)
    {
        await _projectFeatureFlagRepository.DeleteProjectFeatureFlag(project.Id, featureFlagDefinition);
    }

    /// <summary>
    /// Gets all projects that have a given feature flag as a paginated list.
    /// </summary>
    /// <param name="featureFlagDefinition">Feature flag for which to retrieve associated projects</param>
    /// <param name="pageNum">Page number of results, first page: pageNum=1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <returns>Paginated list of projects that have some given feature flag</returns>
    public async Task<PaginatedList<Project>> GetPaginatedProjectsForFeatureFlagAsync(
        FeatureFlagDefinition featureFlagDefinition, int pageNum, int pageSize)
    {
        return await _projectFeatureFlagRepository.GetPaginatedProjectsForFeatureFlagAsync(featureFlagDefinition, pageNum, pageSize);
    }
    
    
    /// <summary>
    /// Gets a window of the projects that do not have a feature flag set, also returns a count of total possible results.
    /// </summary>
    /// <param name="featureFlagDefinition">Feature flag to exclude projects by.</param>
    /// <param name="request">Request object containing parameters needed for virtualization</param>
    /// <returns>Virtualization ready projects without a given feature flag</returns>
    public async Task<VirtualizationResponse<Project>> GetVirtualizedProjectsWithoutFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, VirtualizationRequest<Project> request)
    {
        return await _projectFeatureFlagRepository.GetVirtualizedProjectsWithoutFeatureFlagAsync(featureFlagDefinition, request);
    }
}