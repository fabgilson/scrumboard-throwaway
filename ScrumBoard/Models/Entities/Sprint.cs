using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities
{
    public class Sprint : StoryGroup, ISprintShape
    {      
        [Required]
        public string Name { get; set; }
        
        public long CreatorId { get; set; }

        [Required]
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        [Required]
        public DateTime Created { get; set; }  

        [Required]
        [Display(Name = "Start Date")]
        public DateOnly StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateOnly EndDate { get; set; }

        /// <summary> Moment that the sprint was first moved from Created -> Started </summary>
        [Display(Name = "Time Started")]
        public DateTime? TimeStarted { get; set; }

        [Required]
        public SprintStage Stage { get; set; } = SprintStage.Created;

        public long SprintProjectId { get; set; }

        /// <summary> 
        /// Reference to the project that made this sprint
        /// NOTE: This uses a different relation then used by the Backlog, so that there are no conflicts
        /// </summary>
        [Required]
        [ForeignKey(nameof(SprintProjectId))]
        public override Project Project { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
        
        public ICollection<OverheadEntry> OverheadEntries { get; set; }

        /// <summary>
        /// Clones this sprint to a new sprint that can be saved without updating sub-entities
        /// </summary>
        /// <returns>Clone of this sprint</returns>
        public Sprint CloneForPersisting()
        {
            return new()
            {
                Id = Id,
                Name = Name,
                CreatorId = Creator?.Id ?? CreatorId,
                Created = Created,
                StartDate = StartDate,
                EndDate = EndDate,
                TimeStarted = TimeStarted,
                Stage = Stage,
                SprintProjectId = Project?.Id ?? SprintProjectId,
                Stories = null,
                RowVersion = RowVersion
            };
        }
    }
}
