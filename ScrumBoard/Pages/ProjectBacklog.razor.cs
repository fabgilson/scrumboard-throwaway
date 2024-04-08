using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using System.Threading.Tasks;
using ScrumBoard.Services;
using ScrumBoard.Extensions;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Utils;
using ScrumBoard.Repositories;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages;

public partial class ProjectBacklog : BaseProjectScopedComponent
{
    [Inject]
    protected IProjectChangelogRepository ProjectChangelogRepository { get; set; }
        
    [Inject] 
    protected IProjectMembershipService ProjectMembershipService { get; set; }

    [Inject]
    protected IScrumBoardStateStorageService StateStorageService { get; set; }

    [Inject]
    protected ISprintChangelogRepository SprintChangelogRepository { get; set; }

    [Inject]
    protected IUserStoryChangelogRepository UserStoryChangelogRepository { get; set; }

    [Inject]
    protected IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }

    [Inject]
    protected ISprintRepository SprintRepository { get; set; }
        
    [Inject]
    protected IBacklogRepository BacklogRepository { get; set; }

    [Inject]
    protected IUserStoryRepository UserStoryRepository { get; set; }

    [Inject]
    protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }
        
    [Inject]
    protected ISprintService SprintService { get; set; }
        
    [Inject]
    protected IUserStoryService UserStoryService { get; set; }
        
    [Inject]
    protected IUserStoryTaskService UserStoryTaskService { get; set; }

    [Inject]
    protected IJsInteropService JSInteropService { get; set; }

    [Inject]
    protected ISortableService<UserStory> SortableService { get; set; }

    private bool IsReadOnly => ProjectState.IsReadOnly;

    private List<UserStory> _backlog = new();

    private Sprint _sprintEditing;

    private Sprint _currentViewingSprint = null;

    private bool _isEditingSprint = false;

    private bool _sprintSaveError = false;

    private SprintStatusChangeModal _statusChangeModal;

    private CloseSprintModal _closeSprintModal;

    private StoryTaskDisplayPanel _storyTaskPanel;

    private EditSprint _editSprintComponent;

    private bool PreviousSprintClosed => Project.Sprints
        .Where(sprint => sprint.Id != _currentViewingSprint?.Id)
        .Select(sprint => sprint.Stage as SprintStage?)
        .FirstOrDefault() is SprintStage.Closed or null;
        
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        SortableService.SetSynchroniseOnMove("backlog", true);
        await RefreshBacklog();
    }

    private async Task RefreshBacklog()
    {
        _backlog = await UserStoryRepository.GetByStoryGroupAsync(Project.Backlog, UserStoryIncludes.Tasks);
        await RefreshProject(true);
        _currentViewingSprint = Project.GetCurrentSprint();
        if (_currentViewingSprint != null)
        {
            _currentViewingSprint = await SprintRepository.GetByIdAsync(_currentViewingSprint.Id, SprintIncludes.Creator, SprintIncludes.Tasks);
            if (_currentViewingSprint == null)
            {
                Logger.LogWarning("Project contained current sprint, but failed to fetch it by id");
            }
        }
        NotifyStateChange();
    }

    private async Task OnEditingCancelled() {
        _isEditingSprint = false;
        if (_currentViewingSprint != null) {
            _currentViewingSprint = await SprintRepository.GetByIdAsync(_currentViewingSprint.Id, SprintIncludes.Creator, SprintIncludes.Tasks);
        }            
        _backlog = await UserStoryRepository.GetByStoryGroupAsync(Project.Backlog, UserStoryIncludes.Tasks);
        NotifyStateChange();
    }

    private async Task OnSprintSave() {
        _isEditingSprint = false;
        await SaveBacklog();
        await RefreshBacklog(); // Make sure we see the updated sprint
    }

    private async Task StartCreatingSprint() {
        await SaveBacklog();
        bool currentSprintExists = await SprintRepository.ProjectHasCurrentSprintAsync(Project.Id);
        if (currentSprintExists) {
            _sprintSaveError = true;
            await RefreshBacklog();
            return;
        }
        _sprintEditing = new Sprint { SprintProjectId = Project.Id };
        _isEditingSprint = true;
    }

    private async Task StartEditingSprint(Sprint sprint) {
        bool success = await SaveBacklog();            
        if (!success) return;
        bool sprintWasUpdated = await CurrentSprintWasUpdated();
        if (sprintWasUpdated) {
            await RefreshBacklog();
        } else {
            _sprintEditing = sprint;
            _isEditingSprint = true;
        }            
    }

    public async Task<bool> CurrentSprintWasUpdated() {
        _sprintSaveError = false;
        var latestSprint = await SprintRepository.GetByIdAsync(_currentViewingSprint.Id);
        if (!latestSprint.RowVersion.SequenceEqual(_currentViewingSprint.RowVersion)) {
            _sprintSaveError = true;              
            return true;
        }
        return false;
    }

    public async Task StartCurrentSprint() {
        _sprintSaveError = false;
        var cancelled = await _statusChangeModal.Show(_currentViewingSprint.Stage, SprintStatusAction.Start);
        if (cancelled) return;

        var initialStage = _currentViewingSprint.Stage;
        _currentViewingSprint.Stage = SprintStage.Started;
        _currentViewingSprint.TimeStarted ??= System.DateTime.Now;
        try {
            await SprintRepository.UpdateAsync(_currentViewingSprint.CloneForPersisting());
            await SprintChangelogRepository.AddAsync(new(Self, _currentViewingSprint, nameof(Sprint.Stage), Change<object>.Update(initialStage, _currentViewingSprint.Stage)));
            await ProjectMembershipService.RemoveAllReviewersFromProject(Self, Project);
        } catch (DbUpdateConcurrencyException ex) {
            Logger.LogWarning("Update failed for sprint (name={Name}). Concurrency exception occurred: {ExMessage}", _currentViewingSprint.Name, ex.Message);               
            _sprintSaveError = true;                
        }
        await RefreshBacklog();
    }

    private async Task EndCurrentSprint() {
        ICollection<UserStory> unStartedStories = _currentViewingSprint.Stories.Where(s => s.Stage == Stage.Todo).ToList();
        var cancelled = await _statusChangeModal.Show(_currentViewingSprint.Stage, SprintStatusAction.Finish, unStartedStories);
        if (cancelled) return;
        await SetSprintReadyToReview();
    }


    protected async Task SetSprintReadyToReview() {
        _sprintSaveError = false;
        var initialStage = _currentViewingSprint.Stage;
        _currentViewingSprint.Stage = SprintStage.ReadyToReview;
        try
        {
            Stage StageMapping(Stage stage) => stage == Stage.Todo ? Stage.Deferred : stage;
            await UserStoryService.UpdateStages(Self, _currentViewingSprint, StageMapping);
            await UserStoryTaskService.UpdateStages(Self, _currentViewingSprint, StageMapping);
                
            await SprintRepository.UpdateAsync(_currentViewingSprint.CloneForPersisting());   
            await SprintChangelogRepository.AddAsync(new(Self, _currentViewingSprint, nameof(Sprint.Stage), Change<object>.Update(initialStage, _currentViewingSprint.Stage)));
        } catch (DbUpdateConcurrencyException ex) {
            Logger.LogInformation("Update failed for sprint (name={Name}). Concurrency exception occurred: {ExMessage}", _currentViewingSprint.Name, ex.Message);               
            _sprintSaveError = true;
        }
        await RefreshBacklog();       
    }

    /// <summary>
    /// Reopens the given archived (cancelled or finished) sprint. 
    /// Changes the given sprint's UpdateTaskStagesstatus back to 'Created', and sets all deferred tasks and 
    /// stories to 'Todo'
    /// This function assumes that the option to reopen will only be available
    /// when there are no current sprints, so will override the current viewing sprint.
    /// </summary>
    /// <param name="archivedSprint">Archived sprint to reopen</param>
    private async Task ReopenSprint(Sprint archivedSprint) {
        var initialStage = archivedSprint.Stage;
        var cancelled = await _statusChangeModal.Show(initialStage, SprintStatusAction.Reopen, archivedSprint.Stories);
        if (cancelled) return;

        try {
            archivedSprint.Stage = SprintStage.Created;

            // Disallow reopening a sprint if another user already reopened one.
            bool currentSprintExists = await SprintRepository.ProjectHasCurrentSprintAsync(Project.Id);
            if (currentSprintExists) throw new DbUpdateConcurrencyException("Current sprint already exists");

            Stage StageMapping(Stage stage) => stage == Stage.Deferred ? Stage.Todo : stage;
            await UserStoryService.UpdateStages(Self, archivedSprint, StageMapping);
            await UserStoryTaskService.UpdateStages(Self, archivedSprint, StageMapping);
                
            await SprintRepository.UpdateAsync(archivedSprint.CloneForPersisting());
            await SprintChangelogRepository.AddAsync(new SprintChangelogEntry(Self, archivedSprint, nameof(Sprint.Stage), Change<object>.Update(initialStage, SprintStage.Created)));
        } catch (DbUpdateConcurrencyException ex) {
            Logger.LogInformation("Update failed for sprint (name={ArchivedSprintName}). Concurrency exception occurred: {ExMessage}", archivedSprint.Name, ex.Message);               
            _sprintSaveError = true;  
            await RefreshBacklog(); 
            return;              
        }
        await ProjectMembershipService.RemoveAllReviewersFromProject(Self, Project);
        await RefreshBacklog();
    }
        
    protected async Task CloseSprint(Sprint sprint)
    {
        var cancelled = await _closeSprintModal.Show(sprint);
        if (cancelled) return;
            
        var success = await SprintService.UpdateStage(Self, sprint, SprintStage.Closed);
        if (!success)
        {
            _sprintSaveError = true;  
        }
        await RefreshBacklog(); 
    }

    private async Task<bool> SaveBacklog() {
        Project.Backlog.ReplaceStories(_backlog); 
        var savedBacklog = new Backlog() {
            Id = Project.Backlog.Id,
            BacklogProjectId = Project.Id,
            Stories = null        
        };    
        var backlogStories = Project.Backlog.Stories.ToList();          
        try {
            await BacklogRepository.UpdateBacklogAndStories(savedBacklog, backlogStories);   
        } catch (DbUpdateConcurrencyException ex) {
            Logger.LogInformation("Update failed for backlog (Id={SavedBacklogId}). Concurrency exception occurred when saving stories: {ExMessage}", savedBacklog.Id, ex.Message);               
            _sprintSaveError = true;  
            await RefreshBacklog();   
            return false;                           
        }
        return true;                
    }

    private async Task StartCreatingStory()
    {
        var newStory = new UserStory() { 
            Project = Project, 
            ProjectId = Project.Id,
            StoryGroup = Project.Backlog,
            StoryGroupId = Project.Backlog.Id,
            // Order starts at 1
            Order = _backlog.Count + 1,
            AcceptanceCriterias = new List<AcceptanceCriteria>(),
        };
        await _storyTaskPanel.SelectStory(newStory);
    }

    /// <summary>
    /// Updates the stories and tasks for the current backlog or sprint or editing sprint when applicable.
    /// Called by the StoryTaskDisplayPanel when a story or task is updated.
    /// </summary>
    /// <param name="story">The story that was updated.</param>
    private async Task OnStoryUpdated(UserStory story)
    {         
                       
        if (_backlog.Any(u => u.Id == story.Id)) {
            _backlog = await UserStoryRepository.GetAllByIdsAsync(_backlog.Select(u => u.Id).ToList(), UserStoryIncludes.Tasks);
        } else if (_isEditingSprint && _editSprintComponent.Model.Stories.Any(u => u.Id == story.Id)) {
            _editSprintComponent.Model.Stories = await UserStoryRepository.GetAllByIdsAsync(_editSprintComponent.Model.Stories.Select(u => u.Id).ToList(), UserStoryIncludes.Tasks);
        } else if (_currentViewingSprint != null && _currentViewingSprint.Stories.Any(u => u.Id == story.Id)) {
            _currentViewingSprint.Stories = await UserStoryRepository.GetAllByIdsAsync(_currentViewingSprint.Stories.Select(u => u.Id).ToList(), UserStoryIncludes.Tasks);
        } else {
            // If the story was not in any of the above locations, it must be a newly created story.
            _backlog = await UserStoryRepository.GetByStoryGroupAsync(Project.Backlog, UserStoryIncludes.Tasks);
        }            
        NotifyStateChange();
    }

    // Wrapper for StateHasChanged so it can be overridden by integration test
    protected virtual void NotifyStateChange()
    {
        StateHasChanged();
    }
}