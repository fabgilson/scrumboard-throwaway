using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Models.Forms.Feedback.Response;

public abstract class QuestionResponseForm
{
    public bool EnableFullValidation { get; set; }
    public abstract Answer CreateAnswer(long questionId, long formInstanceId);
}
