using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets.SaveStatus;

namespace ScrumBoard.Shared.FormResponseComponents;

public partial class FormBlockMultiChoiceQuestionResponse : BaseProjectScopedComponent
{
    [Inject] protected IFormInstanceService FormInstanceService { get; set; }
    
    [Parameter, EditorRequired]
    public long? FormInstanceId { get; set; }
    
    [Parameter]
    public MultiChoiceQuestion MultiChoiceQuestion { get; set; }
    
    [Parameter]
    public MultiChoiceAnswerForm MultiChoiceAnswerForm { get; set; }
    
    [Parameter]
    public bool IsReadOnly { get; set; }
    
    [Parameter]
    public bool ShouldChangesBeBroadcastToWholeProject { get; set; }
    
    private long? _multiChoiceAnswerId;
    private bool _hasRegisteredLiveUpdatesForMultiChoiceAnswer;
    private bool _hasRegisteredChangeListenerForFormInstance;
    private FormSaveStatus? _saveStatus;

    private string _saveErrorText;

    protected override async Task OnParametersSetAsync()
    {
        if(FormInstanceId is null) return;
        await base.OnParametersSetAsync();
        await RefreshAnswer();
        RegisterChangeListenerForFormInstance();
    }
    
    private async Task RefreshAnswer()
    {
        var existingAnswer = await FormInstanceService.GetAnswerByFormInstanceAndQuestionIdAsync(FormInstanceId!.Value, MultiChoiceQuestion.Id);
        if (_multiChoiceAnswerId is null && existingAnswer is MultiChoiceAnswer multiChoiceAnswer)
        {
            _multiChoiceAnswerId = existingAnswer.Id;
            MultiChoiceAnswerForm.SetMultiChoiceSelection(multiChoiceAnswer.SelectedOptions);
            StateHasChanged();
            RegisterLiveUpdateListenerForMultiChoiceAnswer();
        }
    }
    
    /// <summary>
    /// If there is already a database entry for this answer, listen for any changes.
    /// </summary>
    private void RegisterLiveUpdateListenerForMultiChoiceAnswer()
    {
        if(_hasRegisteredLiveUpdatesForMultiChoiceAnswer || _multiChoiceAnswerId is null) return;
        _hasRegisteredLiveUpdatesForMultiChoiceAnswer = true;
        RegisterNewLiveEntityUpdateHandler<MultiChoiceAnswer>(_multiChoiceAnswerId.Value, (newAnswerValue, _) =>
        {
            MultiChoiceAnswerForm.SetMultiChoiceSelection(newAnswerValue.SelectedOptions);
            StateHasChanged();
        });
    }
    
    /// <summary>
    /// If there is NOT already a database entry for this text answer, listen for any changes to the form instance.
    /// If the form instance is changed, this may be an indication that a text answer has been saved for this question
    /// and that we need to get it and attach it to this component.
    /// </summary>
    private void RegisterChangeListenerForFormInstance()
    {
        if(_hasRegisteredChangeListenerForFormInstance || _multiChoiceAnswerId is not null) return;
        _hasRegisteredChangeListenerForFormInstance = true;
        RegisterListenerForEntityChanged<FormInstance>(FormInstanceId!.Value, async () =>
        {
            await RefreshAnswer();
        });
    }

    private async Task SaveSelection()
    {
        _saveStatus = FormSaveStatus.Saving;
        try
        {
            await FormInstanceService.SaveAnswerToMultiChoiceFormBlock(
                FormInstanceId!.Value,
                MultiChoiceQuestion.Id,
                MultiChoiceAnswerForm.Selection.Select(x => x.Id).ToList(),
                Self.Id,
                ShouldChangesBeBroadcastToWholeProject
            );
            _saveStatus = FormSaveStatus.Saved;
        }
        catch (InvalidOperationException)
        {
            _saveStatus = FormSaveStatus.Unsaved;
            _saveErrorText = "Unable to save response because this form has already been submitted. Please refresh the page to continue";
        }
    }
}