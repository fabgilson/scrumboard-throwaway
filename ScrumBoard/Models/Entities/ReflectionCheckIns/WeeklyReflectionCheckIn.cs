using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Entities.ReflectionCheckIns;

public class WeeklyReflectionCheckIn : IId
{
    [Key]
    public long Id { get; set; }
    
    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
    
    public long ProjectId { get; set; }
    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }
    
    [Column(TypeName = "TEXT")]
    [Display(Name = "What I did well")]
    [MaxLength(5_000, ErrorMessage = "Cannot be longer than 5000 characters")]
    [NotEntirelyNumbersOrSpecialCharacters]
    [CustomValidation(typeof(WeeklyReflectionCheckIn), nameof(ValidateTextFields))]
    public string WhatIDidWell { get; set; }
    
    [Column(TypeName = "TEXT")]
    [Display(Name = "What I did not do so well")]
    [MaxLength(5_000, ErrorMessage = "Cannot be longer than 5000 characters")]
    [NotEntirelyNumbersOrSpecialCharacters]
    [CustomValidation(typeof(WeeklyReflectionCheckIn), nameof(ValidateTextFields))]
    public string WhatIDidNotDoWell { get; set; }
    
    [Column(TypeName = "TEXT")]
    [Display(Name = "What I will do differently")]
    [MaxLength(5_000, ErrorMessage = "Cannot be longer than 5000 characters")]
    [NotEntirelyNumbersOrSpecialCharacters]
    [CustomValidation(typeof(WeeklyReflectionCheckIn), nameof(ValidateTextFields))]
    public string WhatIWillDoDifferently { get; set; }
    
    [Column(TypeName = "TEXT")]
    [Display(Name = "Anything else")]
    [MaxLength(5_000, ErrorMessage = "Cannot be longer than 5000 characters")]
    [NotEntirelyNumbersOrSpecialCharacters]
    public string AnythingElse { get; set; }

    public int Year { get; set; }
    public int IsoWeekNumber { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime LastUpdated { get; set; }
    
    public CheckInCompletionStatus CompletionStatus { get; set; }
    
    public ICollection<TaskCheckIn> TaskCheckIns { get; set; }

    /// <summary>
    /// Validates that at least one of the required text fields (all but <see cref="AnythingElse"/>) has some non-white-space value
    /// </summary>
    /// <param name="_">Unused string property</param>
    /// <param name="context">Validation context</param>
    /// <returns>Validation result that is either a success, or a failure with appropriate message</returns>
    public static ValidationResult ValidateTextFields(string _, ValidationContext context)
    {
        if (context.ObjectInstance is not WeeklyReflectionCheckIn weeklyReflectionCheckIn) 
            return new ValidationResult(
                "Weekly reflection check-in form data is invalid, please reload the page to continue", 
                new[] { context.MemberName }
            );
        return string.IsNullOrWhiteSpace(weeklyReflectionCheckIn.WhatIDidWell) 
               && string.IsNullOrWhiteSpace(weeklyReflectionCheckIn.WhatIDidNotDoWell) 
               && string.IsNullOrWhiteSpace(weeklyReflectionCheckIn.WhatIWillDoDifferently)
            ? new ValidationResult(
                $"At least one of the required text fields must have a value provided",
                new[] { context.MemberName }
            ) 
            : ValidationResult.Success;
    }
}