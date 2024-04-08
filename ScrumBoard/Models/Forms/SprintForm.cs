using ScrumBoard.Validators;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using System.Linq;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Forms
{
    public class SprintForm : ISprintShape
    {
        private readonly DateOnly? _earliestDate;

        private readonly Sprint _sprint;

        public SprintForm(DateOnly? earliestDate) {            
            _earliestDate = earliestDate;
        }

        public SprintForm(DateOnly? earliestDate, Sprint sprint) {
            this.Name = sprint.Name;
            this.StartDateOptional = sprint.StartDate;
            this.EndDateOptional = sprint.EndDate;
            this._sprint = sprint;
            this.Stories = sprint.Stories.ToList(); 
            if (sprint.Stage == SprintStage.Started) {
                this.StoryStartForms = sprint.Stories.Select(story => new UserStoryStartForm(story)).ToList();
            }
            this._earliestDate = earliestDate;
            
        }
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
        [MaxLength(50, ErrorMessage = "Cannot be longer than 50 characters")]
        [NotEntirelySpecialCharacters(ErrorMessage = "Cannot only contain special characters")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Start date is required")]
        [DateWithinTwoYears(ErrorMessage = "Must be within the next two years")]
        [CustomValidation(typeof(SprintForm), "ValidateStartDate")]
        public DateOnly? StartDateOptional { get; set; }

        public DateOnly StartDate { 
            get => StartDateOptional.Value;
            set => throw new InvalidOperationException("Cannot set SprintForm.StartDate directly, use StartDateOptional");
        }
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "End date is required")]
        [CustomValidation(typeof(SprintForm), "ValidateEndDate")]
        public DateOnly? EndDateOptional { get; set; }

        public DateOnly EndDate { 
            get => EndDateOptional.Value;
            set => throw new InvalidOperationException("Cannot set SprintForm.EndDate directly, use EndDateOptional");
        }
        
        [CustomValidation(typeof(SprintForm), "ValidateStoryStartFormCount")]
        [ValidateComplexType]
        public List<UserStoryStartForm> StoryStartForms { get; set; }

        /// <summary>
        /// Validation function for sprint's story count.
        /// - If the sprint is started, check the story count is at least 1.
        /// - Does not apply when creating a sprint.
        /// </summary>
        /// <param name="userStoryStartForms"> The SprintForm's UserStoryStartForms </param>
        /// <param name="context">The current validation context</param>
        /// <returns> A validation result </returns>
        public static ValidationResult ValidateStoryStartFormCount(List<UserStoryStartForm> userStoryStartForms, ValidationContext context) {
            var sprintForm = context.ObjectInstance as SprintForm;  
            if (sprintForm._sprint != null && sprintForm._sprint.Stage == SprintStage.Started && sprintForm.StoryStartForms.Count < 1) {
                return new ValidationResult("Must have at least one story" , new[] { context.MemberName });
            }        
            return ValidationResult.Success;    
        }

        /// <summary>
        /// Validation function for sprint's end date attribute. 
        /// - Can only be edited if the sprint is in the Created or Started stage
        /// - Cannot be before start date. 
        /// - Cannot be prior to the current date
        /// </summary>
        /// <param name="endDate"> The SprintForm's end date</param>
        /// <param name="context"> The current validation context </param>
        /// <returns> A validation result </returns>
        public static ValidationResult ValidateEndDate(DateOnly? endDate, ValidationContext context) {
            var sprintForm = context.ObjectInstance as SprintForm;            

            if (sprintForm._sprint != null && sprintForm.EndDateOptional != sprintForm._sprint.EndDate) {
                if (sprintForm._sprint.Stage.IsWorkDone()) {
                    return new ValidationResult("Cannot be edited after the sprint has finished" , new[] { context.MemberName });
                }
            }

            if ((sprintForm._sprint != null && sprintForm.EndDate != sprintForm._sprint.EndDate) || sprintForm._sprint == null) {
                if (endDate <= DateOnly.FromDateTime(DateTime.Today)) {
                    return new ValidationResult("Must be in the future", new[] { context.MemberName });
                }
            }

            if (endDate <= sprintForm.StartDate) {
                return new ValidationResult("Cannot be before start date" , new[] { context.MemberName });
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Validation function for sprint's start date attribute. 
        /// - Can only be edited when the sprint is in the Created stage
        /// - Cannot be earlier than the end date of any of the previous sprints
        /// </summary>
        /// <param name="startDate"> The SprintForm's start date </param>
        /// <param name="context"> The current validation context </param>
        /// <returns> A validation result </returns>
        public static ValidationResult ValidateStartDate(DateOnly? startDate, ValidationContext context) {
            var sprintForm = context.ObjectInstance as SprintForm;

            if (sprintForm._earliestDate != null && startDate < sprintForm._earliestDate) {
                return new ValidationResult("Cannot be earlier than previous sprint end date" , new[] { context.MemberName });
            }

            if (sprintForm._sprint != null && sprintForm.StartDate != sprintForm._sprint.StartDate) {
                if (sprintForm._sprint.Stage != SprintStage.Created) {
                    return new ValidationResult("Cannot be edited after the sprint has started" , new[] { context.MemberName });
                }
            }
                

            return ValidationResult.Success;
        }

        private List<UserStory> _stories = new();

        public List<UserStory> Stories { 
            get {
                return _stories;
            }
            set {
                if (_sprint != null && _sprint.Stage == SprintStage.Started) {
                    StoryStartForms = value.Select(story => new UserStoryStartForm(story)).ToList();
                } 
                _stories = value;
            }
        }
       
        /// <summary>
        /// Applies changes made in this model onto the given sprint instance.
        /// </summary>
        /// <param name="actingUser">User to attribute the changes to</param>
        /// <param name="sprint">Sprint instance to apply changes onto</param>
        /// <returns>A list of changes, 1 per each field modified</returns>
        public List<SprintChangelogEntry> ApplyChanges(User actingUser, Sprint sprint) {
            List<SprintChangelogEntry> changes = new List<SprintChangelogEntry>();
            changes.AddRange(ShapeUtils.ApplyChanges<ISprintShape>(this, sprint)
                .Select(fieldAndChange => new SprintChangelogEntry(actingUser, sprint, fieldAndChange.Item1, fieldAndChange.Item2)));

            changes.AddRange(ApplyStoryChanges(actingUser, sprint));

            return changes;
        }

        /// <summary>
        /// Apply changes made in this model onto the provided sprint
        /// Applies additions and removals individiually, so that they can be recorded down as changelog entries. 
        /// </summary>
        /// <param name="actingUser">The current user making the changes</param>
        /// <param name="sprint">Sprint to apply the changes onto</param>
        /// <returns>A list of SprintStoryAssociationChangelogEntries from the story changes made</returns>
        private List<SprintStoryAssociationChangelogEntry> ApplyStoryChanges(User actingUser, Sprint sprint) {
            List<SprintStoryAssociationChangelogEntry> changes = new List<SprintStoryAssociationChangelogEntry>();
             List<UserStory> modelStories = this.Stories;
            List<UserStory> entityStories = sprint.Stories;

            // Changelog entries for user story changes to be implemneted 
            List<UserStory> additions = modelStories.Where(story => !entityStories.Contains(story)).ToList();
            List<UserStory> removals = entityStories.Where(story => !modelStories.Contains(story)).ToList();

            // Replace sprint stories with new ones (in order).    
            sprint.ReplaceStories(modelStories);     

            foreach (UserStory addedStory in additions) {
                changes.Add(new(actingUser, sprint, addedStory, ChangeType.Create));
            }

            foreach (UserStory removedStory in removals) {
                changes.Add(new(actingUser, sprint, removedStory, ChangeType.Delete));
            }
                  
            return changes;
        }
    }
}
