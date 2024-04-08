using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public class TextBlock : FormTemplateBlock
    {
        [Required]
        public string Content { get; set; }

        public override TextBlockForm AsForm => new(this);
    }
}