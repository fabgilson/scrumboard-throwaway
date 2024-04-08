using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Forms.Feedback.Response
{
    public class TextAnswerForm : QuestionResponseForm
    {
        private readonly TextQuestion _formTemplateBlock;

        private const int MaxResponseLengthWithoutValidation = 20_000;
        
        public TextAnswerForm(TextQuestion formTemplateBlock, Answer answer)
        {
            _formTemplateBlock = formTemplateBlock;
            if (answer is null) return;

            if (answer is TextAnswer textAnswer)
            {
                Content = textAnswer.Answer;
            }
            else
            {
                throw new TextAnswerCastException();
            }
        }
        
        [CustomValidation(typeof(TextAnswerForm), nameof(ValidateContent))]
        public string Content { get; set; } = "";
        
        public static ValidationResult ValidateContent(string content, ValidationContext context)
        {
            var textAnswerForm = (context.ObjectInstance as TextAnswerForm)!;
            var formBlock = textAnswerForm._formTemplateBlock;

            if (textAnswerForm.EnableFullValidation)
            {
                if (content.Length > formBlock.MaxResponseLength)
                {
                    return new ValidationResult($"Response cannot be longer than {formBlock.MaxResponseLength} characters", new[] {context.MemberName});
                }

                if (formBlock.Required && string.IsNullOrWhiteSpace(content))
                {
                    return new ValidationResult($"Response is required", new[] {context.MemberName});
                }
            }
            else
            {
                if (content.Length > MaxResponseLengthWithoutValidation)
                {
                    return new ValidationResult($"Response cannot be longer than {MaxResponseLengthWithoutValidation} characters", new[] {context.MemberName});
                }
            }

            return ValidationResult.Success;
        }

        public override Answer CreateAnswer(long questionId, long formInstanceId)
        {
            return new TextAnswer
            {
                QuestionId = questionId,
                FormInstanceId = formInstanceId,
                Answer = Content
            };
        }
    }
}