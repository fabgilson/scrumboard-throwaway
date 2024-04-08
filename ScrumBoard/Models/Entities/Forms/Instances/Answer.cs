using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public class Answer: IId
{
    [Key]
    public long Id { get; set; }
    
    public long QuestionId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; }
    
    public long FormInstanceId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(FormInstanceId))]
    public FormInstance FormInstance { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime LastUpdated { get; set; }
}