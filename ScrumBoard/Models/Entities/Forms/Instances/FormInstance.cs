using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public enum FormStatus
{
    Todo,
    Upcoming,
    Started,
    Submitted
}

public class FormInstance : IId
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// ID of project to whom this form instance has been assigned.
    /// </summary>
    public long ProjectId { get; set; }
    
    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }

    public long AssignmentId { get; set; }
    
    [ForeignKey(nameof(AssignmentId))]
    public Assignment Assignment { get; set; }
    
    public FormStatus Status { get; set; }
    
    public ICollection<Answer> Answers { get; set; }
    
    public DateTime? SubmittedDate { get; set; }
}