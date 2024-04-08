using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.StandUpMeetings;

public partial class EditStandUpMeetingForm : BaseProjectScopedComponent
{
    [Parameter]
    public StandUpMeeting StandUpMeeting { get; set; }
    
    [Parameter]
    public Sprint Sprint { get; set; }
    
    [Parameter]
    public bool ForSchedulingNew { get; set; }
        
    [Parameter]
    public EventCallback CancelCallback { get; set; }
        
    [Parameter]
    public EventCallback AfterValidSubmitCallback { get; set; }
    
    [Inject]
    protected IStandUpMeetingService StandUpMeetingService { get; set; }

    private ElementReference SubmitLabel { get; set; }

    private StandUpMeetingForm _standUpMeetingForm;

    private bool _isNewStandUp;
    private InlineMessageNotification _messageNotificationComponent;

    private const DurationFormatOptions DurationFormatOptions = Utils.DurationFormatOptions.UseDaysAsLargestUnit | Utils.DurationFormatOptions.FormatForLongString;
    private bool _isCurrentlySubmitting = false;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        if (StandUpMeeting is null)
        {
            await SetUpForNewStandUpMeeting();
        }
        else
        {
            SetUpForExistingStandUpMeeting(StandUpMeeting);
        }
    }

    private void ShowSubmitMessage(string message, bool isError)
    {
        _messageNotificationComponent.TriggerMessageDisplay(message, isError);
    }

    private async Task SetUpForNewStandUpMeeting()
    {
        _isNewStandUp = true;
        StandUpMeeting = null;
        _standUpMeetingForm = new StandUpMeetingForm
        {
            Name = $"Daily Scrum #{await StandUpMeetingService.GetStandUpMeetingCountInSprintAsync(Sprint) + 1}",
            ScheduledStart = DateTime.Today.AddDays(1).AddHours(10), // Defaults to 10am tomorrow
            Duration = TimeSpan.FromMinutes(15),
            ExpectedAttendances = Project.GetWorkingMembers().Select(u => new StandUpMeetingAttendance { UserId = u.Id, User = u}).ToList(),
            Sprint = Sprint
        };
        StateHasChanged();
    }
    
    private void SetUpForExistingStandUpMeeting(StandUpMeeting standUpMeeting)
    {
        _isNewStandUp = false;
        StandUpMeeting = standUpMeeting;
        _standUpMeetingForm = StandUpMeeting.ToStandUpMeetingForm();
        StateHasChanged();
    }
    
    private async Task HandleCancel()
    {
        await CancelCallback.InvokeAsync();
    }

    private async Task HandleScheduleAnother()
    {
        await SetUpForNewStandUpMeeting();
    }

    private async Task HandleValidSubmit()
    {
        if (_isCurrentlySubmitting) {return;}
        _isCurrentlySubmitting = true;
        await Submit();
        _isCurrentlySubmitting = false;
    }

    private async Task Submit()
    {
        var newStandUpMeetingValue = _standUpMeetingForm.ToStandUpMeeting();
        if (await HasOverlapWithOtherStandUpMeetings()) return;

        if(_isNewStandUp) {
            await StandUpMeetingService.ScheduleNewStandUpMeetingForSprintAsync(newStandUpMeetingValue, Sprint, Self);
            SetUpForExistingStandUpMeeting(await StandUpMeetingService.GetByIdAsync(newStandUpMeetingValue.Id));
            ShowSubmitMessage("Stand-up meeting scheduled successfully!", false);
        } else {
            newStandUpMeetingValue.Id = StandUpMeeting.Id;
            newStandUpMeetingValue.CreatorId = StandUpMeeting.CreatorId;
            newStandUpMeetingValue.Created = StandUpMeeting.Created;
            newStandUpMeetingValue.Sprint = StandUpMeeting.Sprint;
            newStandUpMeetingValue.SprintId = StandUpMeeting.SprintId;
            newStandUpMeetingValue.StartedBy = StandUpMeeting.StartedBy;
            newStandUpMeetingValue.StartedById = StandUpMeeting.StartedById;
            await StandUpMeetingService.UpdateStandUpMeetingAsync(newStandUpMeetingValue, Self);
            SetUpForExistingStandUpMeeting(newStandUpMeetingValue);
            ShowSubmitMessage("Changes saved successfully!", false);
        }

        await AfterValidSubmitCallback.InvokeAsync();
    }

    private async Task<bool> HasOverlapWithOtherStandUpMeetings()
    {
        var overlappingStandUps = (await StandUpMeetingService
            .GetOverlappingStandUpsAsync(_standUpMeetingForm, Sprint, StandUpMeeting?.Id))
            .ToList();
        if (!overlappingStandUps.Any()) return false;
        ShowSubmitMessage($"Selected start date and duration clashes with '{overlappingStandUps.First().Name}'" +
                          (overlappingStandUps.Count > 1 ? $"and {overlappingStandUps.Count - 1} more" : ""), true);
        return true;
    }
    
    private void UpdateAssignees(ICollection<User> users) {
        _standUpMeetingForm.ExpectedAttendances = users.Select(x => new StandUpMeetingAttendance
        {
            User = x,
            UserId = x.Id,
            StandUpMeeting = StandUpMeeting,
            StandUpMeetingId = StandUpMeeting?.Id ?? default
        }).ToList();
        StateHasChanged();
    }
    /// <summary>
    /// Gets members of the team that are not already marked as attending.
    /// </summary>
    /// <returns>Task with a collection of Users</returns>
    private Task<ICollection<User>> GetValidAssignees()
    {
        return Task.FromResult<ICollection<User>>(Project.GetWorkingMembers()
            .Where(user => !_standUpMeetingForm.ExpectedAttendances.Select(u => u.UserId).Contains(user.Id))
            .ToList());
    }
}