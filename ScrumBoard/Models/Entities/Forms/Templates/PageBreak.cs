using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public class PageBreak : FormTemplateBlock
    {
        public override PageBreakForm AsForm => new(this);
    }
}