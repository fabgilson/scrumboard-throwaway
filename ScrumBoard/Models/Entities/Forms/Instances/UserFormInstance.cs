using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public class UserFormInstance : FormInstance
{
    public long AssigneeId { get; set; }

    [ForeignKey(nameof(AssigneeId))] 
    public User Assignee { get; set; }

    public long? PairId { get; set; }

    [ForeignKey(nameof(PairId))] 
    public User Pair { get; set; }
}