using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Utils;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms
{
    public class OverheadEntryForm : IOverheadEntryShape
    {
        public OverheadEntryForm()
        {
        }

        public OverheadEntryForm(OverheadEntry entry)
        {
            Description = entry.Description;
            Duration = entry.Duration;
            Session = entry.Session;
            Occurred = entry.Occurred;
        }
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
        [MaxLength(750, ErrorMessage = "Description cannot be longer than 750 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
        public string Description { get; set; }
        
        [CustomValidation(typeof(DurationValidation), nameof(DurationValidation.BetweenOneMinuteAndOneDay))]
        public TimeSpan Duration { get; set; }
        
        [Required(ErrorMessage = "Session is required")]
        public OverheadSession Session { get; set;  }
        
        [DateInPast(ErrorMessage = "Work cannot be in the future")]
        public DateOnly DateOccurred { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        
        [CustomValidation(typeof(OverheadEntryForm), nameof(ValidateOccurredNotInFuture))]
        public TimeOnly TimeOccurred { get; set; } = TimeOnly.FromDateTime(DateTime.Now);

        public DateTime Occurred
        {
            get => DateOccurred.ToDateTime(TimeOccurred);
            set
            {
                DateOccurred = DateOnly.FromDateTime(value);
                TimeOccurred = TimeOnly.FromDateTime(value);
            }
        }

        public static ValidationResult ValidateOccurredNotInFuture(TimeOnly timeOccurred, ValidationContext context)
        {
            if (context.ObjectInstance is OverheadEntryForm entryForm)
            {
                var occurred = entryForm.DateOccurred.ToDateTime(timeOccurred);                
                if (occurred > DateTime.Now)
                {
                    return new ValidationResult("Cannot log work in the future", new[] { context.MemberName });
                }
            }
            return ValidationResult.Success;
        }


        /// <summary>
        /// Applies changes made in this model onto the given overhead entry instance.
        /// </summary>
        /// <param name="actingUser">User to attribute the changes to</param>
        /// <param name="entry">OverheadEntry instance to apply changes onto</param>
        public List<OverheadEntryChangelogEntry> ApplyChanges(User actingUser, OverheadEntry entry)
        {
            var changes = new List<OverheadEntryChangelogEntry>();
            changes.AddRange(ShapeUtils.ApplyChanges<IOverheadEntryShape>(this, entry)
                .Select(fieldAndChange => new OverheadEntryChangelogEntry(actingUser, entry, fieldAndChange.Item1, fieldAndChange.Item2))
            );
            // Skip adding a session change if previous session is null,
            // since that should only occur when Overhead is initially created in which case the changes are ignored.
            if (entry.Session != null && Session.Id != entry.Session.Id)
            {
                changes.Add(new OverheadEntrySessionChangelogEntry(actingUser, entry, entry.Session, Session));
            }
            entry.Session = Session;

            return changes;
        }
    }
}

