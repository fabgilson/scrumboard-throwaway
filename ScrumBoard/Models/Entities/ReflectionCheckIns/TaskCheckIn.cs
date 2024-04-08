using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.ReflectionCheckIns;

public class TaskCheckIn : IId
{
    [Key]
    public long Id { get; set; }
    
    public long WeeklyReflectionCheckInId { get; set; }
    [ForeignKey(nameof(WeeklyReflectionCheckInId))]
    public WeeklyReflectionCheckIn WeeklyReflectionCheckIn { get; set; }
    
    public long TaskId { get; set; }
    [ForeignKey(nameof(TaskId))]
    public UserStoryTask Task { get; set; }
    
    public CheckInTaskDifficulty CheckInTaskDifficulty { get; set; }
    
    public CheckInTaskStatus CheckInTaskStatus { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime? LastUpdated { get; set; }
}