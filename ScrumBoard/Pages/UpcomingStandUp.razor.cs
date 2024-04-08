using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Utils;

namespace ScrumBoard.Pages;

public partial class UpcomingStandUp : BaseProjectScopedComponent
{
    [Inject] protected IStandUpMeetingService StandUpMeetingService { get; set; }

    private StandUpMeeting _standUpMeeting;
    private bool _isLoading = true;
    
    private static string LookForwardPeriodText => DurationUtils.DurationStringFrom(
        Services.StandUpMeetingService.LookForwardPeriodForUpcomingStandUp,
        DurationFormatOptions.TakeTwoHighestUnitsOnly
        | DurationFormatOptions.FormatForLongString
        | DurationFormatOptions.UseDaysAsLargestUnit
    );

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        _standUpMeeting = await StandUpMeetingService.GetUpcomingStandUpIfPresentAsync(Self, Project);
        _isLoading = false;
    }
}