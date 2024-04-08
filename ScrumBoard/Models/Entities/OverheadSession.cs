using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities
{
    public class OverheadSession : ITag
    {
        [Key]
        public long Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        [NotMapped]
        public BadgeStyle Style
        {
            get => BadgeStyle.Light; 
            set => throw new NotSupportedException("Cannot set Style of OverheadSession");
        }

        public ICollection<OverheadEntry> Entries { get; set; }
    }
}
