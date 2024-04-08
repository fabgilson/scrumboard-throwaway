using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets.SaveStatus;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.ReflectionCheckIns;

public partial class PerformWeeklyReflectionCheckIn : BaseProjectScopedComponent
{
    [Inject] protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }
    [Inject] protected IClock Clock { get; set; }

    private readonly Guid _editingSessionGuid = Guid.NewGuid();
    
    private int _isoWeek;
    private int _year;
    private DateTime _currentViewingDateTime;

    private WeeklyReflectionCheckIn _checkIn;
    private ICollection<UserStoryTask> _tasksWorkedOnByUser;
    
    private CancellationTokenSource _debounceCts;
    private FormSaveStatus? _saveStatus;
    private EditForm _editForm;

    private DateTime FirstDayOfWeek => ISOWeek.ToDateTime(_checkIn.Year, _checkIn.IsoWeekNumber, DayOfWeek.Monday);
    private DateTime LastDayOfWeek => ISOWeek.ToDateTime(_checkIn.Year, _checkIn.IsoWeekNumber, DayOfWeek.Sunday);

    /// <summary>
    /// Returns true if the current viewed week is the most current week possible. I.e. we are viewing 'this' week.
    /// If this is true, we should not allow the user to go forward a week.
    /// </summary>
    private bool SelectedWeekIsMostCurrent => _year == ISOWeek.GetYear(Clock.Now) && _isoWeek == ISOWeek.GetWeekOfYear(Clock.Now);

    /// <summary>
    /// Returns true if the current viewed week is the earliest possible week that a reflection would be possible.
    /// I.e. if the week is the same week as when the project started.
    /// </summary>
    private bool SelectedWeekIsEarliestPossible =>
        _year == Project.StartDate.Year 
        && _isoWeek == ISOWeek.GetWeekOfYear(Project.StartDate.ToDateTime(TimeOnly.MinValue));
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        _currentViewingDateTime = Clock.Now;
        AssignIsoWeekAndYearFromCurrentDateTime();
        
        await RefreshCheckIn();
    }

    private void AssignIsoWeekAndYearFromCurrentDateTime()
    {
        _isoWeek = ISOWeek.GetWeekOfYear(_currentViewingDateTime);
        _year = ISOWeek.GetYear(_currentViewingDateTime);
    }

    private async Task RefreshCheckIn(bool hasChangedWeek=false)
    {
        var remoteCheckIn = await WeeklyReflectionCheckInService.GetCheckInForUserForIsoWeekAndYear(Self.Id, ProjectState.ProjectId, _isoWeek, _year);
        if (_checkIn is null || remoteCheckIn is null)
        {
            _checkIn = remoteCheckIn ?? new WeeklyReflectionCheckIn
            {
                IsoWeekNumber = _isoWeek,
                Year = _year
            };
        }
        else if (hasChangedWeek)
        {  
            _checkIn = remoteCheckIn;
        }
        else
        {
            // Here we have already loaded the check-in so we just want to update fields rather than re-assigning the object.
            // This prevents text fields from losing focus after an auto-save
            _checkIn.Id = remoteCheckIn.Id;
            _checkIn.IsoWeekNumber = remoteCheckIn.IsoWeekNumber;
            _checkIn.Year = remoteCheckIn.Year;
            _checkIn.WhatIDidWell = remoteCheckIn.WhatIDidWell;
            _checkIn.WhatIDidNotDoWell = remoteCheckIn.WhatIDidNotDoWell;
            _checkIn.WhatIWillDoDifferently = remoteCheckIn.WhatIWillDoDifferently;
            _checkIn.AnythingElse = remoteCheckIn.AnythingElse;
            _checkIn.CompletionStatus = remoteCheckIn.CompletionStatus;
        }

        _tasksWorkedOnByUser = await WeeklyReflectionCheckInService.GetTasksWorkedOrAssignedToUserForIsoWeekAndYear(
            Self.Id, 
            ProjectState.ProjectId, 
            _checkIn.IsoWeekNumber,
            _checkIn.Year,
            SelectedWeekIsMostCurrent
        );
    }

    private async Task Save(CheckInCompletionStatus completionStatus, bool skipValidation)
    {
        if (!skipValidation && _editForm.EditContext?.Validate() != true)
        {
            _saveStatus = FormSaveStatus.Unsaved;
            return;
        }
        
        _checkIn.CompletionStatus = completionStatus;
        await WeeklyReflectionCheckInService.SaveCheckInForUserAsync(_checkIn, Self.Id, ProjectState.ProjectId, _editingSessionGuid);

        _saveStatus = FormSaveStatus.Saved;
        await RefreshCheckIn();
    }
    
    private async Task StartSaveCountdown()
    {
        // Cancel previous debounce timer
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            _saveStatus = FormSaveStatus.Saving;
            await Task.Delay(1000, _debounceCts.Token);
            await Save(CheckInCompletionStatus.Incomplete, false);
        }
        catch (TaskCanceledException)
        {
            // Ignore if the delay was cancelled
        }
    }
    
    private async Task MarkAsDraft()
    {
        _checkIn.WhatIDidWell ??= "";
        _checkIn.WhatIDidNotDoWell ??= "";
        _checkIn.WhatIWillDoDifferently ??= "";
        _checkIn.AnythingElse ??= "";
        await Save(CheckInCompletionStatus.Incomplete, true);
    }
    
    private async Task MarkAsFinished()
    {
        var isValid = _editForm.EditContext?.Validate() ?? false;
        if(!isValid) return;
        await Save(CheckInCompletionStatus.Completed, false);
    }

    /// <summary>
    /// Updates the view to look at a different week's check-ins
    /// </summary>
    /// <param name="changeInWeeks">Number of weeks to change, positive values move forward in time, negative values move backwards.</param>
    private async Task ChangeWeek(int changeInWeeks)
    {
        // Don't allow going past extremes of acceptable date range
        if(SelectedWeekIsMostCurrent && changeInWeeks > 0) return;
        if(SelectedWeekIsEarliestPossible && changeInWeeks < 0) return;

        _saveStatus = null;
        _currentViewingDateTime = _currentViewingDateTime.AddDays(7 * changeInWeeks);
        AssignIsoWeekAndYearFromCurrentDateTime();
        await RefreshCheckIn(true);
        
        StateHasChanged();
    }
}