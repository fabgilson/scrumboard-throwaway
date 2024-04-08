using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Marking;

public partial class SprintSummaryTableRow : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }

    [Parameter] 
    public MarkingTableMetric Metric { get; set; }

    [Parameter] 
    public User SelectedUser { get; set; }
    
    [Parameter] 
    public IList<DateOnly> WeekStartDatesAscending { get; set; }
    
    [Parameter]
    public bool ShowTotal { get; set; }

    [Inject] 
    private IMarkingStatsService MarkingStatsService { get; set; }

    private IEnumerable<WeeklyTimeSpan> _timePerWeek = new List<WeeklyTimeSpan>();
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        
        await GenerateSummary();
    }

    private async Task GenerateSummary()
    {
        var userToFetchFor = Self;
        if (RoleInCurrentProject == ProjectRole.Leader)
        {
            userToFetchFor = SelectedUser ?? Self;
        }

        await GetHoursByWeek(userToFetchFor);
    }

    private async Task GetHoursByWeek(User userToFetchFor)
    {
        _timePerWeek = Metric switch
        {
            MarkingTableMetric.Overhead => await MarkingStatsService.GetOverheadByWeek(userToFetchFor.Id, Project.Id, Sprint?.Id),
            MarkingTableMetric.StoryHours => await MarkingStatsService.GetStoryHoursByWeek(userToFetchFor.Id, Project.Id, Sprint?.Id),
            MarkingTableMetric.TestHours => await MarkingStatsService.GetTestHoursByWeek(userToFetchFor.Id, Project.Id, Sprint?.Id),
            MarkingTableMetric.AvgLogDuration => await MarkingStatsService.GetAvgWorkLogDurationByWeek(userToFetchFor.Id, Project.Id, Sprint?.Id),
            MarkingTableMetric.ShortestWorklogDuration => await MarkingStatsService.GetShortestWorklogDurationByWeek(userToFetchFor.Id, Project.Id, Sprint?.Id),
            _ => throw new NotSupportedException()
        };
    }
    
    private static long CalculateTotalTimeSpentOnMetric(IEnumerable<WeeklyTimeSpan> weeklyTimeSpansForMetric)
    {
        return weeklyTimeSpansForMetric.Sum(wts => wts.Ticks);
    }
    
    private static int GetIsoWeekForDate(DateOnly dateOnly)
    {
        return IsoWeekCalculator.GetIsoWeekForDate(dateOnly);
    }
}