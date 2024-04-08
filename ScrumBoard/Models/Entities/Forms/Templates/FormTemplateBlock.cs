using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public abstract class FormTemplateBlock : IId
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [ForeignKey(nameof(FormTemplateId))]
        public FormTemplate FormTemplate { get; set; }

        public long FormTemplateId { get; set; }
        
        [NotMapped]
        public abstract FormTemplateBlockForm AsForm { get; }
        
        public long FormPosition { get; set; } = 0;
    }
}