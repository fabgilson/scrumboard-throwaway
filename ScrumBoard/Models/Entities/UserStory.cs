using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities
{
    public class UserStory : IId, IUserStoryShape
    {
        [Key]
        public long Id { get; set; }

        public long Order { get; set; }
    
        public long ProjectId { get; set; }

        [Required]
        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; }

        public long StoryGroupId { get; set; }
        // Making this required cascade deletes Sprint-Story changelog entries
        [ForeignKey(nameof(StoryGroupId))]
        public StoryGroup StoryGroup { get; set; } // Either a sprint or the project backlog
        public long CreatorId { get; set; }

        [Required]
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int Estimate { get; set; }

        [Required]
        public Priority Priority { get; set; } = Priority.Normal;

        [Required]
        public Stage Stage { get; set; } = Stage.Todo;

        /// <summary>
        /// Comments left by the reviewers about their overall impressions of this user story
        /// </summary>
        [Display(Name = "Review Comments")]
        public string ReviewComments { get; set; }

        public ICollection<AcceptanceCriteria> AcceptanceCriterias { get; set; }

        public ICollection<UserStoryTask> Tasks { get; set; } = new List<UserStoryTask>();

        [Timestamp]
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// Clones this user story to a new user story that can be saved without updating sub-entities
        /// </summary>
        /// <returns>Clone of this user story</returns>
        public UserStory CloneForPersisting()
        {
            return new() {
                Id = Id,
                Order = Order,
                ProjectId = Project?.Id ?? ProjectId,
                StoryGroupId = StoryGroup?.Id ?? StoryGroupId,
                CreatorId = Creator?.Id ?? CreatorId,
                Created = Created,
                Name = Name,
                Description = Description,
                Estimate = Estimate,
                Priority = Priority,
                Stage = Stage,
                AcceptanceCriterias = AcceptanceCriterias,
                Tasks = null,
                ReviewComments = ReviewComments,
                RowVersion = RowVersion,
            };   
        }
    }
}
