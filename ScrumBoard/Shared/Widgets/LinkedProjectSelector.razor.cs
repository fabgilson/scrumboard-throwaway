using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared.Widgets;

public partial class LinkedProjectSelector : ComponentBase
{
    [Parameter]
    public EventCallback<IEnumerable<LinkedProjects>> OnSelectionUpdated { get; set; }
    
    [Inject]
    protected IProjectService ProjectService { get; set; }
    
    private List<LinkedProjects> _currentProjects = [];
    
    private async Task<VirtualizationResponse<Project>> SearchForProjects(VirtualizationRequest<Project> request)
    {
        return await ProjectService.GetVirtualizedProjectsAsync(request);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var newLinkedProjects = new LinkedProjects();
        _currentProjects.Add(newLinkedProjects);
    }

    private async Task OnProjectSelectionChanged(Project project, bool isFirstProject, LinkedProjects linkedprojects)
    {
        if (isFirstProject)
        {
            linkedprojects.FirstProject = project;
        }
        else
        {
            linkedprojects.SecondProject = project;
        }

        await RefreshAfterSelectionChange();
    }

    private async Task RemoveProjectPair(int indexToRemove)
    {
        _currentProjects.RemoveAt(indexToRemove);
        await RefreshAfterSelectionChange();
    }

    private async Task AddEmptyProjectPair()
    {
        var newLinkedProjects = new LinkedProjects();
        _currentProjects.Add(newLinkedProjects);
        await RefreshAfterSelectionChange();
    }

    private async Task RefreshAfterSelectionChange()
    {
        await OnSelectionUpdated.InvokeAsync(_currentProjects);
    }
}