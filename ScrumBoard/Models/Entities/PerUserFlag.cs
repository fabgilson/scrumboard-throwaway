using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities;

public enum SinglePerUserFlagType
{
    UpcomingStandUpTutorial = 1,
    ViewAllCommits = 2,
}

public class SinglePerUserFlag
{
    [Key]
    public long Id { get; set; }
    
    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
    
    public SinglePerUserFlagType FlagType { get; set; }
    
    /// <summary>
    /// Whether or not the flag is 'set'. No flag existing is synonymous to a flag existing, but this field being `false`.
    /// </summary>
    public bool IsSet { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime? LastUpdated { get; set; }
}