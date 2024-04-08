using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Extensions;
using System.Linq;
using ScrumBoard.Services;
using System.Threading.Tasks;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Entities.Changelog;
using System;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Shared.SprintBoard;

public partial class SprintBoardTask : BaseProjectScopedComponent
{
    [Inject]
    private IUserStoryTaskRepository UserStoryTaskRepository { get; set; }

    [Inject]
    private IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }

    [Parameter]
    public UserStoryTask TaskModel { get; set; }
        
    [Parameter]
    public EventCallback MembersChanged { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object> AdditionalAttributes { get; set; }

    private bool IsReadOnly => ProjectState.IsReadOnly;

    /// <summary>
    /// Returns if sprint board is in read only mode. 
    /// Updates the assigness of a task by creating a new UserStoryTaskForm, 
    /// applying the changes and updating the associations in the database. 
    /// </summary>
    /// <param name="users">List of users to update assignees to</param>
    /// <returns>Task to be returned</returns>
    private async Task UpdateAssignees(ICollection<User> users) {
        if (IsReadOnly) return;
        var model = new UserStoryTaskForm(TaskModel);
        model.Assignees = users;
        IEnumerable<UserStoryTaskChangelogEntry> changes = model.ApplyChanges(Self, TaskModel);
        await UserStoryTaskRepository.UpdateAssociations(TaskModel);
        await UserStoryTaskChangelogRepository.AddAllAsync(changes);
        await MembersChanged.InvokeAsync();
    }

    /// <summary>
    /// Returns if sprint board is in read only mode. 
    /// Updates the reviewers of a task by creating a new UserStoryTaskForm, 
    /// applying the changes and updating the associations in the database. 
    /// </summary>
    /// <param name="users">List of users to update reviewers to</param>
    /// <returns>Task to be returned</returns>
    private async Task UpdateReviewers(ICollection<User> users) {
        if (IsReadOnly) return;
        var model = new UserStoryTaskForm(TaskModel);
        model.Reviewers = users;
        IEnumerable<UserStoryTaskChangelogEntry> changes = model.ApplyChanges(Self, TaskModel);
        await UserStoryTaskRepository.UpdateAssociations(TaskModel);
        await UserStoryTaskChangelogRepository.AddAllAsync(changes);
        await MembersChanged.InvokeAsync();
    }


    /// <summary>
    /// Gets assignees from the task model that are not already assigned to the current task. 
    /// </summary>
    /// <returns>Task with a collection of Users</returns>
    private Task<ICollection<User>> GetValidAssignees()
    {
        return Task.FromResult<ICollection<User>>(Project.GetWorkingMembers()
            .Where(user => !TaskModel.GetReviewingUsers().Select(u => u.Id).Contains(user.Id))
            .ToList());
    }

    /// <summary>
    /// Gets reviewers from the task model that are not already reviewing the current task.
    /// </summary>
    /// <returns>Task with collection of Users</returns>
    private Task<ICollection<User>> GetValidReviewers()
    {
        return Task.FromResult<ICollection<User>>(Project.GetWorkingMembers()
            .Where(user => !TaskModel.GetAssignedUsers().Select(u => u.Id).Contains(user.Id))
            .ToList());
    }
}