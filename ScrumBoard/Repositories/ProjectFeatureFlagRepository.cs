using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Repositories;

public interface IProjectFeatureFlagRepository : IRepository<ProjectFeatureFlag>
{
    Task<bool> ProjectHasFeatureFlagAsync(long projectId, FeatureFlagDefinition featureFlagDefinition);
    Task DeleteProjectFeatureFlag(long projectId, FeatureFlagDefinition featureFlagDefinition);
    Task<PaginatedList<Project>> GetPaginatedProjectsForFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, int pageNum, int pageSize);
    Task<VirtualizationResponse<Project>> GetVirtualizedProjectsWithoutFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, VirtualizationRequest<Project> request);
}

public class ProjectFeatureFlagRepository : Repository<ProjectFeatureFlag>, IProjectFeatureFlagRepository
{
    public ProjectFeatureFlagRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<ProjectFeatureFlagRepository> logger) 
        : base(dbContextFactory, logger) { }

    /// <summary>
    /// Check whether a given project has a feature flag set.
    /// </summary>
    /// <param name="projectId">The ID of the project for which to check feature flag assignment.</param>
    /// <param name="featureFlagDefinition">The specific feature flag to look for.</param>
    /// <returns>True if a matching feature flag exists for the given project, false otherwise</returns>
    public async Task<bool> ProjectHasFeatureFlagAsync(long projectId, FeatureFlagDefinition featureFlagDefinition)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProjectFeatureFlags.AnyAsync(x =>
            x.ProjectId == projectId && x.FeatureFlag == featureFlagDefinition);
    }

    /// <summary>
    /// Deletes a given feature flag for a project from the database.
    /// </summary>
    /// <param name="projectId">Project to remove feature flag from</param>
    /// <param name="featureFlagDefinition">Feature flag to remove from project</param>
    /// <exception cref="KeyNotFoundException">Thrown if no such feature flag for project is given</exception>
    public async Task DeleteProjectFeatureFlag(long projectId, FeatureFlagDefinition featureFlagDefinition)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var flagToDelete = await context.ProjectFeatureFlags
            .Where(x => x.ProjectId == projectId && x.FeatureFlag == featureFlagDefinition)
            .FirstOrDefaultAsync();
        
        if (flagToDelete == default) throw new KeyNotFoundException("No such feature flag found for given project");

        context.ProjectFeatureFlags.Remove(flagToDelete);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns a paginated list of project feature flag
    /// </summary>
    /// <param name="featureFlagDefinition">Feature flag to retrieve projects by, according to whether the project has the flag enabled</param>
    /// <param name="pageNum">Page number of results, first page: pageNum=1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <returns></returns>
    public async Task<PaginatedList<Project>> GetPaginatedProjectsForFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, int pageNum, int pageSize)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await PaginatedList<Project>.CreateAsync(
            context.ProjectFeatureFlags
                .Where(x => x.FeatureFlag == featureFlagDefinition)
                .Select(x => x.Project)
                .OrderBy(p => p.Name),
            pageNum, 
            pageSize
        );
    }
    
    /// <summary>
    /// Gets a window of the projects that do not have a feature flag set, also returns a count of total possible results.
    /// </summary>
    /// <param name="featureFlagDefinition">Feature flag to exclude projects by.</param>
    /// <param name="request">Request object containing parameters needed for virtualization</param>
    /// <returns>Virtualization ready projects without a given feature flag</returns>
    public async Task<VirtualizationResponse<Project>> GetVirtualizedProjectsWithoutFeatureFlagAsync(FeatureFlagDefinition featureFlagDefinition, VirtualizationRequest<Project> request)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var possibleResults = context.Projects
            .Where(p => !p.FeatureFlags.Any(p => p.FeatureFlag == featureFlagDefinition))
            .Where(p => p.Name.ToLower().Contains(request.SearchQuery.ToLower()))
            .Where(p => !request.Excluded.Select(px => px.Id).Contains(p.Id))
            .OrderBy(p => p.Name);
        var totalCount = possibleResults.Count();
        var takenResults = await possibleResults.Skip(request.StartIndex).Take(request.Count).ToListAsync();
        return new VirtualizationResponse<Project>
        {
            Results = takenResults,
            TotalPossibleResultCount = totalCount
        };
    }
}