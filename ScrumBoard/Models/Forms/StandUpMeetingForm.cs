using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms;

public class StandUpMeetingForm : IStandUpMeetingShape
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
    [MaxLength(50, ErrorMessage = "Content cannot be longer than 50 characters")]
    [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Content cannot only contain numbers or special characters")]
    public string Name { get; set; }
    
    [MaxLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
    [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Location cannot only contain numbers or special characters")]
    public string Location { get; set; }
    
    [MaxLength(500, ErrorMessage = "Content cannot be longer than 500 characters")]
    [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Content cannot only contain numbers or special characters")]
    public string Notes { get; set; }
    
    [DateInFuture(ErrorMessage = "Start date must be in the future")]
    [CustomValidation(typeof(StandUpMeetingForm), nameof(ValidateScheduledStartDate))]
    public DateTime ScheduledStart { get; set; }
    
    [CustomValidation(typeof(DurationValidation), nameof(DurationValidation.BetweenFiveMinutesAndThirtyMinutes))]
    public TimeSpan Duration { get; set; }
    
    public ICollection<StandUpMeetingAttendance> ExpectedAttendances { get; set; }

    public Sprint Sprint { get; set;}

    /// <summary>
    /// Validates that the scheduled start date cannot be later than the sprint's scheduled end date
    /// </summary>
    /// <param name="scheduledStart">Scheduled start date of the stand up meeting</param>
    /// <param name="context">Validation context</param>
    /// <returns>Validation result that is either a success, or a failure with appropriate message</returns>
    public static ValidationResult ValidateScheduledStartDate(DateTime scheduledStart, ValidationContext context)
    {
        if (context.ObjectInstance is not StandUpMeetingForm standUpMeetingForm) 
            return new ValidationResult(
                "Daily Scrum form data is invalid, please reload the page to continue", 
                new[] { context.MemberName }
            );
        return scheduledStart > standUpMeetingForm.Sprint.EndDate.ToDateTime(TimeOnly.MinValue)
            ? new ValidationResult(
                $"Scheduled start cannot occur after the sprint has ended ({standUpMeetingForm.Sprint.EndDate})",
                new[] { context.MemberName }
                ) 
            : ValidationResult.Success;
    }
}