using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Widgets;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Pages;

public partial class StandUpSchedule : BaseProjectScopedComponent
{
    [Inject]
    protected IStandUpMeetingService StandUpMeetingService { get; set; }

    private const int PageSize = 5;

    private SprintSelection _sprintSelection;
    private IEnumerable<Sprint> _availableSprints;

    private bool _upcomingIsLoading;
    private PaginatedList<StandUpMeeting> _upcomingStandUps = PaginatedList<StandUpMeeting>.Empty(PageSize);

    private bool _pastIsLoading;
    private PaginatedList<StandUpMeeting> _pastStandUps = PaginatedList<StandUpMeeting>.Empty(PageSize);

    private bool IsReadOnly => ProjectState.IsReadOnly;
    private bool _isSchedulingNewStandUp;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        RefreshSprintSelection();
        await RefreshAllStandUps();
    }
    
    private async Task RefreshAllStandUps()
    {
        if (_sprintSelection.sprint is null) return;
        await Task.WhenAll(RefreshUpcomingStandUps(_upcomingStandUps.PageNumber), RefreshPastStandUps(_pastStandUps.PageNumber));
    }
    
    private async Task RefreshUpcomingStandUps(int pageNum)
    {
        _upcomingIsLoading = true;
        _upcomingStandUps = await StandUpMeetingService.GetPaginatedUpcomingStandUpsForSprintAsync(
            _sprintSelection.sprint, pageNum, PageSize);
        _upcomingIsLoading = false;
        StateHasChanged();
    }
    
    private async Task RefreshPastStandUps(int pageNum)
    {
        _pastIsLoading = true;
        _pastStandUps = await StandUpMeetingService.GetPaginatedPastStandUpsForSprintAsync(
            _sprintSelection.sprint, pageNum, PageSize);
        _pastIsLoading = false;
        StateHasChanged();
    }

    private void RefreshSprintSelection()
    {
        _availableSprints = Project.Sprints;
        var currentSprint = Project.GetCurrentSprint();
            
        if (currentSprint?.TimeStarted != null) 
        {
            _sprintSelection = new SprintSelection { isWholeProject = false, sprint = currentSprint };
        } else {
            _sprintSelection = new SprintSelection { 
                isWholeProject = false, 
                sprint = _availableSprints.MaxBy(sprint => sprint.EndDate) 
            };                         
        }

        StateHasChanged();
    }
}