using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using Microsoft.AspNetCore.Components.Web;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.Extensions;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Shared.SprintBoard
{
    public partial class SprintBoardColumn : ComponentBase
    {
        [Inject]
        public IUserStoryTaskRepository UserStoryTaskRepository { get; set; }

        [Inject]
        public IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }

        [Inject]
        public ILogger<SprintBoardColumn> Logger { get; set; }

        [CascadingParameter(Name = "ProjectState")]
        public ProjectState ProjectState { get; set; }

        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }
        
        [CascadingParameter(Name = "IsSprintReadOnly")]
        public bool IsSprintReadOnly { get; set; }

        [Parameter]
        public UserStory Story { get; set; }

        [Parameter]
        public Stage ColumnStage { get; set; }

        [Parameter]
        public string ColumnName { get; set; }

        [Parameter]
        public EventCallback<UserStoryTask> TaskClicked { get; set; } 
        
        [Parameter]
        public ICollection<UserStoryTask> Tasks { get; set; }

        [Parameter]
        public EventCallback OnUpdate { get; set; }

        [Parameter]
        public EventCallback OnConcurrencyError { get; set; }

        private UpdateStoryStageModal _updateStoryStageModal;

        private List<UserStoryTask> _storyTasks = new();

        private TaskComparer _comparer = new();
        
        private bool IsReadOnly => ProjectState.IsReadOnly || IsSprintReadOnly;

        protected override void OnParametersSet()
        {
            RefreshFilteredTasks();
        }

        /// <summary>
        /// Clears the current tasks list and adds tasks that match the column stage.
        /// </summary>
        private void RefreshFilteredTasks()
        {
            _storyTasks.Clear();
            _storyTasks.AddRange(Tasks.Where(x => x.Stage == ColumnStage));
            StateHasChanged();
        }

        /// <summary>
        /// Called when an item is dragged into the column. Cannot be done if column is in read only.
        /// Returns when story stage modal (if present) is cancelled.
        /// Creates a new UserStoryTaskForm, applies changes and updates the database. 
        /// </summary>
        /// <param name="task">Given task to update stage of</param>
        /// <returns>Task to be completed</returns>
        public async Task ItemAdded(UserStoryTask task)
        {
            if (IsReadOnly) return;
            var isCancelled = await _updateStoryStageModal.Show(task, ColumnStage);
            if (isCancelled) {
                return;
            }            
            var model = new UserStoryTaskForm(task);
            model.Stage = ColumnStage;
            var changes = model.ApplyChanges(Self, task);
            try {
                await UserStoryTaskRepository.UpdateAsync(task.CloneForPersisting());
                await UserStoryTaskChangelogRepository.AddAllAsync(changes);
                StateHasChanged();
                await OnUpdate.InvokeAsync();
            } catch (DbUpdateConcurrencyException) {                
                Logger.LogInformation($"Update failed for task (name={task.Name}). Concurrency exception occurred."); 
                await OnConcurrencyError.InvokeAsync();       
            }         
        }
    }
    
    /// <summary>
    /// Task equality comparer for making sure that a forced-redraw is not done as often.
    /// </summary>
    public class TaskComparer : EqualityComparer<UserStoryTask>
    {
        public override bool Equals(UserStoryTask x, UserStoryTask y)
        {
            if (x == null || y == null) return x == y;

            return
                x.Id == y.Id &&
                x.Name == y.Name &&
                x.Description == y.Description &&
                x.Estimate == y.Estimate &&
                x.Priority == y.Priority &&
                x.Stage == y.Stage &&
                x.UserAssociations.SequenceEqual(y.UserAssociations) &&
                x.Tags.SequenceEqual(y.Tags);
        }

        public override int GetHashCode(UserStoryTask obj)
            => throw new InvalidOperationException();
    }
}