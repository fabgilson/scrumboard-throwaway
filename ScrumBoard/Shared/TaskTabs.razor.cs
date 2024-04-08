using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Messages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;

namespace ScrumBoard.Shared;

public partial class TaskTabs : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "AddingWorklog")]
    public bool AddingWorklog { get; set; }

    [Parameter]
    public EventCallback OnUpdate { get; set; }

    [Parameter]
    public UserStoryTask Task { get; set; }

    [Parameter]
    public WorklogEntry WorklogEntryFocusedOnInit { get; set; }
    private long? _worklogEntryFocusedOnInitId;
    
    [Inject]
    protected IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }
    
    [Inject]
    protected ISprintRepository SprintRepository { get; set; }
    
    [Inject]
    protected IWorklogEntryService WorklogEntryService { get; set; }

    private List<UserStoryTaskChangelogEntry> _changelog = new();

    private ICollection<WorklogEntry> _worklog = new List<WorklogEntry>();

    private string _worklogErrorMessage;
    private Stage? _worklogErrorStage;

    private WorklogEntry _createdWorklogEntry = null;
    private WorklogEntry _editedWorklogEntry = null;

    private CreatedMessage CreatedMessage => new(Task.Created, Task.Creator, "task");

    private bool _addWorklogEntryDisabled = true;

    private bool IsReadOnly => ProjectState.IsReadOnly;

    protected override async Task OnInitializedAsync()
    {
        _addWorklogEntryDisabled = await AddWorklogEntryDisabled();
        if (!IsReadOnly && AddingWorklog && !_addWorklogEntryDisabled) AddWorklogEntry();
        await base.OnInitializedAsync();   
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _worklogEntryFocusedOnInitId = WorklogEntryFocusedOnInit?.Id;
        _changelog = await UserStoryTaskChangelogRepository.GetByUserStoryTaskAsync(Task, UserStoryTaskChangelogIncludes.Display);
        await RefreshWorklogs();
    }

    /// <summary> 
    /// Creates a new empty worklog entry for the current task.
    /// </summary>
    private void AddWorklogEntry()
    {
        _createdWorklogEntry = new WorklogEntry {Task = Task };
    }

    /// <summary> 
    /// Sets the current editing worklog entry to the given worklog entry.
    /// </summary>
    /// <param name="worklogEntry">A WorklogEntry to start editing</param>
    private void StartEditingWorklog(WorklogEntry worklogEntry)
    {
        _editedWorklogEntry = worklogEntry;
    }

    /// <summary> 
    /// Checks whether adding a new worklog entry should be disabled.
    /// It is disabled when the story is in the backlog, or 
    /// the sprint is not in progress, or
    /// the task is in To Do or Deferred.
    /// </summary>
    /// <returns>A boolean, true if adding an entry should be disabled.</returns>
    private async Task<bool> AddWorklogEntryDisabled()
    {
        _worklogErrorMessage = null;
        _worklogErrorStage = null;
        if (_editedWorklogEntry != null) return true;

        var currentSprint = await SprintRepository.GetByIdAsync(Task.UserStory.StoryGroupId);
        if (currentSprint == null) {
            // Story in backlog - disallow worklogs
            _worklogErrorMessage = "Story in backlog";
            return true;
        }

        if (currentSprint.Stage == SprintStage.Created) {
            // Sprint not started - disallow worklogs
            _worklogErrorMessage = "Sprint not started";
            return true;
        }
        if (currentSprint.Stage != SprintStage.Started) {
            // Sprint not in progress - disallow worklogs
            _worklogErrorMessage = "Sprint not in progress";
            return true;
        }
        if (Task.Stage is Stage.Todo or Stage.Deferred) {
            // Task not 'In Progress'|'In Review'|'Done' - disallow worklogs
            _worklogErrorMessage = "Task is in";
            _worklogErrorStage = Task.Stage;
            return true;
        }

        return false;
    }

    /// <summary> 
    /// Updates all worklogs from the database
    /// </summary>
    /// <returns>A task</returns>
    public async Task RefreshWorklogs() {      
        _worklog = await WorklogEntryService.GetWorklogEntriesForTaskAsync(Task.Id);
    }

    /// <summary>
    /// When generating rows for worklog entries, this method determines what should be added to the element's
    /// list of classes. If the worklog has been selected from another view, and as such is marked to be focused
    /// when it renders, this method returns the "scroll-to-in-modal" class which our JS observer is looking for.
    /// </summary>
    /// <param name="worklog">Worklog entry which is having its row generated</param>
    /// <returns>Classname to trigger automatic scrolling if appropriate, empty string otherwise</returns>
    private string ClassForWorklogRow(WorklogEntry worklog) {
        return worklog.Id == _worklogEntryFocusedOnInitId ? "scroll-to-in-modal" : "";
    }
}