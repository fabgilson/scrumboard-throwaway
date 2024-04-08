using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.StandUpMeetings;

public partial class StandUpMeetingDisplay
{
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }
    
    [CascadingParameter(Name = "ProjectState")]
    public ProjectState ProjectState { get; set; }
    
    [Parameter]
    public StandUpMeeting StandUpMeeting { get; set; }
    
    [Parameter]
    public bool IsEditable { get; set; }
    
    [Inject]
    protected IStandUpMeetingService StandUpMeetingService { get; set; }

    [Inject]
    protected IProjectRepository ProjectRepository { get; set; }
    
    private bool _isEditing;
    private bool IsReadOnly => ProjectState?.IsReadOnly ?? true;
    
    private const DurationFormatOptions SelectedDurationFormatOptions =
        DurationFormatOptions.FormatForLongString
        | DurationFormatOptions.IgnoreSecondsInOutput
        | DurationFormatOptions.UseDaysAsLargestUnit;
    
    private TimeSpan DifferenceBetweenStartAndNow => StandUpMeeting.ScheduledStart < DateTime.Now
        ? DateTime.Now - (StandUpMeeting.ActualStart ?? StandUpMeeting.ScheduledStart)
        : (StandUpMeeting.ActualStart ?? StandUpMeeting.ScheduledStart) - DateTime.Now;

    private string TimeFromNowString =>
        $"{(StandUpMeeting.ScheduledStart > DateTime.Now ? "Starts in " : StandUpMeeting.ActualStart is null ? "Scheduled start was " : "Started ")}" +
        $"{DurationUtils.DurationStringFrom(DifferenceBetweenStartAndNow, SelectedDurationFormatOptions)} " +
        $"{(StandUpMeeting.ScheduledStart > DateTime.Now ? "from now" : "ago")}.";

    private string TimeAndDateString => StandUpMeeting.ScheduledStart.ToString("ddd dd MMM',' h:mm tt");
    
    [Parameter]
    public bool ShowNotesSection { get; set; }

    [Parameter]
    public bool ShowLocationSection { get; set; }

    [Parameter]
    public bool ShowProjectNameSection { get; set; }
    
    private Project _project;

    protected override async Task OnParametersSetAsync()
    {
        _project = await ProjectRepository.GetByStandUpMeetingAsync(StandUpMeeting);
    }
    
    private async Task UpdateAssignees(ICollection<User> users) {
        if (IsReadOnly) return;
        StandUpMeeting.ExpectedAttendances = users.Select(x => new StandUpMeetingAttendance
        {
            User = x,
            UserId = x.Id,
            StandUpMeeting = StandUpMeeting,
            StandUpMeetingId = StandUpMeeting.Id,
        }).ToList();
        await StandUpMeetingService.UpdateStandUpMeetingAsync(StandUpMeeting, Self);
        StateHasChanged();
    }

    private async Task RefreshStandUpMeeting()
    {
        StandUpMeeting = await StandUpMeetingService.GetByIdAsync(StandUpMeeting.Id);
    }

    /// <summary>
    /// Gets members of the team that are not already marked as attending.
    /// </summary>
    /// <returns>Task with a collection of Users</returns>
    private Task<ICollection<User>> GetValidAssignees()
    {
        return Task.FromResult<ICollection<User>>(_project.GetWorkingMembers()
            .Where(user => !StandUpMeeting.ExpectedAttendances.Select(u => u.UserId).Contains(user.Id))
            .ToList());
    }
}

