using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ScrumBoard.Models.Entities.Relationships;

public class TaggedWorkInstance
{
    [Key]
    public long Id { get; set; }
    
    public long WorklogTagId { get; set; }
    
    [JsonIgnore]
    [ForeignKey(nameof(WorklogTagId))]
    public WorklogTag WorklogTag { get; set; }

    public long WorklogEntryId { get; set; }
    
    [JsonIgnore]
    [ForeignKey(nameof(WorklogEntryId))]
    public WorklogEntry WorklogEntry { get; set; }
    
    public TimeSpan Duration { get; set; }
}