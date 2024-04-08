using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Marking;

public partial class SprintSummaryTable : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }
    
    [Parameter]
    public User SelectedUser { get; set; }
    
    [Inject]
    private IMarkingStatsService MarkingStatsService { get; set; }

    private IList<DateOnly> _weekStartDatesAscending = new List<DateOnly>();

    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await GenerateSummary();
    }
    
    private async Task GenerateSummary()
    {
        if(Project.Sprints.Count == 0) return;
        _weekStartDatesAscending = await MarkingStatsService.CalculateDateRangesForSprintOrSprints(Project.Sprints, Sprint?.Id);
    }
    
    private static int GetIsoWeekForDate(DateOnly dateOnly)
    {
        return IsoWeekCalculator.GetIsoWeekForDate(dateOnly);
    }
}