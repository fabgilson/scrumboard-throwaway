using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Forms.Feedback.Response
{
    public class MultiChoiceAnswerForm : QuestionResponseForm
    {
        private readonly MultiChoiceQuestion _formBlock;
        
        public MultiChoiceAnswerForm(MultiChoiceQuestion formBlock, Answer answer)
        {
            _formBlock = formBlock;
            foreach (var option in formBlock.Options) SelectionForms.Add(option, new());
            if (answer is null) return;
            SetMultiChoiceSelection(((MultiChoiceAnswer)answer).SelectedOptions);
        }

        public void SetMultiChoiceSelection(IEnumerable<MultichoiceAnswerMultichoiceOption> selection)
        {
            var selectedOptionIds = new HashSet<long>(selection.Select(s => s.MultichoiceOptionId));

            foreach (var selectionForm in SelectionForms)
            {
                selectionForm.Value.Selected = selectedOptionIds.Contains(selectionForm.Key.Id);
            }
        }


        /// <summary>
        /// Index of the only selection within SelectionForms
        /// Is only expected to be used when AllowMultiple is false
        /// </summary>
        public int? SingleIndex
        {
            get => SelectionForms
                .Select((pair, index) => (pair.Value.Selected, index))
                .Where(newPair => newPair.Item1)
                .Select(newPair => newPair.Item2)
                .SingleOrDefault(-1);
            set
            {
                for (int i = 0; i < SelectionForms.Count; i++)
                {
                    var nowSelected = value == i;
                    SelectionForms.ElementAt(i).Value.Selected = nowSelected;
                }
            }
        }

        [CustomValidation(typeof(MultiChoiceAnswerForm), nameof(ValidateSelection))]
        public IDictionary<MultiChoiceOption, MultichoiceOptionResponseForm> SelectionForms { get; } = new Dictionary<MultiChoiceOption, MultichoiceOptionResponseForm>();
        
        public ICollection<MultiChoiceOption> Selection =>
            SelectionForms
                .Where(pair => pair.Value.Selected)
                .Select(pair => pair.Key)
                .ToList();
        
        /// <summary>
        /// Validates the multi-choice selection. This method accounts for whether the question is required or optional,
        /// and will allow any selection (or no selection) to be valid when EnableFullValidation is false.  
        /// </summary>
        /// <param name="selection">A dictionary of options and corresponding forms that contain where that selection is selected</param>
        /// <param name="context">The current validation context</param>
        /// <returns></returns>
        public static ValidationResult ValidateSelection(IDictionary<MultiChoiceOption, MultichoiceOptionResponseForm> selection, ValidationContext context)
        {
            var multiChoiceAnswerForm = (context.ObjectInstance as MultiChoiceAnswerForm)!;
            var formBlock = multiChoiceAnswerForm._formBlock;
            var selected = selection.Values.Count(value => value.Selected);

            if (!multiChoiceAnswerForm.EnableFullValidation || !formBlock.Required) return ValidationResult.Success;
            
            if (formBlock.AllowMultiple) {
                if (selected < 1)
                    return new ValidationResult("At least one answer must be chosen", new[] {context.MemberName});
            }
            else
            {
                if (selected != 1)
                    return new ValidationResult("One answer must be provided", new[] {context.MemberName});
            }

            return ValidationResult.Success;
        }
        
        /// <summary>
        /// Creates and returns a MultiChoiceAnswer entity using the current form content.
        /// </summary>
        /// <param name="questionId">The ID of the Question the answer is answering</param>
        /// <param name="formInstanceId">The ID of the related form instance for the new answer</param>
        /// <returns></returns>
        public override Answer CreateAnswer(long questionId, long formInstanceId)
        {
            return new MultiChoiceAnswer
            {
                QuestionId = questionId,
                FormInstanceId = formInstanceId,
                SelectedOptions = Selection.Select(o => new MultichoiceAnswerMultichoiceOption() { MultichoiceOptionId = o.Id, MultichoiceOption = o}).ToList()
            };
        }
    }

    public class MultichoiceOptionResponseForm
    {
        public bool Selected { get; set; }
    }
}