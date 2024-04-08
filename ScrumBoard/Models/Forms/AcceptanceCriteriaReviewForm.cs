using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms;

public class AcceptanceCriteriaReviewForm
{
    [Required(ErrorMessage="Must select pass or fail")]
    public AcceptanceCriteriaStatus? Status { get; set; }

    [NotEntirelyNumbersOrSpecialCharacters]
    [MaxLength(500, ErrorMessage = "Comments cannot be longer than 500 characters")]
    [CustomValidation(typeof(AcceptanceCriteriaReviewForm), nameof(ValidateComments))]
    public string ReviewComments { get; set; } = "";

    public static ValidationResult ValidateComments(string comments, ValidationContext context)
    {
        var acceptanceCriteriaForm = context.ObjectInstance as AcceptanceCriteriaReviewForm;    
        if (acceptanceCriteriaForm.Status == AcceptanceCriteriaStatus.Fail && string.IsNullOrWhiteSpace(comments))
        {
            return new ValidationResult("Must provide reason why acceptance criteria failed" , new[] { context.MemberName });
        }
        return ValidationResult.Success;
    }
}