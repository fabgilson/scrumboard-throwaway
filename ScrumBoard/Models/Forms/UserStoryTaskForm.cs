using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Utils;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Forms
{
    public class UserStoryTaskForm : IUserStoryTaskShape
    {
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot be longer than 200 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
        [MaxLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
        public string Description { get; set; }
        
        [CustomValidation(typeof(DurationValidation), nameof(DurationValidation.BetweenOneMinuteAndOneDay))]
        [CustomValidation(typeof(UserStoryTaskForm), "ValidateTaskEstimate")]
        public TimeSpan Estimate { get; set; }

        public ICollection<UserStoryTaskTag> Tags { get; set; } = new List<UserStoryTaskTag>();
        
        public Priority Priority { get; set; }
        
        [CustomValidation(typeof(UserStoryTaskForm), "ValidateComplexity")]
        public Complexity Complexity { get; set; }

        public ICollection<User> Assignees { get; set; } = new List<User>();

        public ICollection<User> Reviewers { get; set; } = new List<User>();

        public Stage Stage { get; set; }

        public UserStoryTaskForm() { }

        public UserStoryTaskForm(UserStoryTask task) {
            Name = task.Name;
            Description = task.Description;
            Estimate = task.Estimate;
            Tags = task.Tags.ToList();
            Priority = task.Priority;
            Complexity = task.Complexity;
            Assignees = task.GetAssignedUsers();
            Reviewers = task.GetReviewingUsers();   
            Stage = task.Stage; 
        }
        
        /// <summary>
        /// Validation function for the complexity which cannot remain unset.
        /// </summary>
        /// <param name="complexity">The UserStoryTaskForm's complexity</param>
        /// <param name="context">The current validation context</param>
        /// <returns>A validation result</returns>
        public static ValidationResult ValidateComplexity(Complexity complexity, ValidationContext context) {
            if (complexity == default) {
                return new ValidationResult("Complexity is required" , new[] { context.MemberName });
            }        
            return ValidationResult.Success;    
        }

        /// <summary>
        /// Validation function for task's estimate.
        /// - Estimate must be present
        /// </summary>
        /// <param name="Estimate"> The UserStoryTaskForm's estimate </param>
        /// <param name="context">The current validation context</param>
        /// <returns> A validation result </returns>
        public static ValidationResult ValidateTaskEstimate(TimeSpan Estimate, ValidationContext context) {
            var userStoryTaskForm = context.ObjectInstance as UserStoryTaskForm;       
            if (Estimate == default) {
                return new ValidationResult("Estimate is required" , new[] { context.MemberName });
            }        
            return ValidationResult.Success;    
        }

        ///<summary>Apply changes made in this model onto the provided user story and record the changes</summary>
        ///<param name="actingUser">User to attribute the changes to</param>
        ///<param name="task">Story task to apply the changes onto</param>
        ///<returns>List of changes, 1 per each field modified<returns>
        public List<UserStoryTaskChangelogEntry> ApplyChanges(User actingUser, UserStoryTask task) {
            Name = LanguageUtils.StripNewLines(Name);

            var changes = new List<UserStoryTaskChangelogEntry>();
            changes.AddRange(ShapeUtils.ApplyChanges<IUserStoryTaskShape>(this, task)
                .Select(fieldAndChange => new UserStoryTaskChangelogEntry(actingUser, task, fieldAndChange.Item1, fieldAndChange.Item2))
            );

            changes.AddRange(ApplyAssigneeChanges(actingUser, task));
            changes.AddRange(ApplyReviewerChanges(actingUser, task));
            changes.AddRange(ApplyTagChanges(actingUser, task));
            return changes;
        }

        ///<summary>Apply changes made in this model onto the provided user story and record the changes
        /// Specifically applies the additions and removals to the task's assigned users</summary>
        ///<param name="actingUser">User to attribute the changes to</param>
        ///<param name="task">Story task to apply the changes onto</param>
        ///<returns>List of changes, 1 per each field modified<returns>
        private List<UserTaskAssociationChangelogEntry> ApplyAssigneeChanges(User actingUser, UserStoryTask task) {
            string fieldName = "Assignee";

            ICollection<User> modelAssignees = this.Assignees;
            ICollection<User> entityAssignees = task.GetAssignedUsers();

            ICollection<User> additions = modelAssignees.Where(user => !entityAssignees.Contains(user)).ToList();
            ICollection<User> removals = entityAssignees.Where(user => !modelAssignees.Contains(user)).ToList();

            List<UserTaskAssociationChangelogEntry> changes = new List<UserTaskAssociationChangelogEntry>();

            foreach (User addedUser in additions) {
                addedUser.AssignTask(task);                
                changes.Add(new(actingUser, task, addedUser, fieldName, Change<TaskRole>.Create(TaskRole.Assigned)));
            }

            foreach (User removedUser in removals) {
                removedUser.RemoveTaskAssignment(task);
                changes.Add(new(actingUser, task, removedUser, fieldName, Change<TaskRole>.Delete(TaskRole.Assigned)));
            }

            return changes;
        }


        ///<summary>Apply changes made in this model onto the provided user story and record the changes
        /// Specifically applies the additions and removals to the task's reviewing users</summary>
        ///<param name="actingUser">User to attribute the changes to</param>
        ///<param name="task">Story task to apply the changes onto</param>
        ///<returns>List of changes, 1 per each field modified<returns>
        private List<UserTaskAssociationChangelogEntry> ApplyReviewerChanges(User actingUser, UserStoryTask task) {
            string fieldName = "Reviewer";

            ICollection<User> modelReviewers = this.Reviewers;
            ICollection<User> entityReviewers = task.GetReviewingUsers();

            ICollection<User> additions = modelReviewers.Where(user => !entityReviewers.Contains(user)).ToList();
            ICollection<User> removals = entityReviewers.Where(user => !modelReviewers.Contains(user)).ToList();

            List<UserTaskAssociationChangelogEntry> changes = new List<UserTaskAssociationChangelogEntry>();

            foreach (User addedUser in additions) {
                addedUser.ReviewTask(task);
                changes.Add(new(actingUser, task, addedUser, fieldName, Change<TaskRole>.Create(TaskRole.Reviewer)));
            }

            foreach (User removedUser in removals) {
                removedUser.RemoveTaskReview(task);
                changes.Add(new(actingUser, task, removedUser, fieldName, Change<TaskRole>.Delete(TaskRole.Reviewer)));
            }

            return changes;
        }

        private List<UserStoryTaskTagChangelogEntry> ApplyTagChanges(User actingUser, UserStoryTask task)
        {
            List<UserStoryTaskTagChangelogEntry> changes = new();

            var addedTags = Tags.ExceptBy(task.Tags.Select(tag => tag.Id), tag => tag.Id);
            foreach (var tag in addedTags)
            {
                changes.Add(UserStoryTaskTagChangelogEntry.Add(actingUser, task, tag));
            }
            
            var removedTags = task.Tags.ExceptBy(Tags.Select(tag => tag.Id), tag => tag.Id);
            foreach (var tag in removedTags)
            {
                changes.Add(UserStoryTaskTagChangelogEntry.Remove(actingUser, task, tag));
            }

            task.Tags = Tags.ToList();
            return changes;
        }
    }
}
