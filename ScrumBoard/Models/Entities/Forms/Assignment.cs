using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Entities.Forms;

public enum AssignmentType
{
    Individual,
    Pairwise,
    Team
}

public class Assignment
{
    
    [Key]
    public long Id { get; set; }
    
    public long FormTemplateId { get; set; }

    [ForeignKey(nameof(FormTemplateId))]
    public FormTemplate FormTemplate { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public long RunNumber { get; set; }
    
    public AssignmentType AssignmentType { get; set; }
    
    public bool AllowSavingBeforeStartDate { get; set; }

    public ICollection<FormInstance> Instances { get; set; }
}