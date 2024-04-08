using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities;
using ScrumBoard.Validators;
using System.Collections.Generic;
using System;
using System.Linq;
using ScrumBoard.Models.Gitlab;

namespace ScrumBoard.Models.Forms;

public class WorklogEntryForm
{
    public WorklogEntryForm() {}

    public WorklogEntryForm(DateTime? timeStarted)
    {
        if (timeStarted.HasValue)
        {
            _timeStarted = timeStarted.Value;
        }
    }

    public WorklogEntryForm(WorklogEntry entry, DateTime? timeStarted) {
        Description = entry.Description;
        TaggedWorkInstanceForms = entry.TaggedWorkInstances.Select(TaggedWorkInstanceForm.From).ToList();
        PairUser = entry.PairUser;
        LinkedCommits = entry.LinkedCommits;
        Occurred = entry.Occurred;

        if (timeStarted.HasValue)
        {
            _timeStarted = timeStarted.Value;
        }
    }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
    [MaxLength(750, ErrorMessage = "Description cannot be longer than 750 characters")]
    [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
    public string Description { get; set; }
    
    [CollectionNotEmpty(ErrorMessage = "You must include at least one instance of logged work")]
    public ICollection<TaggedWorkInstanceForm> TaggedWorkInstanceForms { get; set; } = new List<TaggedWorkInstanceForm>();
    
    [CustomValidation(typeof(WorklogEntryForm), nameof(ValidateOccurredAfterStartDate))]
    public DateTime Occurred { get; set; } = DateTime.Now;

    /// <summary> The user that the worklog creator worked with. Can be null. </summary>
    public User PairUser { get; set; }

    private readonly DateTime _timeStarted;

    public ICollection<GitlabCommit> LinkedCommits { get; set; } = new List<GitlabCommit>();

    public static ValidationResult ValidateOccurredAfterStartDate(DateTime timeOccurred, ValidationContext context)
    {
        if (context.ObjectInstance is not WorklogEntryForm entryForm) return ValidationResult.Success;

        var occurred = entryForm.Occurred;
        if (occurred < entryForm._timeStarted)
        {
            return new ValidationResult("Cannot log work before the sprint has started", new[] { context.MemberName });
        }
        return occurred > DateTime.Now 
            ? new ValidationResult("Cannot log work in the future", new[] { context.MemberName }) 
            : ValidationResult.Success;
    }
}
