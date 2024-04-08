using System;
using System.Collections.Generic;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.Models.Shapes;

public interface IStandUpMeetingShape
{
    public string Name { get; set; }
    public string Location { get; set; }
    public string Notes { get; set; }
    public DateTime ScheduledStart { get; set; }
    public Sprint Sprint { get; set; }
    public TimeSpan Duration { get; set; }
    public ICollection<StandUpMeetingAttendance> ExpectedAttendances { get; set; }
}