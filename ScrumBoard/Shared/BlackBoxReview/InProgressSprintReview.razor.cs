using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Shared.BlackBoxReview;

public partial class InProgressSprintReview : BaseProjectScopedComponent
{
    [Parameter, EditorRequired] 
    public long SprintId { get; set; }
    private long? _lastSprintId;

    [Parameter]
    public long? StartFromStoryId { get; set; }
    
    [Parameter]
    public EventCallback<bool> OnFinished { get; set; }

    [Inject]
    protected IUserStoryService UserStoryService { get; set; }
    
    [Inject]
    protected ISprintService SprintService { get; set; }
    
    [Inject]
    protected IUserRepository UserRepository { get; set; }

    private int _currentStoryIndex;

    private string _noSuchSprintErrorText;
    private Sprint _sprint;
    
    private List<UserStory> _stories;
    private List<StoryInReview> _storyInReviewComponents = [];
    
    private SubmitSprintReviewModal _submitSprintReviewModal;
    private SprintReviewFinishedByAnotherUserModal _reviewFinishedWhileEditingModal;
    
    private StoryInReview RegisterStoryInReviewComponent
    {
        get => throw new NotSupportedException();
        set => _storyInReviewComponents.Add(value);
    }
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (_lastSprintId == SprintId) return;
        _lastSprintId = SprintId;

        RegisterNewLiveEntityUpdateHandler<Sprint>(SprintId, async (newValue, editingUserId) =>
        {
            if (newValue.Stage is SprintStage.Reviewed or SprintStage.Closed && _sprint.Stage is SprintStage.InReview)
            {
                var userWhoFinishedReview = await UserRepository.GetByIdAsync(editingUserId);
                var reOpenReview = await _reviewFinishedWhileEditingModal.Show(userWhoFinishedReview);
                if (reOpenReview) await AttemptToReOpenReview();
                else NavigationManager.NavigateTo(PageRoutes.ToProjectReview(Project.Id), true);
            }
        });

        _sprint = await SprintService.GetByIdAsync(SprintId);
        if (_sprint is null)
        {
            _noSuchSprintErrorText = "Error: could not find the specified sprint";
            return;
        }

        _stories = (await UserStoryService.GetBySprintIdAsync(SprintId)).ToList();

        if (StartFromStoryId is not null)
        {
            _currentStoryIndex = _stories.IndexOf(_stories.First(x => x.Id == StartFromStoryId));
        }
    }

    /// <summary> 
    /// Changes the current page to the first page with a validation error message.
    /// </summary>
    /// <returns>A task</returns>
    private bool ValidateAllStoriesShowingFirstError()
    {
        for (var i = 0; i < _stories.Count; i++)
        {
            if (_storyInReviewComponents[i].Validate()) continue;
            
            _currentStoryIndex = i;
            return false;
        }

        return true;
    }

    private async Task FinishReview()
    {
        if(!ValidateAllStoriesShowingFirstError()) return;
        
        var cancelled = await _submitSprintReviewModal.Show(_sprint);
        if (cancelled) return;

        var currentSprint = await SprintService.GetByIdAsync(SprintId);
        var success = await SprintService.UpdateStage(Self, currentSprint, SprintStage.Reviewed);
        _sprint = currentSprint;
        
        await OnFinished.InvokeAsync(success);
    }

    private async Task AttemptToReOpenReview()
    {
        var updatedSprint = await SprintService.GetByIdAsync(SprintId);
        await SprintService.UpdateStage(Self, updatedSprint, SprintStage.InReview);
        _sprint = updatedSprint;
    }
}