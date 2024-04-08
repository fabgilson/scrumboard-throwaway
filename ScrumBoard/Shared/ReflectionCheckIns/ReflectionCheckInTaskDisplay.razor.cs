using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.ReflectionCheckIns;

public partial class ReflectionCheckInTaskDisplay : BaseProjectScopedComponent
{
    [Inject] protected IUserStatsService UserStatsService { get; set; }
    [Inject] protected IUserStoryTaskService UserStoryTaskService { get; set; }
    [Inject] protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }
    [Inject] protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }
    
    [Parameter, EditorRequired] public long WeeklyCheckInId { get; set; }
    [Parameter, EditorRequired] public long TaskId { get; set; }
    [Parameter, EditorRequired] public int Year { get; set; }
    [Parameter, EditorRequired] public int IsoWeekNumber { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private UserStoryTask _task;
    private TaskCheckIn _taskCheckIn;
    private bool _showFullTaskInfo;
    
    private const DurationFormatOptions SelectedDurationFormatOptions =
        DurationFormatOptions.FormatForLongString
        | DurationFormatOptions.IgnoreSecondsInOutput
        | DurationFormatOptions.UseDaysAsLargestUnit;

    private string CssClassForCompletionStatus => _taskCheckIn?.CheckInTaskDifficulty is not null and not CheckInTaskDifficulty.None
        ? "success"
        : "info";
    
    private DateTime? _timeAssignedToTask;
    private TimeSpan? TimeSinceAssigned => _timeAssignedToTask.HasValue ? DateTime.Now - _timeAssignedToTask.Value : null;
    private string TimeAndDateAssignedString => _timeAssignedToTask?.ToString("ddd dd MMM',' h:mm tt") ?? "";
    private string TimeSinceAssignedString => TimeSinceAssigned.HasValue
        ? DurationUtils.DurationStringFrom(TimeSinceAssigned.Value, SelectedDurationFormatOptions) + " ago"
        : "Not assigned";
    
    private TimeSpentOnTask _tagsWorkedByUserSinceLastStandUp;
    private string TotalTimeSpentThisWeek => DurationUtils.DurationStringFrom(
        _tagsWorkedByUserSinceLastStandUp.TotalTime, SelectedDurationFormatOptions);
    
    private TimeSpentOnTask _tagsWorkedByUserOverall;
    private StoryTaskDisplayPanel _storyTaskDisplayPanel;

    private string OverallTimeSpentOnTaskByUserString => DurationUtils.DurationStringFrom(
        _tagsWorkedByUserOverall.TotalTime, SelectedDurationFormatOptions);
    
    private string CurrentTaskEstimateString => DurationUtils.DurationStringFrom(_task.Estimate, SelectedDurationFormatOptions);
    private string OriginalTaskEstimateString => DurationUtils.DurationStringFrom(_task.OriginalEstimate, SelectedDurationFormatOptions);

    private DateTime StartOfWeek => ISOWeek.ToDateTime(Year, IsoWeekNumber, DayOfWeek.Monday);

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if(_taskCheckIn is not null) return;
        await RefreshTaskCheckIn();
        await LoadStats();
    }

    private async Task RefreshTaskCheckIn()
    { 
        if(WeeklyCheckInId == default) return;
        _taskCheckIn = await WeeklyReflectionCheckInService.GetTaskCheckInAsync(WeeklyCheckInId, TaskId);
        _taskCheckIn ??= new TaskCheckIn
        {
            TaskId = TaskId 
        };
    }

    private async Task LoadStats()
    {
        _task = await UserStoryTaskRepository.GetByIdAsync(TaskId, UserStoryTaskIncludes.Story);
        _timeAssignedToTask = await UserStoryTaskService.GetTimeUserWasAssignedToTask(_task, Self);
        _tagsWorkedByUserOverall = await UserStatsService.TagsWorkOnTaskByUser(TaskId, Self.Id);

        _tagsWorkedByUserSinceLastStandUp = await UserStatsService.TagsWorkOnTaskByUser(
            TaskId, 
            Self.Id, 
            start: StartOfWeek
        );
    }

    private async Task Save()
    {
        await WeeklyReflectionCheckInService.SaveTaskCheckInAsync(_taskCheckIn, WeeklyCheckInId);
        await RefreshTaskCheckIn();
    }
}