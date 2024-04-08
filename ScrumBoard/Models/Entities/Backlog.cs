using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities
{
    public class Backlog : StoryGroup
    {      
        public long BacklogProjectId { get; set; }

        /// <summary> 
        /// Reference to the project that owns this backlog
        /// NOTE: This uses a different relation then used by the Sprint, so that there is no overlap
        /// </summary>
        [Required]
        [ForeignKey(nameof(BacklogProjectId))]
        public override Project Project { get; set; }
    }
}
