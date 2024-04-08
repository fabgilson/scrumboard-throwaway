using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Forms.Feedback.Response;

// These classes are required to enable the ObjectGraphDataAnnotationsValidator to successfully evaluate all nested
// validation rules for the form response.

public class FormResponseForm
{
    [ValidateComplexType]
    public IList<BlockWithQuestionFormList> Pages { get; set; } = new List<BlockWithQuestionFormList>();
}

public class BlockWithQuestionFormList
{
    [ValidateComplexType]
    public IList<BlockWithQuestionForm> BlockWithQuestionForms { get; set; } = new List<BlockWithQuestionForm>();
}

public class BlockWithQuestionForm
{
    public FormTemplateBlock Block { get; set; }
    
    [ValidateComplexType]
    public QuestionResponseForm QuestionResponseForm { get; set; }
}