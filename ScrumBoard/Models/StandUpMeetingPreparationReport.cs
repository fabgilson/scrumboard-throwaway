using System.Collections.Generic;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models;

public record StandUpMeetingPreparationReport
{
    public IList<UserStoryTask> TasksWorkedOn { get; set; }
    
    public StandUpMeeting PriorStandUpMeeting { get; set; }
}