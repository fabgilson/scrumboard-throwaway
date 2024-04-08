using ScrumBoard.Validators;
using System;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Models.Forms
{
    public class ProjectCreateForm
    {
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
        public string Description { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Start date is required")]
        [DateInFuture(ErrorMessage = "Start date must be in the future")]
        [DateWithinTwoYears(ErrorMessage = "Start date must be within the next two years")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(AllowEmptyStrings = false, ErrorMessage = "End date is required")]
        [CustomValidation(typeof(ProjectCreateForm), "ValidateEndDate")]
        public DateOnly? EndDate { get; set; } = null;

        public static ValidationResult ValidateEndDate(DateOnly? endDate, ValidationContext context) {
            var projectCreateForm = context.ObjectInstance as ProjectCreateForm;            

            if (endDate != null && endDate <= projectCreateForm.StartDate) {
                return new ValidationResult("End date cannot be before start date");
            }
            return ValidationResult.Success;
        }
       
    }
}
