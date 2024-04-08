using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Utils;
using Timer = System.Timers.Timer;

namespace ScrumBoard.Shared.Widgets;

/// <summary>
/// <para>
/// Component used for rendering a text display (and only text display, i.e. not wrapped in any html tags) of some live time.
/// </para>
/// 
/// <para>
/// If no text formatting function is specified, will default to displaying the time remaining (which will be negative
/// if target time is after current time) from the current time until target time with days as the largest unit, and
/// only displaying the two highest units.
/// </para>
///
/// <para>
/// For example:
/// <list type="bullet">
///     <item>2 days, 45 minutes</item>
///     <item>2 days, 45 seconds</item>
///     <item>-3 days, 5 hours</item>
/// </list>
/// </para>
/// </summary>
public partial class LiveTimeText : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public DateTime? TargetTime { get; set; }
    
    /// <summary>
    /// Optional. If specified, will be used to format given datetime as string, otherwise, a default format will be used.
    /// The first parameter corresponds to the current datetime, and the second parameter to the target datetime.
    /// </summary>
    [Parameter]
    public Func<DateTime, DateTime, string> DateTimeFormatFunc { get; set; } = (now, target) => DurationUtils.DurationStringFrom(
        target.Subtract(now), 
        DurationFormatOptions.FormatForLongString 
        | DurationFormatOptions.TakeTwoHighestUnitsOnly
        | DurationFormatOptions.UseDaysAsLargestUnit
    );

    [Parameter]
    public int RefreshPeriodInMilliseconds { get; set; } = 1000;

    [Parameter]
    public EventCallback OnTimerPassesNow { get; set; } = EventCallback.Empty;
    
    // Use clock abstraction to allow for it to be mocked in unit tests
    [Inject] 
    protected IClock Clock { get; set; }

    private readonly CancellationTokenSource _timerCancellationTokenSource = new();
    private string _timeDisplayText = "...";
    private bool _lastTickWasBeforeTarget;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _timeDisplayText = DateTimeFormatFunc(Clock.Now, TargetTime!.Value);
        _lastTickWasBeforeTarget = TargetTime < Clock.Now;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshPeriodInMilliseconds));
        while (!_timerCancellationTokenSource.IsCancellationRequested && await timer.WaitForNextTickAsync())
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        if(TargetTime is null) return;
        _timeDisplayText = DateTimeFormatFunc(Clock.Now, TargetTime.Value);
        var currentTickWasBeforeTarget = Clock.Now < TargetTime;
        // If the last tick was before the target time, but this tick is after the target time, call the event handler
        if (_lastTickWasBeforeTarget && !currentTickWasBeforeTarget)
        {
            InvokeAsync(OnTimerPassesNow.InvokeAsync);
        }
        _lastTickWasBeforeTarget = currentTickWasBeforeTarget;
        InvokeAsync(StateHasChanged);
    }
    
    public void Dispose()
    {
        _timerCancellationTokenSource.Cancel();
        _timerCancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}

