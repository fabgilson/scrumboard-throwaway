using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public class MultiChoiceOption : IId
    {
        [Key]
        public long Id { get; set;}
        
        [Required]
        [ForeignKey(nameof(BlockId))]
        public MultiChoiceQuestion MultiChoiceQuestion { get; set; }

        public long BlockId { get; set; }

        [Required]
        public string Content { get; set; }
        
        /// <summary>
        /// This is a many-to-many relationship between MultichoiceAnswer and this entity.
        /// This allows the MultichoiceAnswer to store which option(s) are selected.
        /// </summary>
        public ICollection<MultichoiceAnswerMultichoiceOption> Answers { get; set; }
    }
}