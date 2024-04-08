using System;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Shared.FormCreatorComponents;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public class TextBlockForm : FormTemplateBlockForm
    {
        public TextBlockForm()
        {
        }
        public TextBlockForm(TextBlock templateBlock) : base(templateBlock)
        {
            Content = templateBlock.Content;
        }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Text cannot be empty")]
        [MaxLength(1000, ErrorMessage = "Text cannot be longer than 1000 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Text cannot only contain numbers or special characters")]
        public string Content { get; set; }

        public override string BlockName => "Text";

        public override TextBlock AsEntity => new()
        {
            Id = Id,
            Content = Content,
        };

        public override Type RazorComponentType => typeof(EditText);
    }
}