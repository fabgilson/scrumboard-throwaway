using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Models.Entities.Forms.Templates;

public class FormTemplate
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(95)")]
    public string Name { get; set; }

    [Required]
    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }

    public long CreatorId { get; set; }

    public DateTime Created { get; set; }

    public ICollection<FormTemplateBlock> Blocks { get; set; }

    public ICollection<Assignment> Assignments { get; set; }

    public long RunNumber { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}