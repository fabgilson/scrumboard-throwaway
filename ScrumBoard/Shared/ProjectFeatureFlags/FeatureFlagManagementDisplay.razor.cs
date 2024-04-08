using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Services;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared.ProjectFeatureFlags;

public partial class FeatureFlagManagementDisplay
{
    [Inject] 
    private IProjectFeatureFlagService ProjectFeatureFlagService { get; set; }

    [Parameter]
    public FeatureFlagDefinition FeatureFlagDefinition { get; set; }
    
    [Parameter]
    public EventCallback<PaginatedList<Project>> AfterAssociatedProjectsLoadedCallback { get; set; }
    
    [CascadingParameter(Name="Self")]
    public User Self { get; set; }

    private const int PageSize = 10;
    private PaginatedList<Project> _associatedProjects = PaginatedList<Project>.Empty(PageSize);

    private IEnumerable<Project> _newAssociatedProjects = new List<Project>();
    
    private bool _isLoading;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshAssociatedProjects(_associatedProjects.PageNumber);
        }
    }

    private async Task RefreshAssociatedProjects(int pageNumber)
    {
        _isLoading = true;
        _associatedProjects = await ProjectFeatureFlagService.GetPaginatedProjectsForFeatureFlagAsync(
                FeatureFlagDefinition, pageNumber, PageSize);
        _isLoading = false;
        await AfterAssociatedProjectsLoadedCallback.InvokeAsync(_associatedProjects);
        StateHasChanged(); 
    }

    private async Task RemoveFeatureFlagFromProject(Project project)
    {
        await ProjectFeatureFlagService.DeleteFeatureFlagFromProjectAsync(project, FeatureFlagDefinition);
        await RefreshAssociatedProjects(_associatedProjects.PageNumber);
        StateHasChanged();
    }

    private async Task<VirtualizationResponse<Project>> SearchForProjects(VirtualizationRequest<Project> request)
    {
        return await ProjectFeatureFlagService.GetVirtualizedProjectsWithoutFeatureFlagAsync(FeatureFlagDefinition, request);
    }

    private async Task AddFeatureFlagToSelectedProjects()
    {
        foreach (var project in _newAssociatedProjects)
        {
            await ProjectFeatureFlagService.AddProjectFeatureFlagAsync(project, Self, FeatureFlagDefinition);
        }

        _newAssociatedProjects = new List<Project>();
        await RefreshAssociatedProjects(_associatedProjects.PageNumber);
        StateHasChanged();
    }
}