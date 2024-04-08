using System;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms;

public class TaggedWorkInstanceForm
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "A valid worklog tag must be selected")]
    public long WorklogTagId { get; set; }
    
    [CustomValidation(typeof(DurationValidation), nameof(DurationValidation.BetweenOneMinuteAndOneDay))]
    public TimeSpan Duration { get; set; }
    
    public static TaggedWorkInstanceForm From(TaggedWorkInstance taggedWorkInstance)
    {
        return new TaggedWorkInstanceForm
        {
            WorklogTagId = taggedWorkInstance.WorklogTagId,
            Duration = taggedWorkInstance.Duration,
        };
    }
}