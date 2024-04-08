using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets.SaveStatus;

namespace ScrumBoard.Shared.BlackBoxReview;

public partial class StoryInReview : BaseProjectScopedComponent
{
    [Parameter]
    public long StoryId { get; set; }
    
    [Parameter]
    public EventCallback OnSuccessfulSubmit { get; set; }
    
    [Parameter]
    public RenderFragment ChildContent { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes {  get; set; }
    
    [Inject]
    protected IUserStoryChangelogRepository UserStoryChangelogRepository { get; set; }
    
    [Inject]
    protected ISprintService SprintService { get; set; }
    
    [Inject]
    protected IUserStoryService UserStoryService { get; set; }
    
    [Inject]
    protected IEntityLiveUpdateConnectionBuilder LiveUpdateConnectionBuilder { get; set; }

    // GUID unique to this editing session to allow for coalescing all changes to a single changelog entry
    private readonly Guid _reviewSessionEditingGuid = Guid.NewGuid();

    private Sprint _sprint;
    private UserStory _story;
    private StoryReviewForm _storyReviewForm;

    private EditContext _editContext;

    private bool CanEdit => _sprint.Stage == SprintStage.InReview 
        || (RoleInCurrentProject == ProjectRole.Leader && _sprint.Stage is SprintStage.Reviewed or SprintStage.Closed);

    [Parameter]
    public EventCallback OnEdit { get; set; }

    private List<UserStoryChangelogEntry> _changelog;

    [Parameter]
    public bool Disabled { get; set; }
    
    private CancellationTokenSource _debounceCts;
    private FormSaveStatus? _saveStatus;

    private List<AcceptanceCriteriaInReview> _acceptanceCriteriaInReview = new();
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if(_story is not null) return;
        
        _story = await UserStoryService.GetByIdAsync(StoryId);
        _sprint = await SprintService.GetByIdAsync(_story.StoryGroupId);
        _storyReviewForm = new StoryReviewForm(_story);
        _editContext = new EditContext(_storyReviewForm);
        
        RegisterNewLiveEntityUpdateHandler<UserStory>(StoryId, (newValue, _) =>
        {
            _story.ReviewComments = newValue.ReviewComments;
            _storyReviewForm.ReviewComments = newValue.ReviewComments;
            StateHasChanged();
        });
    }

    private AcceptanceCriteriaInReview RegisterAcceptanceCriteriaInReviewComponent
    {
        get => throw new NotSupportedException();
        set
        {
            if (value is null) return;
            var existing = _acceptanceCriteriaInReview
                .FirstOrDefault(x => x.AcceptanceCriteria.Id == value.AcceptanceCriteria.Id);
            if (existing is not null) _acceptanceCriteriaInReview.Remove(existing);
            _acceptanceCriteriaInReview.Add(value);
        }
    }

    /// <summary> 
    /// Toggles displaying the changelog of the current story. 
    /// If toggled to display, will fetch all changelog entries from the database if they are not already loaded.
    /// </summary>
    /// <returns>A task</returns>
    private async Task ToggleChangelog()
    {
        if (_changelog != null)
        {
            _changelog = null;
        }
        else
        {
            _changelog = (await UserStoryChangelogRepository.GetByUserStoryAsync(_story, UserStoryChangelogIncludes.Display))
                .Where(change => change.IsReviewChange)
                .ToList();
        }
    }

    private async Task OnStoryCommentsChanged()
    {
        // Cancel previous debounce timer
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            _saveStatus = FormSaveStatus.Saving;
            await BroadcastUpdateBegun<UserStory>(_story.Id);
            await Task.Delay(1000, _debounceCts.Token);

            if (!_editContext.Validate())
            {
                _saveStatus = FormSaveStatus.Unsaved;
                return;
            }
            
            await UserStoryService.SetReviewCommentsForIdAsync(
                _story.Id, 
                Self.Id, 
                _storyReviewForm.ReviewComments, 
                _reviewSessionEditingGuid
            );
            _saveStatus = FormSaveStatus.Saved;
        }
        catch (TaskCanceledException)
        {
            // Ignore if the delay was cancelled
        }
    }

    public bool Validate()
    {
        var storyIsValid = _editContext.Validate();
        var failingAcs = _acceptanceCriteriaInReview.Where(ac => !ac.Validate()).ToList();
        
        return storyIsValid && failingAcs.Count == 0;
    }

}