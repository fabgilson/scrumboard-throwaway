using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets.SaveStatus;

namespace ScrumBoard.Shared.FormResponseComponents;

public partial class FormBlockTextQuestionResponse : BaseProjectScopedComponent
{
    [Inject]
    protected IFormInstanceService FormInstanceService { get; set; }
    
    [Parameter, EditorRequired]
    public long? FormInstanceId { get; set; }
    
    [Parameter, EditorRequired]
    public TextQuestion TextQuestion { get; set; }

    [Parameter, EditorRequired]
    public TextAnswerForm TextAnswerForm { get; set; }
    
    [Parameter]
    public bool IsReadOnly { get; set; }
    
    [Parameter]
    public bool ShouldChangesBeBroadcastToWholeProject { get; set; }

    private CancellationTokenSource _debounceCts;
    private FormSaveStatus? _saveStatus;

    private long? _textAnswerId;
    private bool _hasRegisteredLiveUpdatesForTextAnswer;
    private bool _hasRegisteredChangeListenerForFormInstance;

    private string _saveErrorText;
    
    protected override async Task OnParametersSetAsync()
    {
        if(FormInstanceId is null) return;
        await base.OnParametersSetAsync();
        await RefreshTextAnswer();
        RegisterChangeListenerForFormInstance();
    }

    private async Task RefreshTextAnswer()
    {
        var existingAnswer = await FormInstanceService.GetAnswerByFormInstanceAndQuestionIdAsync(FormInstanceId!.Value, TextQuestion.Id);
        if (_textAnswerId is null && existingAnswer is TextAnswer textAnswer)
        {
            _textAnswerId = existingAnswer.Id;
            TextAnswerForm.Content = textAnswer.Answer;
            StateHasChanged();
            RegisterLiveUpdateListenerForTextAnswer();
        }
    }
    
    /// <summary>
    /// If there is already a database entry for this text answer, listen for any changes.
    /// </summary>
    private void RegisterLiveUpdateListenerForTextAnswer()
    {
        if(_hasRegisteredLiveUpdatesForTextAnswer || _textAnswerId is null) return;
        _hasRegisteredLiveUpdatesForTextAnswer = true;
        RegisterNewLiveEntityUpdateHandler<TextAnswer>(_textAnswerId.Value, (newAnswerValue, _) =>
        {
            TextAnswerForm.Content = newAnswerValue.Answer;
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
        if(_hasRegisteredChangeListenerForFormInstance || _textAnswerId is not null) return;
        _hasRegisteredChangeListenerForFormInstance = true;
        RegisterListenerForEntityChanged<FormInstance>(FormInstanceId!.Value, async () =>
        {
            await RefreshTextAnswer();
        });
    }

    private async Task BroadcastUpdateStarted()
    {
        if (_textAnswerId is not null) await BroadcastUpdateBegun<TextAnswer>(_textAnswerId.Value);
    }
    
    private async Task BroadcastUpdateEnded()
    {
        if (_textAnswerId is not null) await BroadcastUpdateEnded<TextAnswer>(_textAnswerId.Value);
    }

    private async Task StartSaveCountdown()
    {
        // Cancel previous debounce timer
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            if (_textAnswerId is not null) await BroadcastUpdateStarted();
            _saveStatus = FormSaveStatus.Saving;
            await Task.Delay(1000, _debounceCts.Token);
            try
            {
                await FormInstanceService.SaveAnswerToTextFormBlock(
                    FormInstanceId!.Value, 
                    TextQuestion.Id, 
                    TextAnswerForm.Content, 
                    Self.Id,
                    ShouldChangesBeBroadcastToWholeProject
                );
                await RefreshTextAnswer();
                _saveStatus = FormSaveStatus.Saved;
                _saveErrorText = null;
            }
            catch (InvalidOperationException)
            {
                _saveStatus = FormSaveStatus.Unsaved;
                _saveErrorText = "Unable to save response because this form has already been submitted. Please refresh the page to continue";
            }

        }
        catch (TaskCanceledException)
        {
            // Ignore if the delay was cancelled
        }
    }
}