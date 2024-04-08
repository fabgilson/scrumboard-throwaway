using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Pages;

public partial class AdminStandUpSchedule : ComponentBase
{
    private bool _pageIsLoading = true;
    private bool _upcomingIsLoading = true;

    private const int PageSize = 20;

    private PaginatedList<StandUpMeeting> _upcomingStandUps = PaginatedList<StandUpMeeting>.Empty(PageSize);

    private IEnumerable<Project> _filteredProjects = new List<Project>();
    private bool _onlyShowStandUpsInActiveSprints = true;

    [Inject]
    protected IProjectService ProjectService { get; set; }
    
    [Inject] 
    protected IStandUpMeetingService StandUpMeetingService { get; set; }
    
    [Inject]
    protected IClock Clock { get; set; }
    
    protected override async Task OnParametersSetAsync()
    {
        await RefreshUpcomingStandUps(_upcomingStandUps.PageNumber);
        _pageIsLoading = false;
    }
    
    private async Task<VirtualizationResponse<Project>> SearchForProjects(VirtualizationRequest<Project> request)
    {
        return await ProjectService.GetVirtualizedProjectsAsync(request);
    }
    
    private async Task RefreshUpcomingStandUps(int pageNum)
    {
        _upcomingIsLoading = true;
        _upcomingStandUps = await StandUpMeetingService.GetPaginatedAllUpcomingStandUpsForProjects(
            pageNum, 
            PageSize, 
            _filteredProjects,
            _onlyShowStandUpsInActiveSprints
        );
        _upcomingIsLoading = false;
        StateHasChanged();
    }

    private async Task OnProjectSelectionChanged(IEnumerable<Project> filteredProjects)
    {
        _filteredProjects = filteredProjects;
        await RefreshUpcomingStandUps(1);
    }

    private async Task OnActiveSprintsCheckboxChangedAsync(ChangeEventArgs args)
    {
        _onlyShowStandUpsInActiveSprints = (bool)args.Value!;
        await RefreshUpcomingStandUps(1);
    }
}