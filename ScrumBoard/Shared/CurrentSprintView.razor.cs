using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Repositories;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;

namespace ScrumBoard.Shared
{
    public partial class CurrentSprintView : ComponentBase
    {
        [Inject]
        public IUserStoryRepository UserStoryRepository { get; set; }

        [CascadingParameter(Name = "ProjectState")]
        public ProjectState ProjectState { get; set; }

        [Parameter]
        public Sprint Sprint { get; set; }

        [Parameter]
        public EventCallback<Sprint> EditSprint { get; set; }

        [Parameter]
        public EventCallback EndSprint { get; set; }

        [Parameter]
        public EventCallback StartSprint { get; set; }

        [Parameter]
        public EventCallback<UserStory> StoryClicked { get; set; }

        private bool _previousSprintClosed;
        
        [Parameter]
        public bool PreviousSprintClosed
        {
            get => _previousSprintClosed;
            set
            {
                _previousSprintClosed = value;
                if (Model != null)
                    Model.PreviousSprintClosed = value;
            }
        }

        private bool IsReadOnly => ProjectState.IsReadOnly;

        public SprintStartForm Model { get; set; }

        private List<UserStory> _stories = new();

        private int _totalPoints => Sprint.Stories.Sum(story => story.Estimate);

        private TimeSpan _totalEstimate => Sprint.Stories.Sum(story => story.GetDurationEstimate());

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetContent();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            SetContent();
        }

        /// <summary> 
        /// Sets the current stories from the sprint and refreshes the Modal.
        /// </summary>
        private void SetContent() {
            _stories = Sprint.Stories.ToList();
            Model = new SprintStartForm(Sprint, PreviousSprintClosed);
        }

        /// <summary> 
        /// Adds the given story to the current sprint and persists the changes.
        /// </summary>
        /// <param name="story">A UserStory to add to the sprint</param>
        /// <returns>A Task</returns>
        private async Task AddStoryToSprint(UserStory story) {
            story.StoryGroup = Sprint;
            await UserStoryRepository.UpdateAsync(story.CloneForPersisting());
        }

        /// <summary> 
        /// Invokes the StoryClicked EventCallback for the given story.
        /// </summary>
        /// <param name="story">A UserStory that was clicked</param>
        /// <returns>A Task</returns>
        private async Task ItemMouseUp(UserStory story) {
            await StoryClicked.InvokeAsync(story);
        }
    }
}