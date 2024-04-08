using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Pages;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Shared;

public partial class FormResponse : BaseProjectScopedComponent
{
    private FormTemplate _formTemplate => FormInstance is not null 
        ? FormInstance.Assignment.FormTemplate 
        : FormTemplate;

    [Parameter]
    public FormTemplate FormTemplate { get; set; }
    
    [Parameter]
    public FormInstance FormInstance { get; set; }
    
    [Parameter]
    public bool IsPreview { get; set; }
    
    [Parameter]
    public bool IsReadOnly { get; set; }
    
    [Parameter]
    public EventCallback OnClose { get; set; }
    
    [Inject]
    public IFormInstanceService FormInstanceService { get; set; }

    private FormResponseForm _model;

    private EditContext _editContext;
    
    private int _pageIndex;

    private ConfirmModal _confirmModal;

    private CancellationTokenSource _cancellationTokenSource = new();

    public bool FormCanBeEditedByWholeProject => FormInstance?.Assignment.AssignmentType is AssignmentType.Team;

    protected override async Task OnParametersSetAsync()
    {
        // Only attempt to perform base project scoped actions when not previewing a form from the admin menu
        if (FormInstance is not null) await base.OnParametersSetAsync();
        
        var answers = FormInstance?.Answers;
        
        _model = new FormResponseForm();
        _model.Pages.Clear();
        _model.Pages.Add(new BlockWithQuestionFormList());
        
        // For all blocks in the form, order them correctly then produce an enumerable of pairs of 
        // blocks with possible question forms (if the block is a question to be answered)
        var blocksWithForms = _formTemplate.Blocks.OrderBy(block => block.FormPosition)
            .Select(block =>
            {
                QuestionResponseForm responseForm = null;
                var answer = answers?.FirstOrDefault(a => a.QuestionId == block.Id);
                if (block is Question question)
                {
                    responseForm = question.CreateResponseForm(answer);
                }
                return (block, responseForm);
            });

        // Move through all blocks with forms, adding them to the correct page (or making a new page 
        // if the block is a page break)
        foreach (var blocksWithForm in blocksWithForms)
        {
            if (blocksWithForm.block is PageBreak)
            {
                if (_model.Pages.Last().BlockWithQuestionForms.Any())
                    _model.Pages.Add(new BlockWithQuestionFormList());
            }
            else
            {
                _model.Pages.Last().BlockWithQuestionForms.Add(
                    new BlockWithQuestionForm
                    {
                        Block = blocksWithForm.block, 
                        QuestionResponseForm = blocksWithForm.responseForm
                    });
            }
        }

        // Remove any trailing page breaks
        if (_model.Pages.Count > 1 && !_model.Pages.Last().BlockWithQuestionForms.Any()) 
            _model.Pages.RemoveAt(_model.Pages.Count - 1);
        
        _editContext = new EditContext(_model);
    }
    
    /// <summary> 
    /// Gets the total number of questions before the current page index.
    /// </summary>
    /// <param name="pageIndex">An index to count questions before</param>
    /// <returns>An integer containing the total number of questions before the given index.</returns>
    private int GetTotalQuestionsBefore(int pageIndex)
    {
        return _model.Pages
            .Take(pageIndex)
            .SelectMany(page => page.BlockWithQuestionForms)
            .Count(item => item.QuestionResponseForm != null);
    }

    private void SetFullValidationForAllPages(bool newValue)
    {
        foreach (var page in ((FormResponseForm)_editContext.Model).Pages)
        {
            foreach (var blockWithQuestionForm in page.BlockWithQuestionForms)
            {
                if (blockWithQuestionForm.QuestionResponseForm is not null)
                {
                    blockWithQuestionForm.QuestionResponseForm.EnableFullValidation = newValue;
                }
            }
        }
    }
    

    private void RequestValidationOnCurrentPage()
    {
        SetFullValidationForAllPages(true);
        _editContext.Validate();
    }

    /// <summary>
    /// Submits the form.
    /// </summary>
    private async Task SubmitAsync()
    {
        await _cancellationTokenSource.CancelAsync();

        SetFullValidationForAllPages(true);

        var valid = _editContext.Validate();
        if (!valid)
            return;
        
        var confirmed = await _confirmModal.Show();
        if (confirmed)
        {
            await FormInstanceService.SubmitForm(FormInstance.Id);
            NavigationManager.NavigateTo(PageRoutes.ToFillForms(Project.Id), true);
        }
    }
}