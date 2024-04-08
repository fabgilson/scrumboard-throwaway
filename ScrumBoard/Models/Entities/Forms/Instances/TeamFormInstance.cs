using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public class TeamFormInstance : FormInstance
{
    public long LinkedProjectId { get; set; }
    
    [ForeignKey(nameof(LinkedProjectId))]
    public Project LinkedProject { get; set; }
}