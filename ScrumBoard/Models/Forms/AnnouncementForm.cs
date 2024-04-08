using System;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms
{
    public class AnnouncementForm : IAnnouncementShape
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Content is required")]
        [MaxLength(1000, ErrorMessage = "Content cannot be longer than 1000 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Content cannot only contain numbers or special characters")]
        public string Content { get; set; }

        [DateInFuture(ErrorMessage = "Start date must be in the future")]
        public DateTime? Start { get; set; }

        [DateInFuture(ErrorMessage = "End date must be in the future")]
        [CustomValidation(typeof(AnnouncementForm), "ValidateEndDate")]
        public DateTime? End { get; set; }

        public bool CanBeHidden { get; set; }
        public bool ManuallyArchived { get; set; }

        public static ValidationResult ValidateEndDate(DateTime? end, ValidationContext context) {
            var announcementForm = context.ObjectInstance as AnnouncementForm;            

            if (end.HasValue && announcementForm.Start.HasValue && end.Value <= announcementForm.Start.Value) {
                return new ValidationResult("End date cannot occur before start date");
            }
            if (end.HasValue && announcementForm.Start.HasValue && end.Value.AddMinutes(-5) <= announcementForm.Start.Value) {
                return new ValidationResult("Announcement must be shown for at least 5 minutes");
            }
            return ValidationResult.Success;
        }
    }
}