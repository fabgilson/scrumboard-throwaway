using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Shared
{
    public partial class ArchivedSprintView : BaseProjectScopedComponent
    {
        private ElementReference _root;

        [CascadingParameter]
        public Sprint _currentViewingSprint {  get; set; }

        [Parameter]
        public Sprint Sprint { get; set; }

        [Parameter]
        public EventCallback<UserStory> StoryClicked { get; set; }

        [Parameter]
        public EventCallback<Sprint> ReopenSprint { get; set; }
        
        [Parameter]
        public EventCallback<Sprint> CloseSprint { get; set; }

        [Inject]
        protected IJsInteropService JSInteropService { get; set; }

        [Inject]
        protected IUserStoryRepository UserStoryRepository { get; set; }
        
        [Inject]
        protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }

        private List<UserStory> _sprintStories;

        private int? _totalPoints;

        private TimeSpan? _totalEstimate;

        /// <summary>
        /// Invokes StoryClicked EventCallback with the given story.
        /// </summary>
        /// <param name="story">A UserStory that was clicked</param>
        /// <returns>A Task</returns>
        private async Task ItemClicked(UserStory story) {
            await StoryClicked.InvokeAsync(story);
        }

        /// <summary>
        /// Invokes ReopenSprint EventCallback with the current sprint.
        /// </summary>
        /// <returns>A Task</returns>
        private async Task ReopenClicked() {
            await ReopenSprint.InvokeAsync(Sprint);
        }

        /// <summary>
        /// Invokes CloseSprint EventCallback with the current sprint.
        /// </summary>
        /// <returns>A Task</returns>
        private async Task CloseClicked()
        {
            await CloseSprint.InvokeAsync(Sprint);
        }

        /// <summary>
        /// Calls the ScrollTo method with the root ElementReference.
        /// This will scroll the page to the top of the expanded list.
        /// </summary>
        /// <returns>A Task</returns>
        private async Task OnExpandStories() {
            await JSInteropService.ScrollTo(_root);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (_sprintStories != null)
            {
                _sprintStories = await UserStoryRepository.GetByStoryGroupAsync(Sprint, UserStoryIncludes.Tasks);
                _totalPoints = _sprintStories.Sum(story => story.Estimate);
                _totalEstimate = _sprintStories.SelectMany(story => story.Tasks).Sum(task => task.Estimate);
            }
            else
            {
                _totalPoints = await UserStoryRepository.GetEstimateByStoryGroup(Sprint);
                _totalEstimate = await UserStoryTaskRepository.GetEstimateByStoryGroup(Sprint);
            }
        }

        /// <summary>
        /// Fetches all user stories (with tasks) for the current sprint if they aren't already loaded.
        /// </summary>
        /// <returns>A Task</returns>
        private async Task OnStartExpandStories()
        {
            if (_sprintStories == null)
            {
                _sprintStories = await UserStoryRepository.GetByStoryGroupAsync(Sprint, UserStoryIncludes.Tasks);
            }
        }
    }
}