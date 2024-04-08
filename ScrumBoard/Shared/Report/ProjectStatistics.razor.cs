using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Pages;

namespace ScrumBoard.Shared.Report;

public partial class ProjectStatistics : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }
    private Sprint _previousSprint;
    private bool _isFirstLoad = true;

    [Inject]
    protected IProjectStatsService ProjectStatsService { get; set; }
    
    private bool _viewWholeProject = false;

    private IStatsBar _timeData;
    private IStatsBar _storyData;
    private IStatsBar _taskData;
    private IStatsBar _reviewData;
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!_isFirstLoad && _previousSprint == Sprint) return;
        _isFirstLoad = false;
        _previousSprint = Sprint;
        
        await RefreshData();     
    }

    /// <summary>
    /// Calls ProjectStatsService to get statistics given current sprint or entire project.
    /// </summary>
    /// <returns>Task to be completed</returns>
    private async Task RefreshData() 
    {       
        if (Project is null) return; 
        _timeData = await ProjectStatsService.GetTimePerUser(Project.Id, Sprint?.Id);
        _storyData = await ProjectStatsService.GetStoriesWorkedPerUser(Project.Id, Sprint?.Id);
        _taskData = await ProjectStatsService.GetTasksWorkedOnPerUser(Project.Id, Sprint?.Id);
        _reviewData = await ProjectStatsService.GetStatsBarForStoriesWithReviewedTaskPerUser(Project.Id, Sprint?.Id);

        _viewWholeProject = Sprint is null;
        
        StateHasChanged();
    }

    /// <summary>
    /// Called when the project changes to refresh data on page
    /// </summary>
    private async Task HandleProjectChanged() 
    {
        await RefreshData();
    }
}