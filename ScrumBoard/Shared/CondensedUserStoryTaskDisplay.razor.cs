using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;

namespace ScrumBoard.Shared;

public partial class CondensedUserStoryTaskDisplay
{
    [Parameter]
    public UserStoryTask TaskModel { get; set; }
    
    [Parameter]
    public bool HidePriority { get; set; }
    
    [Parameter]
    public bool HideComplexity { get; set; }
    
    [Parameter]
    public bool HideTaskTags { get; set; }

    [Parameter]
    public bool HideEstimates { get; set; }

    [Inject]
    private IWorklogEntryService WorklogEntryService { get; set; }
    
    private TimeSpan _timeLogged;

    protected override async Task OnParametersSetAsync()
    {
        var worklogs = await WorklogEntryService.GetWorklogEntriesForTaskAsync(TaskModel.Id);
        _timeLogged = worklogs.Sum(x => x.GetTotalTimeSpent());
    }
}