using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Pages;

public partial class SprintReview : BaseProjectScopedComponent
{
    [Inject]
    protected IUserStoryService UserStoryService { get; set; }

    [Inject]
    private ISprintService SprintService { get; set; }

    private SkipSprintReviewModal _skipSprintReviewModal;
    
    private string _errorMessage;

    private bool _reviewInProgress;

    private List<Sprint> _availableSprints = new();

    private Sprint _viewingSprint;
    private Sprint _sprintBeingEdited;

    private long? _startFromStoryId;

    private IList<UserStory> _stories = new List<UserStory>();
        
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        
        if(_viewingSprint is not null) return;
        await RefreshSprint();
    }

    private async Task UpdateStories()
    {
        if (_viewingSprint == null) {
            _stories = new List<UserStory>();
        } else
        {
            _stories = await UserStoryService.GetStoriesForSprintReviewAsync(_viewingSprint.Id);
        }
    }

    private async Task RefreshSprint()
    {
        await RefreshProject(true);
        
        _reviewInProgress = false;
        _errorMessage = null;
        _availableSprints = Project.Sprints.ToList();                

        // Show most recently reviewed sprint if there is no currently reviewed sprint
        _viewingSprint = _availableSprints.FirstOrDefault(sprint => sprint.Stage is SprintStage.ReadyToReview or SprintStage.InReview or SprintStage.Reviewed);
            
        if (RoleInCurrentProject is ProjectRole.Reviewer) {      
            if (_viewingSprint != null) {
                _availableSprints = _availableSprints.Where(sprint => sprint.Id == _viewingSprint.Id).ToList();
            } else {
                _availableSprints = new();
            }                           
        }
                           
        await UpdateStories();
    }
        
    private async Task StartReview()
    {
        Logger.LogInformation("Starting sprint review: Id={ViewingSprintId}, Name={ViewingSprintName}", _viewingSprint.Id, _viewingSprint.Name);

        if (!_stories.Any())
        {
            _errorMessage = "There are no stories to review";
            return;
        }
            
        _errorMessage = null;
        var success = await SprintService.UpdateStage(Self, _viewingSprint, SprintStage.InReview);
        await RefreshSprint();
        if (!success)
        {
            Logger.LogWarning("Failed to start sprint review due to concurrency error");
            _errorMessage = "Sprint was already updated elsewhere. Page has been refreshed.";
            return;
        }

        StartEditingFromStory(null);
    }
        
    private async Task SkipReview()
    {
        _sprintBeingEdited = _viewingSprint;
        var cancelled = await _skipSprintReviewModal.Show(_sprintBeingEdited);
        if (cancelled) return;
            
        Logger.LogInformation("Skipping sprint review: Id={EditingSprintId}, Name={EditingSprintName}", _sprintBeingEdited.Id, _sprintBeingEdited.Name);

        _errorMessage = null;
        var success = await SprintService.UpdateStage(Self, _sprintBeingEdited, SprintStage.Closed);
        await RefreshSprint();
        if (!success)
        {
            Logger.LogWarning("Failed to skip sprint review due to concurrency error");
            _errorMessage = "Sprint was updated before updating sprint stage. Page has been refreshed.";
        }
    }

    private void EditStoryPressed(UserStory story)
    {
        StartEditingFromStory(story);
    }

    private void StartEditingFromStory(UserStory story)
    {
        _startFromStoryId = story?.Id;
        _errorMessage = null;
        _reviewInProgress = true;
    }

    private async Task FinishReview(bool isSuccess)
    {
        await RefreshSprint();
        _reviewInProgress = false;
        _errorMessage = isSuccess ? null : "Sprint had already been updated elsewhere. Page has been refreshed.";
    }

    private async Task ViewSprint(Sprint sprint)
    {
        _viewingSprint = sprint;
        _reviewInProgress = false;

        await UpdateStories();
    }
}