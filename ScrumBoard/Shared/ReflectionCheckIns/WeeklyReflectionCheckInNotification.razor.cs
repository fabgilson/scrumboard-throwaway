using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.ReflectionCheckIns;

public partial class WeeklyReflectionCheckInNotification : BaseProjectScopedComponent
{
    [Inject] protected IClock Clock { get; set; }
    [Inject] protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }
    
    private CheckInCompletionStatus? _completionStatus;
    
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        var thisWeekCheckIn = await WeeklyReflectionCheckInService.GetCheckInForUserForIsoWeekAndYear(
            Self.Id,
            ProjectState.ProjectId,
            ISOWeek.GetWeekOfYear(Clock.Now),
            ISOWeek.GetYear(Clock.Now)
        );
        _completionStatus = thisWeekCheckIn?.CompletionStatus ?? CheckInCompletionStatus.NotYetStarted;
        StateHasChanged();
    }
}