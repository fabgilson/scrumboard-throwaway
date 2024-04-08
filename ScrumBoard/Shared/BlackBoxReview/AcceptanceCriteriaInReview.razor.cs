using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets.SaveStatus;

namespace ScrumBoard.Shared.BlackBoxReview;

public partial class AcceptanceCriteriaInReview : BaseProjectScopedComponent
{
    [Parameter]
    public AcceptanceCriteria AcceptanceCriteria { get; set; }
    
    [Parameter]
    public bool Disabled { get; set; }
    
    [Inject]
    public IAcceptanceCriteriaService AcceptanceCriteriaService { get; set; }
    
    [Inject]
    protected IEntityLiveUpdateConnectionBuilder LiveUpdateConnectionBuilder { get; set; }

    private Guid _editingSessionGuid = Guid.NewGuid();

    private CancellationTokenSource _debounceCts;
    private FormSaveStatus? _saveStatus;
    
    private AcceptanceCriteriaReviewForm _form;
    private EditContext _editContext;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if(_form is not null) return;
        
        _form = new AcceptanceCriteriaReviewForm
        {
            Status = AcceptanceCriteria.Status,
            ReviewComments = AcceptanceCriteria.ReviewComments
        };
        _editContext = new EditContext(_form);

        RegisterNewLiveEntityUpdateHandler<AcceptanceCriteria>(AcceptanceCriteria.Id, (newValue, _) =>
        {
            AcceptanceCriteria.ReviewComments = newValue.ReviewComments;
            AcceptanceCriteria.Status = newValue.Status;
            _form.ReviewComments = newValue.ReviewComments;
            _form.Status = newValue.Status;
            StateHasChanged();
        });
    }

    public bool Validate()
    {
        return _editContext.Validate();
    }

    private async Task UpdateAcceptanceCriteriaReviewState()
    {
        if (!_editContext.Validate())
        {
            _saveStatus = FormSaveStatus.Unsaved;
            return;
        }
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            AcceptanceCriteria.Id, 
            Self.Id,
            _form.Status!.Value, 
            _form.ReviewComments,
            _editingSessionGuid
        );
        _saveStatus = FormSaveStatus.Saved;
    }
    
    private async Task SetAcceptanceCriteriaStatus(AcceptanceCriteriaStatus status)
    {
        _form.Status = status;
        await UpdateAcceptanceCriteriaReviewState();
    }

    private async Task OnReviewCommentsChange()
    {
        // Cancel previous debounce timer
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            _saveStatus = FormSaveStatus.Saving;
            await BroadcastUpdateBegun<AcceptanceCriteria>(AcceptanceCriteria.Id);
            await Task.Delay(1000, _debounceCts.Token);
            
            await UpdateAcceptanceCriteriaReviewState();
        }
        catch (TaskCanceledException)
        {
            // Ignore if the delay was cancelled
        }
    }
}