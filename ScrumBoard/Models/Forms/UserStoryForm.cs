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
    public class UserStoryForm : IUserStoryShape
    {
        public static readonly List<int> PointValues = new List<int>{
            1,
            2,
            3,
            5,
            8,
            13,
            20,
            40,
        };

        [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
        [MaxLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Point estimate is required")]
        public int? EstimateOptional { get; set; }

        public int Estimate { 
            get => EstimateOptional.Value;
            set => throw new InvalidOperationException("Cannot set SprintForm.Estimate directly, use EstimateOptional");
        }

        public Priority Priority { get; set; } = Priority.Normal;

        [MinLength(1, ErrorMessage = "There must be at least 1 acceptance criteria")]
        [ValidateComplexType]
        public List<AcceptanceCriteriaForm> AcceptanceCriterias { get; set; } = new List<AcceptanceCriteriaForm>{
            new AcceptanceCriteriaForm(),
        };

        public UserStoryForm() {}
        public UserStoryForm(UserStory story)
        {
            Name = story.Name;
            Description = story.Description;
            EstimateOptional = story.Estimate;
            Priority = story.Priority;
            AcceptanceCriterias = story.AcceptanceCriterias
                .Select(ac => new AcceptanceCriteriaForm() { Id = ac.Id, Content = ac.Content})
                .ToList();
        }


        ///<summary>Apply changes made in this model onto the provided user story and record the changes</summary>
        ///<param name="actingUser">User to attribute the changes to</param>
        ///<param name="story">User story to apply the changes onto</param>
        ///<returns>List of changes, 1 per each field modified<returns>
        public List<UserStoryChangelogEntry> ApplyChanges(User actingUser, UserStory story) {
            var changes = new List<UserStoryChangelogEntry>();

            changes.AddRange(ShapeUtils.ApplyChanges<IUserStoryShape>(this, story)
                .Select(fieldAndChange => new UserStoryChangelogEntry(actingUser.Id, story.Id, fieldAndChange.Item1, fieldAndChange.Item2))
            );

            foreach (var (existingAcceptanceCriteria, index) in story.AcceptanceCriterias
                .ToList() // Need to copy, since we're modifying this list
                .Select((value, index) => (value, index))
            ) {
                var modelAcceptanceCriteria = AcceptanceCriterias
                    .FirstOrDefault(model => model.Id == existingAcceptanceCriteria.Id);
                
                if (modelAcceptanceCriteria == default) {
                    // Acceptance criteria has been deleted
                    changes.Add(new(
                        actingUser.Id, 
                        story.Id, 
                        $"AC{existingAcceptanceCriteria.InStoryId}",
                        Change<object>.Delete(existingAcceptanceCriteria.Content)
                    ));
                    story.AcceptanceCriterias.Remove(existingAcceptanceCriteria);
                } else if (existingAcceptanceCriteria.Content != modelAcceptanceCriteria.Content) {
                    // Acceptance criteria has been updated
                    changes.Add(new(
                        actingUser.Id, 
                        story.Id, 
                        $"AC{existingAcceptanceCriteria.InStoryId}",
                        Change<object>.Update(existingAcceptanceCriteria.Content, modelAcceptanceCriteria.Content)
                    ));
                    existingAcceptanceCriteria.Content = modelAcceptanceCriteria.Content;
                }
            }

            // Add new acceptance criteria
            foreach (var modelAcceptanceCriteria in AcceptanceCriterias) {
                if (modelAcceptanceCriteria.Id == default) {
                    changes.Add(new(
                        actingUser.Id, 
                        story.Id, 
                        $"AC{story.AcceptanceCriterias.Count + 1}",
                        Change<object>.Create(modelAcceptanceCriteria.Content)
                    ));
                    story.AcceptanceCriterias.Add(new AcceptanceCriteria() { 
                        Content = modelAcceptanceCriteria.Content, 
                        UserStory = story
                    });
                }
            }
            
            // Fixup acceptance criteria InStoryIds
            var currentId = 1;
            foreach (var acceptanceCriteria in story.AcceptanceCriterias)
                acceptanceCriteria.InStoryId = currentId++;

            return changes;
        }
    }

    public class AcceptanceCriteriaForm {
        public long Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Acceptance criteria cannot be empty")]
        [MaxLength(750, ErrorMessage = "Acceptance criteria cannot be longer than 750 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Acceptance criteria cannot only contain numbers or special characters")]
        public string Content { get; set; }
    }
}
