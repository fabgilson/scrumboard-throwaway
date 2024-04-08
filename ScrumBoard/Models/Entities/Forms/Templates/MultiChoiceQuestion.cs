using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public class MultiChoiceQuestion : Question
    {
        public bool AllowMultiple { get; set; }

        public ICollection<MultiChoiceOption> Options { get; set; }
        
        public override MultiChoiceAnswerForm CreateResponseForm(Answer answer) =>  new(this, answer);
        
        public override MultiChoiceQuestionForm AsForm => new(this);

        public override MultiChoiceAnswer CreateAnswer()
        {
            var instance = new MultiChoiceAnswer
            {
                QuestionId = Id
            };
            return instance;
        }
    }
}