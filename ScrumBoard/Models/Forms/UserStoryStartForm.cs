using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities;
using System.Linq;
using System;

namespace ScrumBoard.Models.Forms
{
    public class UserStoryStartForm
    {
        public UserStory Story { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Story must have estimate")]
        public int Estimate { get; set; }

        [MinLength(1, ErrorMessage = "Must have at least one task")]
        [CustomValidation(typeof(UserStoryStartForm), "ValidateTasks")]
        public IEnumerable<UserStoryTaskStartForm> Tasks { get; set; }

        public UserStoryStartForm(UserStory story) {
            Story = story;
            Estimate = story.Estimate;
            Tasks = story.Tasks.Select(task => new UserStoryTaskStartForm() { Estimate = task.Estimate}).ToList();
        }

        public static ValidationResult ValidateTasks(ICollection<UserStoryTaskStartForm> tasks, ValidationContext context) {
            var sprintStartForm = context.ObjectInstance as SprintForm;

            List<ValidationResult> validationResults = new();
            bool valid = true;

            foreach (UserStoryTaskStartForm task in tasks) {
                ValidationContext taskContext = new ValidationContext(task);
                bool taskValid = Validator.TryValidateObject(task, taskContext, validationResults, true);
                valid &= taskValid;
            }

            if (valid) {
                return ValidationResult.Success;
            } else {
                return new ValidationResult("Some tasks are missing estimates", new[] { context.MemberName });
            }
        }
    }

    public class UserStoryTaskStartForm
    {
        [Range(typeof(TimeSpan), "00:00:01", "100.00:00", ErrorMessage = "Task must have estimate")]
        public TimeSpan Estimate { get; set; }
    }
}