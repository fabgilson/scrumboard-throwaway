using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.Report;

public partial class MyStatistics : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }
    private Sprint _previousSprint;
    private bool _isFirstLoad = true;

    [Parameter]
    public User SelectedUser { get; set; }
    
    [Inject] 
    protected IUserRepository UserRepository { get; set; }
    
    [Inject] 
    protected IUserStatsService UserStatsService { get; set; }

    [Inject]
    protected ISprintRepository SprintRepository { get; set; }

    private IEnumerable<IStatistic> _statCardData = new List<IStatistic>();

    private IStatsBar _tagsWorked;

    private IEnumerable<IStatistic> _usersPaired = new List<IStatistic>();
    private IEnumerable<IStatistic> _usersReviewedBy = new List<IStatistic>();
    private IEnumerable<IStatistic> _usersReviewed = new List<IStatistic>();

    private int _renderKey = 0;
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        SelectedUser ??= Self; // If no user is specified, generate statistics for currently logged in user
        if(!_isFirstLoad && Sprint == _previousSprint) return;
        _isFirstLoad = false;
        _previousSprint = Sprint;
        await RefreshData();
    }

    /// <summary>
    /// Gets user and related worklogs and calls RefreshData to display statistics 
    /// for currently selected user.
    /// </summary>
    /// <param name="user">User to display statistics for</param>
    /// <returns>Task to be completed</returns>
    public async Task ChangeUser(User user)
    {
        SelectedUser = await UserRepository.GetByIdAsync(user.Id);
        await RefreshData();
    }

    /// <summary>
    /// Calls UserStatsService to get statistics for the selected user, sprint or entire project. 
    /// </summary>
    /// <returns>Task to be completed</returns>
    private async Task RefreshData()
    {        
        // Sometimes, the pre-render step of blazor hasn't properly propagated cascade parameters
        // So we'll check here if they're null, and if so, skip making any changes this render pass
        if (SelectedUser == null || Project == null) {
            return;
        }

        // Check that only leaders can view data about other users, just to be extra safe
        if (SelectedUser != null && SelectedUser.Id != Self.Id && ProjectState.ProjectRole is not ProjectRole.Leader)
        {
            return;
        }
        
        async Task StatCards() => _statCardData = await UserStatsService.GetStatCardData(SelectedUser, Project, Sprint);
        async Task TagsWorked() => _tagsWorked = await UserStatsService.TagsWorked(SelectedUser, Project, Sprint);
        async Task PairRankings() => _usersPaired = await UserStatsService.PairRankings(SelectedUser.Id, Project.Id, Sprint?.Id);
        async Task ReviewsFromTeam() => _usersReviewedBy = await UserStatsService.ReviewCountOfUserFromTeamMates(SelectedUser.Id, Project.Id, Sprint?.Id);
        async Task ReviewsForTeam() => _usersReviewed = await UserStatsService.ReviewCountOfUserForTeamMates(SelectedUser.Id, Project.Id, Sprint?.Id);

        await Task.WhenAll(StatCards(), TagsWorked(), PairRankings(), ReviewsFromTeam(), ReviewsForTeam());
        _renderKey++;
    }
}