using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities
{
    public class UserStoryTask : IUserStoryTaskShape
    {
        [Key]
        public long Id { get; set; }
        
        [Required]
        public string Name { get; set; }
    
        public string Description { get; set; }

        [Required]
        public ICollection<UserStoryTaskTag> Tags { get; set; }
        
        [Required]
        public DateTime Created { get; set; }
        
        public long CreatorId { get; set; }

        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        public ICollection<UserTaskAssociation> UserAssociations { get; set; } = new List<UserTaskAssociation>();

        public ICollection<WorklogEntry> Worklog { get; set; } = new List<WorklogEntry>();  

        [Required]
        public Priority Priority { get; set; } = Priority.Normal;

        [Required] 
        public Complexity Complexity { get; set; }

        public long OriginalEstimateTicks { get; set; }
        
        /// <summary> Time estimate from before the sprint this task belongs to was first started </summary>
        [NotMapped]
        public TimeSpan OriginalEstimate { get => TimeSpan.FromTicks(OriginalEstimateTicks); set => OriginalEstimateTicks = value.Ticks; }

        public long EstimateTicks { get; set; }
        
        [NotMapped]
        public TimeSpan Estimate { get => TimeSpan.FromTicks(EstimateTicks); set => EstimateTicks = value.Ticks; }

        public Stage Stage { get; set; } = Stage.Todo;      

        public long UserStoryId { get; set; }

        [ForeignKey(nameof(UserStoryId))]
        public UserStory UserStory { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
        
        
        /// <summary>
        /// Clones this task to a new task that can be saved without updating sub-entities
        /// </summary>
        /// <returns>Clone of this task</returns>
        public UserStoryTask CloneForPersisting()
        {
            return new UserStoryTask
            {
                Id               = Id,
                Name             = Name,
                Description      = Description,
                Created          = Created,
                CreatorId        = CreatorId,
                Worklog          = Worklog,
                Priority         = Priority,
                Complexity       = Complexity,
                OriginalEstimate = OriginalEstimate,
                Estimate         = Estimate,
                Stage            = Stage,
                UserStoryId      = UserStoryId, 
                RowVersion       = RowVersion,
            };
        }
    }
}
