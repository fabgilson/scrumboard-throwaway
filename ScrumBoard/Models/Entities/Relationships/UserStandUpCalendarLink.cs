using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Relationships;

public class UserStandUpCalendarLink
{
    public long UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
    
    public long ProjectId { get; set; }
    
    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }
    
    public string Token { get; set; }
}