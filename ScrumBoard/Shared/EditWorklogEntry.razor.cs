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
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Shared;

public partial class EditWorklogEntry : BaseProjectScopedComponent
{
    [Inject]
    protected IWorklogEntryService WorklogEntryService { get; set; }
        
    [Inject]
    protected ISprintRepository SprintRepository { get; set; }

    [Parameter]
    public WorklogEntry Entry { get; set;}

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnUpdate { get; set; }

    [Parameter]
    public EventCallback RefreshWorklogs { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes {  get; set; }
    
    public WorklogEntryForm Model;

    private EditContext _editContext;        

    private bool IsNewWorklogEntry => Entry.Id == default;
    
    private ICollection<User> GetValidPairUsers() => Project.GetMembers().Where(user => user.Id != Self.Id).ToList();

    private bool _saveError = false;
        
    private IEnumerable<GitlabCommit> _commits = new List<GitlabCommit>();
    
    private bool _isCurrentlySubmitting = false;

    private Sprint _sprint = null;

    /// <summary>
    /// Work cannot be logged before the current sprint's start date. If the task does not belong in any sprint,
    /// no work should be able to be logged to it
    /// </summary>
    private DateOnly _minimumDate;

    /// <summary>
    /// A list of the current pair users for the worklog. Will only ever contain one item.
    /// </summary>
    private ICollection<User> CurrentPairUsers {
        get
        {
            if (Model.PairUser != null) {
                return new List<User> { Model.PairUser };
            }

            return new List<User>();
        }
        set => Model.PairUser = value.FirstOrDefault();
    }
        
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsNewWorklogEntry) {
            if (Entry.Task.UserStory.StoryGroup is Sprint s) _sprint = s;
        } else {
            _sprint = await SprintRepository.GetByWorklogEntry(Entry);
        }
        
        Model = IsNewWorklogEntry ? new WorklogEntryForm(_sprint?.TimeStarted) : new WorklogEntryForm(Entry, _sprint?.TimeStarted);            
        _editContext = new EditContext(Model);

        if (_sprint?.TimeStarted != null)
        {
            _minimumDate = DateOnly.FromDateTime(_sprint.TimeStarted.Value);
        }

        await base.OnInitializedAsync(); 
    }
    
    protected async Task<bool> SubmitForm()
    {
        if (_isCurrentlySubmitting) { return false; }
        _isCurrentlySubmitting = true;
        var returnVal = await Submit();
        _isCurrentlySubmitting = false;
        return returnVal;
    }

    private async Task<bool> Submit() {
        _saveError = false;
        try
        {
            if (IsNewWorklogEntry)
            {
                Logger.LogInformation("Adding new worklog entry for (Task.Id={TaskId})", Entry.Task?.Id ?? Entry.TaskId);
                await WorklogEntryService.CreateWorklogEntryAsync(
                    Model, 
                    Self.Id, 
                    Entry.Task?.Id ?? Entry.TaskId,
                    taggedWorkInstanceForms: Model.TaggedWorkInstanceForms,
                    pairId: Model.PairUser?.Id,
                    linkedCommits: Model.LinkedCommits?.ToList()
                );
            }
            else
            {
                Logger.LogInformation("Updating existing worklog entry {Description} (Id={EntryId}) for (Task.Id={TaskId})",
                    Model.Description, Entry.Id, Entry.TaskId);
                await WorklogEntryService.UpdateWorklogEntryAsync(Entry.Id, Model, Self.Id);
                await WorklogEntryService.UpdatePairUserAsync(Entry.Id, Self.Id, Model.PairUser?.Id);
                await WorklogEntryService.SetTaggedWorkInstancesAsync(Entry.Id, Self.Id, Model.TaggedWorkInstanceForms);
                await WorklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(Entry.Id, Self.Id, Model.LinkedCommits);
            }
        } catch (DbUpdateConcurrencyException ex) {                    
            Logger.LogWarning("Update failed for worklog entry: (Id={EntryId}). Concurrency exception occurred: {ExMessage}", Entry.Id, ex.Message);               
            _saveError = true;
            return false;
        }

        await RefreshWorklogs.InvokeAsync();
        return true; 
    }

    /// <summary> 
    /// Tries to save the current form contents. 
    /// If the save is successful, the OnClose and OnUpdate EventCallbacks will be invoked.
    /// </summary>
    /// <returns>A Task</returns>
    private async Task OnFormSubmit() {
        bool success = await SubmitForm();
        if (!success) return;
        await OnClose.InvokeAsync();
        await OnUpdate.InvokeAsync();                      
    }

    /// <summary> 
    /// Invokes the OnClose and RefreshWorklogs EventCallbacks.
    /// </summary>
    /// <returns>A Task</returns>
    public async Task OnWorklogClosed() {
        await OnClose.InvokeAsync();
        await RefreshWorklogs.InvokeAsync();
    }

    /// <summary> 
    /// Sets the current model's linked commits to the given list.
    /// </summary>
    /// <param name="commits">A list of GitlabCommits.</param>
    private void UpdateCommits(List<GitlabCommit> commits)
    {
        Model.LinkedCommits = commits;
    }
}