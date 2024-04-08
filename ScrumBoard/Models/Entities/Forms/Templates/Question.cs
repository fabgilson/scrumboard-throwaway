using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.Response;

namespace ScrumBoard.Models.Entities.Forms.Templates
{
    public abstract class Question : FormTemplateBlock
    {
        [Required]
        public string Prompt { get; set; }

        [Required] 
        public bool Required { get; set; } = true;
        
        public abstract QuestionResponseForm CreateResponseForm(Answer answer);

        /// <summary>
        /// Creates a new Answer entity (of the correct type) linked to this question
        /// </summary>
        public abstract Answer CreateAnswer();
    }
}