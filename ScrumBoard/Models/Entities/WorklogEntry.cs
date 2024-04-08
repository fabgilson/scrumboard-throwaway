using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Gitlab;

namespace ScrumBoard.Models.Entities;

public class WorklogEntry
{
    [Key]
    public long Id { get; set; }

    public long UserId { get; set; }
        
    public User User { get; set; }

    public long TaskId { get; set; }

    public UserStoryTask Task { get; set; }

    [Required]
    public string Description { get; set; }
        
    /// <summary> Date and time that the work was logged </summary>
    [Required]
    public DateTime Created { get; set; }
        
    /// <summary> Date and time that the work occurred </summary>
    [Required]
    public DateTime Occurred { get; set; }

    public long? PairUserId { get; set; }

    /// <summary> User that the worklog creator worked with. Can be null </summary>
    public User PairUser { get; set; }
    
    public ICollection<TaggedWorkInstance> TaggedWorkInstances { get; set; }
    public ICollection<GitlabCommit> LinkedCommits { get; set; } = new List<GitlabCommit>();

    public TimeSpan GetTotalTimeSpent()
    {
        return new TimeSpan(TaggedWorkInstances?.Sum(x => x.Duration.Ticks) ?? 0);
    }
    
    public IEnumerable<WorklogTag> GetWorkedTags()
    {
        return TaggedWorkInstances.Select(x => x.WorklogTag);
    }
}