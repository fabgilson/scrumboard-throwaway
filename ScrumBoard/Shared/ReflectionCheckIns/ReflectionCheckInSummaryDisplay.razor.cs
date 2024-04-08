using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.ReflectionCheckIns;

public partial class ReflectionCheckInSummaryDisplay : BaseProjectScopedComponent
{
    [Parameter]
    public User SelectedUser { get; set; }

    [Parameter, EditorRequired]
    public ICollection<WeeklyReflectionCheckIn> CheckIns { get; set; }
    private ICollection<WeeklyReflectionCheckIn> _priorCheckIns;

    [Inject]
    protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }
    
    private IDictionary<TaskCheckIn, TimeSpentOnTask> _timeSpentOnTaskCheckIns;
    private NamedSeries _timeSpentAsNamedSeries = GenerateNamedColoredValuesForTime(_ => TimeSpan.Zero);
    private NamedSeries _taskCountAsNamedSeries = GenerateNamedColoredValuesForCount(_ => 0);
    
    private DurationFormatOptions _durationFormatOptions = DurationFormatOptions.FormatForLongString | DurationFormatOptions.IgnoreSecondsInOutput;
    
    private static SemaphoreSlim _semaphore = new(1, 1);
    private bool _isLoading = false;
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!CheckInsProvidedHaveChanged()) return;
        _priorCheckIns = CheckIns;
        
        if (!_isLoading && CheckIns is not null)
        {
            _isLoading = true;
            await _semaphore.WaitAsync();
            try
            {
                await UpdateStats();
            }
            finally
            {
                _semaphore.Release();
                _isLoading = false;
            }
        }
    }
    
    private bool CheckInsProvidedHaveChanged()
    {
        if (CheckIns is null) return false;
        if (_priorCheckIns is null && CheckIns is not null) return true;
        var oldPrimaryKeyAttrs = _priorCheckIns?.Select(x => x.Id).ToList() ?? [];
        var newPrimaryKeyAttrs = CheckIns?.Select(x => x.Id).ToList() ?? [];
        if (oldPrimaryKeyAttrs.Count != newPrimaryKeyAttrs.Count) return true;
        if (oldPrimaryKeyAttrs.Any(x => !newPrimaryKeyAttrs.Contains(x))) return true;
        if (newPrimaryKeyAttrs.Any(x => !oldPrimaryKeyAttrs.Contains(x))) return true;
        return false;
    }

    private async Task UpdateStats()
    {
        _timeSpentOnTaskCheckIns = await WeeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync(
            CheckIns.SelectMany(x => x.TaskCheckIns).ToList(),
            SelectedUser?.Id ?? Self.Id
        );
        _timeSpentAsNamedSeries = GenerateNamedColoredValuesForTime(TimeSpentForDifficulty);
        _taskCountAsNamedSeries = GenerateNamedColoredValuesForCount(CountForDifficulty);
        StateHasChanged();
    }

    private int CountForDifficulty(CheckInTaskDifficulty? difficulty)
    {
        if (CheckIns is null) return 0;
        return CheckIns
            .SelectMany(x => x.TaskCheckIns)
            .Count(c => difficulty is null || c.CheckInTaskDifficulty == difficulty);
    }
    
    private TimeSpan TimeSpentForDifficulty(CheckInTaskDifficulty? difficulty)
    {
        if (CheckIns is null) return TimeSpan.Zero;
        if(_timeSpentOnTaskCheckIns is null) return TimeSpan.Zero;
        return _timeSpentOnTaskCheckIns
            .Where(c => difficulty is null || c.Key.CheckInTaskDifficulty == difficulty)
            .Sum(x => x.Value.TotalTime);
    }

    private static NamedSeries GenerateNamedColoredValuesForTime(Func<CheckInTaskDifficulty?, TimeSpan> generatorFunc)
    {
        var values = new List<NamedValue>
        {
            new() { Name = "Very easy", Value = generatorFunc(CheckInTaskDifficulty.VeryEasy).TotalHours, },
            new() { Name = "Easy", Value = generatorFunc(CheckInTaskDifficulty.Easy).TotalHours },
            new() { Name = "Medium", Value = generatorFunc(CheckInTaskDifficulty.Medium).TotalHours },
            new() { Name = "Hard", Value = generatorFunc(CheckInTaskDifficulty.Hard).TotalHours },
            new() { Name = "Very hard", Value = generatorFunc(CheckInTaskDifficulty.VeryHard).TotalHours },
        };
        return new NamedSeries
        {
            Name = "Time spent (hours)",
            NamedValues = values,
            Color = "rgba(50, 50, 255, 0.8)",
            BorderColor = "rgba(0, 0, 0, 0.8)",
        };
    }
    
    private static NamedSeries GenerateNamedColoredValuesForCount(Func<CheckInTaskDifficulty?, int> generatorFunc)
    {
        var values =  new List<NamedValue>
        {
            new() { Name = "Very easy", Value = generatorFunc(CheckInTaskDifficulty.VeryEasy) },
            new() { Name = "Easy", Value = generatorFunc(CheckInTaskDifficulty.Easy) },
            new() { Name = "Medium", Value = generatorFunc(CheckInTaskDifficulty.Medium)},
            new() { Name = "Hard", Value = generatorFunc(CheckInTaskDifficulty.Hard) },
            new() { Name = "Very hard", Value = generatorFunc(CheckInTaskDifficulty.VeryHard)},
        };
        return new NamedSeries
        {
            Name = "Number of check-ins",
            NamedValues = values,
            Color = "rgba(0, 120, 0, 0.8)",
            BorderColor = "rgba(0, 0, 0, 0.8)",
        };
    }
}