using System;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public abstract class FormTemplateBlockForm
    {
        protected FormTemplateBlockForm()
        {
        }

        protected FormTemplateBlockForm(FormTemplateBlock templateBlock)
        {
            Id = templateBlock.Id;
            FormPosition = templateBlock.FormPosition;
        }

        public long Id { get; set; }

        public abstract string BlockName { get; }

        public abstract FormTemplateBlock AsEntity { get; }
        
        public abstract Type RazorComponentType { get; }
        
        public long FormPosition { get; set; } = 0;
    }
}