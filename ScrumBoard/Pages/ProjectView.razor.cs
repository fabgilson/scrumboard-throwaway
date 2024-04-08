using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Pages
{
    public partial class ProjectView : BaseProjectScopedComponent
    {
        [Inject]
        protected IProjectChangelogRepository ProjectChangelogRepository { get; set; }
        [Inject]
        protected ISprintService SprintService { get; set; }
        [Inject]
        protected ISprintRepository SprintRepository { get; set; }  
        [Inject]
        protected IUserStoryRepository UserStoryRepository { get; set; }
        [Inject]
        protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; } 
        [Inject]
        protected IWorklogEntryService WorklogEntryService { get; set; }
        [Inject]
        protected IProjectMembershipService ProjectMembershipService { get; set; }

        private Sprint _sprint;
        private UserStory _story;
        private UserStoryTask _task;

        private StartSprintReviewModal _startSprintReviewModal;
        private CancelSprintReviewModal _cancelSprintReviewModal;
        private ManageReviewersModal _manageReviewersModal;

        private bool _isEditingStory;
        private bool _isEditingTask;
        private bool _projectEditMode;
        private bool _sprintSaveError;
        private bool ShowSidebar => _task == null || _story == null;

        private List<WorklogEntry> _recentWorklogs = new();
        
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await UpdateProject();
        }

        private async Task UpdateProject()
        {
            _sprint = Project.GetCurrentSprint();
            if (_sprint != null) {
                _sprint = await SprintRepository.GetByIdAsync(_sprint.Id, SprintIncludes.Story);
            }
            
            _isEditingStory = false;
            _isEditingTask = false;
            _task = null;
            _story = null;

            await RefreshMostRecentWorklogs();
            NotifyStateChanged();
        }
        
        private void ToggleEditView()
        {
            _projectEditMode = !_projectEditMode;
        }

        private void OnViewTaskDetails(UserStoryTask task) {
            _task = task;
            _isEditingTask = false;
        }

        private void CloseStoryView() {
            _story = null;
            _isEditingStory = false;
        }

        private async Task SelectStory(UserStory story) {
            if (_isEditingStory) return;
            _story = await UserStoryRepository.GetByIdAsync(story.Id, UserStoryIncludes.Display);
        }

        private async Task RefreshMostRecentWorklogs()
        {
            _recentWorklogs = await WorklogEntryService.GetMostRecentWorklogForProjectAsync(Project.Id, 6, _sprint?.Id);
        }

        private async Task TaskUpdated()
        {
            _task = await UserStoryTaskRepository.GetByIdAsync(_task.Id, UserStoryTaskIncludes.StoryGroup, UserStoryTaskIncludes.Creator);
            if (_story is not null)
            {
                _story = await UserStoryRepository.GetByIdAsync(_story.Id, 
                    UserStoryIncludes.Display
                );
            }

            await RefreshMostRecentWorklogs();
            NotifyStateChanged();
        }
        
        private async Task ManageSprintReviewers()
        {
            await _manageReviewersModal.Show(Project);
        }

        /// <summary> 
        /// Cancels reviewing of the current sprint._
        /// If the sprint reviewers have not been selected, the sprint will cancel immediately.
        /// However if the sprint is in the 'Ready to review' stage, a confirmation modal will be displayed.
        /// </summary>
        /// <param name="sprint">The sprint to cancel the review of</param>
        /// <returns>A task</returns>
        private async Task CancelReview(Sprint sprint)
        {
            var cancelled = await _cancelSprintReviewModal.Show(sprint);
            if (cancelled) return;
            bool success = await UpdateSprintStage(sprint, SprintStage.ReadyToReview);
            if (!success) return;

            await ProjectMembershipService.RemoveAllReviewersFromProject(Self, Project);
            
            await UpdateProject();
        }

        /// <summary> 
        /// Updates the stage of the current sprint to the given stage.
        /// </summary>
        /// <param name="sprint">The sprint to update the stage of</param>
        /// <param name="newStage">The new stage to update to</param>
        /// <returns>False if the update failed because of a concurrency exception, true otherwise.</returns>
        private async Task<bool> UpdateSprintStage(Sprint sprint, SprintStage newStage)
        {
            var success = await SprintService.UpdateStage(Self, sprint, newStage);
            _sprintSaveError = !success;
            if (!success) await UpdateProject();
            return success;
        }

        /// <summary> 
        /// Updates the currently showing story from the database.
        /// </summary>
        private async Task RefreshStory() 
        {
            _sprint = await SprintRepository.GetByIdAsync(_sprint.Id, SprintIncludes.Story);
            _story = await UserStoryRepository.GetByIdAsync(_story.Id, 
                UserStoryIncludes.Display
            );        
        }

        /// <summary> 
        /// Updates the current sprint's stories.
        /// </summary>
        private async Task RefreshSprintStories()
        {
            _sprint.Stories = await UserStoryRepository.GetByStoryGroupAsync(_sprint, UserStoryIncludes.Display);  
        }

        /// <summary>
        /// Call StateHasChanged from virtual method so it can be mocked in tests
        /// </summary>
        protected virtual void NotifyStateChanged() 
        {
            StateHasChanged();
        }
    }
}