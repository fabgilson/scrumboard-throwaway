using System;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public class PageBreakForm : FormTemplateBlockForm
    {
        public PageBreakForm()
        {
        }
        public PageBreakForm(PageBreak templateBlock) : base(templateBlock)
        {
        }

        public override string BlockName => "Page Break";

        public override PageBreak AsEntity => new()
        {
            Id = Id,
        };
        
        public override Type RazorComponentType => null;
    }
}