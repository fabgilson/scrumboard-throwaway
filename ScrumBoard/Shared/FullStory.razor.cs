using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Shared;

public partial class FullStory : BaseProjectScopedComponent
{
    [Parameter]
    public UserStory Story { get; set; }

    [Parameter]
    public bool IsEditing { get; set; }

    [Parameter]
    public bool IsEditingTask { get; set; }
    
    ///<summary> Callback for when the story is saved, the boolean argument indicates whether the story is a new story</summary>
    [Parameter]
    public EventCallback<bool> OnSave { get; set; }

    [Parameter]
    public EventCallback<bool> IsEditingChanged { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<UserStoryTask> OnViewTaskDetails { get; set; }

    [Parameter]
    public EventCallback RefreshStory { get; set; }
    
    [Inject] 
    protected IUserStoryRepository UserStoryRepository { get; set; }
    
    [Inject] 
    protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }
    
    [Inject] 
    protected IUserStoryChangelogRepository UserStoryChangelogRepository { get; set; }
    
    [Inject] 
    protected IWorklogEntryService WorklogEntryService { get; set; }
    
    [Inject] 
    protected IUserStoryService UserStoryService { get; set; }
    
    [Inject] 
    protected IUserStoryTaskService UserStoryTaskService { get; set; }
    

    private List<IMessage> _changelog = new();

    private ICollection<WorklogEntry> _worklog = new List<WorklogEntry>();

    private string AddTaskCssClasses => "btn btn-primary btn-sm mt-1" + (IsEditingTask ? " disabled" : "");

    private WorklogEntry _editedWorklogEntry = null;

    private bool IsReadOnly => ProjectState.IsReadOnly || Story.GetSprint()?.Stage.IsWorkDone() == true;

    private ConfirmModal _confirmModal;

    /// <summary> 
    /// Invokes the OnClose EventCallback when the current story is a new story.
    /// Otherwise invokes the IsEditingChanged and RefreshStory callbacks.
    /// </summary>
    private async Task OnEditingCancelled() {
        if (Story.Id == default) {
            await OnClose.InvokeAsync();
        } else {
            await IsEditingChanged.InvokeAsync(false);
            await RefreshStory.InvokeAsync();
        }
    }

    /// <summary> 
    /// If a task is not currently being edited, creates a new task and 
    /// calls OnViewTaskDetails EventCallback to display the task edit form with the new task.
    /// </summary>
    private async Task StartAddingTask() {
        if (IsEditingTask) return;

        var newTask = new UserStoryTask() { UserStoryId = Story.Id, UserStory = Story, Tags = new List<UserStoryTaskTag>() };
        await OnViewTaskDetails.InvokeAsync(newTask);
    }

    /// <summary> 
    /// If a task is not currently being edited, calls OnViewTaskDetails with the given task.
    /// </summary>
    /// <param name="task">A UserStoryTask to view the details of</param>
    private async Task OpenTaskDetails(UserStoryTask task) {
        if (IsEditingTask) return;

        await OnViewTaskDetails.InvokeAsync(task);
    }

    protected override async Task OnParametersSetAsync() {   
        await base.OnParametersSetAsync();

        if (Story.Id == default) return; // Don't bother if story has not been saved yet
        _editedWorklogEntry = null;
        _changelog = (await UserStoryChangelogRepository.GetByUserStoryAsync(Story, UserStoryChangelogIncludes.Display))
            .Cast<IMessage>()
            .ToList();
        _changelog.Add(new CreatedMessage(Story.Created, Story.Creator, "story"));

        await RefreshWorklogs();
    }

    /// <summary> 
    /// Sets the current edited worklog entry to the given worklog entry.
    /// </summary>
    /// <param name="worklogEntry">A worklogEntry</param>
    private void StartEditingWorklog(WorklogEntry worklogEntry) {
        _editedWorklogEntry = worklogEntry;
    }

    /// <summary> 
    /// Invokes the IsEditingChanged EventCallback and the OnSave callback with the newly saved story.
    /// </summary>
    /// <param name="isNewStory">A boolean, true if the current story is new (i.e. we are not editing an existing story)</param>
    private async Task StorySaved(bool isNewStory)
    {
        await IsEditingChanged.InvokeAsync(false);
        await OnSave.InvokeAsync(isNewStory);
    }

    /// <summary> 
    /// Creates a formatted string of the completed and ongoing task counts for the current story.
    /// </summary>
    /// <returns>A string containing the number of completed and ongoing tasks.</returns>
    private string GetTaskCounts() {
        return $"{Story.GetCompletedTasksCount()} Completed : {Story.GetNotCompletedTasksCount()} Ongoing";      
    }

    /// <summary> 
    /// Creates a formatted string of number of deferred tasks for the current story.
    /// </summary>
    /// <returns>A string containing the number of deferred tasks.</returns>
    public string GetDeferredCount() {
        string finalString = "";
        int deferredTaskCount = Story.GetDeferredTasksCount();
        if (deferredTaskCount > 0) {
            finalString = $"({deferredTaskCount} Deferred)";
        }
        return finalString;
    }

    /// <summary> 
    /// Refreshes the worklog for the current story.
    /// </summary>
    public async Task RefreshWorklogs()
    {
        _worklog = await WorklogEntryService.GetWorklogEntriesForStoryAsync(Story.Id);
    }
    
    /// <summary>
    /// Presents a modal asking if the user is sure they want to defer the story.
    /// If they press confirm, the story and all tasks are deferred.
    /// </summary>
    private async Task ShowDeferralConfirmation()
    {
        var confirmed = await _confirmModal.Show();
        if (confirmed)
        {
            await DeferStory();
        }
    }

    /// <summary>
    /// Marks the story and all tasks as deferred if not already in Done.
    /// </summary>
    private async Task DeferStory()
    {
        await UserStoryService.DeferStory(Self, Story); 
        await UserStoryTaskService.DeferAllIncompleteTasksInStory(Self, Story);
        await RefreshStory.InvokeAsync();
    }
}