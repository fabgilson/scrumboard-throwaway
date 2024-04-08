using System;

namespace ScrumBoard.Models.Entities.Relationships;

public class StandUpMeetingAttendance
{
    public long UserId { get; set; }
    public User User { get; set; }
    
    public long StandUpMeetingId { get; set; }
    public StandUpMeeting StandUpMeeting { get; set; }
    
    public DateTime? ArrivedAt { get; set; } 
}
