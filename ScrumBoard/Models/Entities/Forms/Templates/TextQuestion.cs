using System;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public class TextQuestion : Question
    {
        public int MaxResponseLength { get; set; }

        public override TextAnswerForm CreateResponseForm(Answer answer) => new(this, answer);

        public override TextQuestionForm AsForm => new(this);
        
        public override TextAnswer CreateAnswer()
        {
            var instance = new TextAnswer
            {
                QuestionId = Id
            };
            return instance;
        }
    }
}