using System;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Shared.FormCreatorComponents;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public class TextQuestionForm : QuestionForm
    {
        public TextQuestionForm()
        {
        }
        public TextQuestionForm(TextQuestion templateBlock) : base(templateBlock)
        {
            MaxResponseLength = templateBlock.MaxResponseLength;
        }

        [Range(10, 10_000, ErrorMessage = "Max Response Length must be between 10 and 10,000 characters")]
        public int MaxResponseLength { get; set; } = 1000;

        public override string BlockName => "Question";

        public override TextQuestion AsEntity => new()
        {
            Id = Id,
            Prompt = Prompt,
            Required = Required,
            MaxResponseLength = MaxResponseLength,
        };
        
        public override Type RazorComponentType => typeof(EditTextQuestion);
    }
}