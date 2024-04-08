using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services;

public interface IProjectService
{
    /// <summary>
    /// Get all projects in a format that supports virtualization and allows for excluding some results (for example
    /// if they have already been selected in some selector).
    /// </summary>
    /// <param name="request">Virtualization request with string search query</param>
    /// <returns>Virtualization ready set of projects matching search criteria</returns>
    Task<VirtualizationResponse<Project>> GetVirtualizedProjectsAsync(VirtualizationRequest<Project> request);
    
    /// <summary>
    /// Asynchronously retrieves the membership details of a user in a specific project.
    /// </summary>
    /// <param name="projectId">The identifier of the project.</param>
    /// <param name="userId">The identifier of the user.</param>
    /// <returns>
    /// The user's membership in the specified project. If the user is not a member of the project, the result is null.
    /// </returns>
    Task<ProjectUserMembership> GetUserMembershipInProjectAsync(long projectId, long userId);
}

public class ProjectService : IProjectService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public ProjectService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task<VirtualizationResponse<Project>> GetVirtualizedProjectsAsync(VirtualizationRequest<Project> request)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var possibleResults = context.Projects
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

    public async Task<ProjectUserMembership> GetUserMembershipInProjectAsync(long projectId, long userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProjectUserMemberships
            .Where(x => x.ProjectId == projectId && x.UserId == userId)
            .FirstOrDefaultAsync();
    }
}