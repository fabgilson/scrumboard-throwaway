using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities
{
    public class OverheadEntry : IOverheadEntryShape
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string Description { get; set; }
        
        /// <summary> Date and time that the overhead was logged </summary>
        [Required]
        public DateTime Created { get; set; }
        
        /// <summary> Date and time that the overhead occurred </summary>
        [Required]
        public DateTime Occurred { get; set; }

        /// <summary> Duration that the overhead took </summary>
        public long DurationTicks { get; set; }

        [NotMapped]
        public TimeSpan Duration { get => TimeSpan.FromTicks(DurationTicks); set => DurationTicks = value.Ticks; }

        [Required]
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        
        public long UserId { get; set; }
        
        [Required]
        [ForeignKey(nameof(SprintId))]
        public Sprint Sprint { get; set; }
        
        public long SprintId { get; set; }
        
        [Required]
        [ForeignKey(nameof(SessionId))]
        public OverheadSession Session { get; set; }
        
        public long SessionId { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// Clones this OverheadEntry to a new OverheadEntry that can be saved without updating sub-entities
        /// </summary>
        /// <returns>Clone of this OverheadEntry</returns>
        public OverheadEntry CloneForPersisting()
        {
            return new()
            {
                Id = Id,
                Description = Description,
                Created = Created,
                Occurred = Occurred,
                Duration = Duration,
                UserId = User?.Id ?? UserId,
                SprintId = Sprint?.Id ?? SprintId,
                SessionId = Session?.Id ?? SessionId,
                RowVersion = RowVersion,
            };
        }
    }
}