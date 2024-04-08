using System;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Services;

namespace ScrumBoard.Extensions;

public static class StandUpExtensions
{
    public static StandUpMeeting ToStandUpMeeting(this IStandUpMeetingShape shape)
    {
        return shape.ToConcreteImplementation<StandUpMeeting>();
    }

    public static StandUpMeetingForm ToStandUpMeetingForm(this IStandUpMeetingShape shape)
    {
        return shape.ToConcreteImplementation<StandUpMeetingForm>();
    }

    private static T ToConcreteImplementation<T>(this IStandUpMeetingShape shape) where T : IStandUpMeetingShape, new()
    {
        return new T
        {
            Name = shape.Name,
            Duration = shape.Duration,
            Notes = shape.Notes,
            ScheduledStart = shape.ScheduledStart,
            ExpectedAttendances = shape.ExpectedAttendances,
            Sprint = shape.Sprint,
            Location = shape.Location
        };
    }

    public static bool UpcomingNotificationShouldBeShown(this IStandUpMeetingShape shape) =>
        shape.ScheduledStart.Subtract(StandUpMeetingService.LookForwardPeriodForUpcomingStandUp) <= DateTime.Now;
    
    public static bool CheckInHasOpenedUp(this IStandUpMeetingShape shape) =>
        shape.ScheduledStart.Subtract(StandUpMeetingService.AllowCheckInBeforeStandUpPeriod) <= DateTime.Now;

}