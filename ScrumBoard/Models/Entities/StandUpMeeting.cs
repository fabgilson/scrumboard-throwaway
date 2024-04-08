using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities;

public class StandUpMeeting : IStandUpMeetingShape
{
    [Key]
    public long Id { get; set; }
    
    public DateTime Created { get; set; }
    
    [ForeignKey(nameof(SprintId))]
    public Sprint Sprint { get; set; }
    public long SprintId { get; set; }
    
    public string Name { get; set; }
    
    public string Location { get; set; }
    
    public string Notes { get; set; }
    
    public DateTime ScheduledStart { get; set; }
    
    public DateTime? ActualStart { get; set; }

    public TimeSpan Duration { get; set; }

    public ICollection<StandUpMeetingAttendance> ExpectedAttendances { get; set; }
    
    public User Creator { get; set; }
    public long CreatorId { get; set; }
    
    public User StartedBy { get; set; }
    public long? StartedById { get; set; }
}