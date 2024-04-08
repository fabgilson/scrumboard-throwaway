using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Shared;

public partial class TaskEditForm : BaseProjectScopedComponent
{
    [Inject] 
    protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }

    [Inject] 
    protected IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }

    [Inject] 
    protected IJsInteropService JsInteropService { get; set; }
        
    [Inject] 
    protected IUserStoryTaskTagRepository UserStoryTaskTagRepository { get; set; }
    
    [Parameter]
    public bool AddingWorklog { get; set; }

    [Parameter]
    public bool StoryLinkDisabled { get; set; }

    private ICollection<User> ValidAssignees => Project.GetWorkingMembers().Where(user => Model.Reviewers.All(u => u.Id != user.Id)).ToList();

    private ICollection<User> ValidReviewers => Project.GetWorkingMembers().Where(user => Model.Assignees.All(u => u.Id != user.Id)).ToList();

    protected IUpdateStoryStageModal UpdateStoryStageModal;

    private UserStoryTask _task;

    [Parameter]
    public UserStoryTask Task
    {
        get => _task;
        set
        {
            var previous = _task;
            _task = value;
            if (previous != value) ResetFields();
        }
    }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter] 
    public EventCallback<long> OnUpdate { get; set; }
        
    [Parameter]
    public EventCallback<UserStory> OnStorySelect { get; set; }

    /// <summary>
    /// Callback used to notify parent when this component starts/stops editing
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnEditStatusChanged { get; set; }

    [Parameter]
    public WorklogEntry WorklogEntryFocusedOnInit { get; set; }

    public UserStoryTaskForm Model = new();

    public EditContext EditContext;

    /// <summary>
    /// Used so that there is some reference at the base of the dom when the entire component needs to be blurred on submit
    /// </summary>
    protected ElementReference ContainerDivReference;

    private bool IsNewTask => Task.Id == default;

    private string NameCssClasses
    {
        get
        {
            var cssClasses = "form-control text-black py-0 ps-2 hide-valid text-break";
            if (!IsEditingName) cssClasses += " transparent-input";
            if (!IsReadOnly) cssClasses += " text-area-expand text-area-no-newlines";
            if (IsNewTask) cssClasses += " autofocus";
            return cssClasses;
        }
    }

    private bool IsSprintStarted => Task.GetSprint()?.Stage == SprintStage.Started;
        
    private bool IsReadOnly => ProjectState.IsReadOnly  || Task.GetSprint()?.Stage.IsWorkDone() == true;

    private bool _isEditing = false;

    private bool _saveError = false;

    private bool IsEditingName => Model.Name != Task.Name;

    private bool IsEditingDescription => Model.Description != Task.Description;

    private bool IsEditingEstimate => Model.Estimate != Task.Estimate;

    private bool IsEditingTags {
        get {
            if (Model.Tags.Count != Task.Tags.Count) return true;
            var modelIds = Model.Tags.Select(tag => tag.Id).ToList();
            var taskIds = Task.Tags.Select(tag => tag.Id).ToList();
            return !modelIds.All(taskIds.Contains);
        }
    }

    private bool IsEditingPriority => Model.Priority != Task.Priority;

    private bool IsEditingComplexity => Model.Complexity != Task.Complexity;

    private bool IsEditingAssignees
    {
        get
        {
            IEnumerable<User> taskAssignees = Task.GetAssignedUsers();
            return !Model.Assignees.All(taskAssignees.Contains) ||
                   !taskAssignees.All(Model.Assignees.Contains);
        }
    }

    private bool IsEditingReviewers
    {
        get
        {
            IEnumerable<User> taskReviewers = Task.GetReviewingUsers();
            return !Model.Reviewers.All(taskReviewers.Contains) ||
                   !taskReviewers.All(Model.Reviewers.Contains);
        }
    }

    private bool IsEditingStage => Model.Stage != Task.Stage;
        
    private bool _isCurrentlySubmitting = false;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        EditContext = new EditContext(Model);            
        await UpdateIsEditing();
    }
        

    /// <summary> 
    /// Checks whether the task is currently being edited (to display the save/cancel buttons if so and notify the parent of changes)
    /// </summary>
    /// <returns>A task</returns>
    private async Task UpdateIsEditing()
    {
        var shouldBeEditing =
            IsEditingName ||
            IsEditingDescription ||
            IsEditingEstimate ||
            IsEditingTags ||
            IsEditingPriority ||
            IsEditingComplexity ||
            IsEditingAssignees ||
            IsEditingReviewers ||
            IsEditingStage ||
            IsNewTask ||
            _saveError;

        if (shouldBeEditing == _isEditing) return;
        _isEditing = shouldBeEditing;

        NotifyStateChange();
        await OnEditStatusChanged.InvokeAsync(_isEditing);
        
        NotifyStateChange();
    }

    ///<summary>Moves the user's focus away from any selected textboxes</summary>
    private async Task StopEditing()
    {
        await JsInteropService.BlurElementAndDescendents(ContainerDivReference);
    }

    /// <summary>
    /// Saves the task changed or creates a new task if task doesn't exist.       
    /// </summary>
    /// <returns>An optional long containing either the task's id. 
    ///  Returns 0 if the update was cancelled and nothing if there was a concurrency error.
    /// </returns>
    private async Task<long?> SaveChanges()
    {
        _saveError = false;
        var isCancelled = await UpdateStoryStageModal.Show(Task, Model.Stage);
        if (isCancelled) return 0;
            
        var changes = Model.ApplyChanges(Self, Task);
        if (IsNewTask) {
            Task.CreatorId = Self.Id;
            Task.Created = DateTime.Now;
            Task.OriginalEstimate = Model.Estimate;     
            Task.UserStoryId = Task.UserStory.Id;           
        }

        // Create a new entity to avoid clobbering the Task parameter (as related entities need to be severed)
        var savedTask = Task.CloneForPersisting();

        if (IsNewTask)
        {
            Logger.LogInformation("Creating new task for story (Id={SavedTaskUserStoryId})", savedTask.UserStoryId);
            savedTask.UserAssociations = Task.UserAssociations.Select(association => new UserTaskAssociation() { Role = association.Role, UserId = association.UserId, TaskId = association.TaskId}).ToList();
            savedTask.Tags = Task.Tags;
            await UserStoryTaskRepository.AddAsync(savedTask);
        }
        else
        {
            try {
                savedTask.UserAssociations = null;
                Logger.LogInformation("Updating task (Id={SavedTaskId})", savedTask.Id);
                await UserStoryTaskRepository.UpdateTaskAssociationsAndTags(savedTask, Task.UserAssociations.ToList(), Task.Tags.ToList());
                await UserStoryTaskChangelogRepository.AddAllAsync(changes);
            } catch (DbUpdateConcurrencyException ex) {
                Logger.LogInformation("Update failed for task (Id={SavedTaskId}). Concurrency exception occurred: {ExMessage}", savedTask.Id, ex.Message);               
                _saveError = true;
                return null;
            }                
        }
        return savedTask.Id;
    }


    protected async Task OnSubmitForm()
    {
        if (_isCurrentlySubmitting) {return;}
        _isCurrentlySubmitting = true;
        await Submit();
        _isCurrentlySubmitting = false;
    }


    /// <summary> 
    /// Tries to save the current form in the database, if successful will stop editing and invoke OnUpdate EventCallback.
    /// </summary>
    /// <returns>A task</returns>
    private async Task Submit()
    {
        long? id = await SaveChanges();
        if (!id.HasValue) return;
        await StopEditing();
        await OnUpdate.InvokeAsync(id.Value);
    }

    /// <summary> 
    /// Resets all form fields to their original values from the Task parameter (and the database for the assignees/reviewers)
    /// </summary>
    private async Task ResetFields()
    {
        var assigneesAndReviewers = await UserStoryTaskRepository.GetAssigneesAndReviewers(Task);
        Task.UserAssociations = assigneesAndReviewers;
        Model = new UserStoryTaskForm(Task);
        EditContext = new EditContext(Model);
        NotifyStateChange();          
    }
        
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        await UpdateIsEditing();
    }

    /// <summary> 
    /// Cancels editing the current task. If the task was a new task, 
    /// invokes the OnClose EventCallback, otherwise invokes the OnUpdate EventCallback and resets the form fields.
    /// </summary>
    private async Task OnCancelPressed()
    {
        _saveError = false;
        if (IsNewTask)
        {
            await OnClose.InvokeAsync();
        }
        else
        {
            await OnUpdate.InvokeAsync(Task.Id);
            ResetFields();
        }
    }

    // Wrapper for StateHasChanged so it can be overidden by integration test
    protected virtual void NotifyStateChange()
    {
        StateHasChanged();
    }
}