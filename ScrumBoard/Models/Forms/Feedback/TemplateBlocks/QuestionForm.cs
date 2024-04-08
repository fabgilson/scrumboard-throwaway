using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public abstract class QuestionForm : FormTemplateBlockForm
    {
        protected QuestionForm()
        {
        }
        
        protected QuestionForm(Question templateBlock) : base(templateBlock)
        {
            Prompt = templateBlock.Prompt;
            Required = templateBlock.Required;
        }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Prompt cannot be empty")]
        [MaxLength(1000, ErrorMessage = "Prompt cannot be longer than 1000 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Prompt cannot only contain numbers or special characters")]
        public string Prompt { get; set; }

        public bool Required { get; set; } = true;
    }
}