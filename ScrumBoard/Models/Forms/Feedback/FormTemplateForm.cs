using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms.Feedback
{
    public class FormTemplateForm
    {
        private string _name;

        public FormTemplateForm()
        {
        }

        public FormTemplateForm(FormTemplate formTemplate)
        {
            Name = formTemplate.Name;
            Blocks = formTemplate.Blocks.Select(block => block.AsForm).OrderBy(block => block.FormPosition).ToList();
        }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Name cannot be empty")]
        [MaxLength(50, ErrorMessage = "Name cannot be longer than 50 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
        [CustomValidation(typeof(FormTemplateForm), nameof(ValidateName))]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                DuplicateName = false;
            }
        }
        
        public bool DuplicateName { get; set; }

        [ValidateComplexType]
        [CustomValidation(typeof(FormTemplateForm), nameof(ValidateBlocks))]
        public IList<FormTemplateBlockForm> Blocks { get; set; } = new List<FormTemplateBlockForm>();

        public static ValidationResult ValidateName(string name, ValidationContext context)
        {
            var form = context.ObjectInstance as FormTemplateForm;
            if (form.DuplicateName) {
                return new ValidationResult("Name already in use", new[] {context.MemberName});
            }
            return ValidationResult.Success;
        }
        
        public static ValidationResult ValidateBlocks(IList<FormTemplateBlockForm> blocks, ValidationContext context) {
            if (!blocks.Any(block => block is QuestionForm)) {
                return new ValidationResult("At least one question must be provided", new[] {context.MemberName});
            }
            return ValidationResult.Success;
        }
    }
}