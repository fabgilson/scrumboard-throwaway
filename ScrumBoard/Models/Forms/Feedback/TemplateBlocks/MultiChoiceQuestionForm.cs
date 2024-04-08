using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Shared.FormCreatorComponents;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms.Feedback.TemplateBlocks
{
    public class MultiChoiceQuestionForm : QuestionForm
    {
        public MultiChoiceQuestionForm()
        {
        }
        
        public MultiChoiceQuestionForm(MultiChoiceQuestion block) : base(block)
        {
            AllowMultiple = block.AllowMultiple;
            Options = block.Options.Select(option => new MultichoiceOptionForm(option)).ToList();
        }

        public bool AllowMultiple { get; set; }

        [ValidateComplexType]
        [MinLength(2, ErrorMessage = "Must provide at least 2 options")]
        public IList<MultichoiceOptionForm> Options { get; set; } = new List<MultichoiceOptionForm>();

        public override string BlockName => "Multichoice";

        public override MultiChoiceQuestion AsEntity => new() {
            Id = Id,
            Prompt = Prompt,
            Required = Required,
            AllowMultiple = AllowMultiple,
            Options = Options.Select(option => new MultiChoiceOption() { Id = option.Id, Content = option.Content }).ToList(),
        };

        public override Type RazorComponentType => typeof(EditMultichoiceQuestion);
    }

    public class MultichoiceOptionForm
    {
        public MultichoiceOptionForm()
        {
        }
        
        public MultichoiceOptionForm(MultiChoiceOption option)
        {
            Id = option.Id;
            Content = option.Content;
        }

        public long Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Option cannot be empty")]
        [MaxLength(300, ErrorMessage = "Option cannot be longer than 300 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Option cannot only contain numbers or special characters")]
        public string Content { get; set; }
    }
}