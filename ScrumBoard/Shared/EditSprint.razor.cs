using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Forms;
using System;
using System.Linq;
using ScrumBoard.Models.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services;
using ScrumBoard.Extensions;
using System.Collections.Generic;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Repositories;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Shared
{
    public partial class EditSprint : BaseProjectScopedForm
    {   
        // Currently logged in user
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }

        [Inject]
        protected ILogger<EditSprint> Logger { get; set; }

        [Inject]
        protected IProjectRepository ProjectRepository { get; set; }

        [Inject]
        protected ISprintRepository SprintRepository { get; set; }

        [Inject]
        protected IUserStoryRepository UserStoryRepository { get; set; }

        [Inject]
        protected ISprintChangelogRepository SprintChangelogRepository { get; set;} 

        [Inject]
        protected IUserStoryTaskChangelogRepository UserStoryTaskChangelogRepository { get; set; }
        
        [Inject]
        protected IUserStoryService UserStoryService { get; set; }
        
        [Inject]
        protected IUserStoryTaskService UserStoryTaskService { get; set; }
        
        [Parameter]
        public EventCallback OnCancel { get; set; }
        
        [Parameter]
        public EventCallback OnSave { get; set; }

        [Parameter]
        public EventCallback<UserStory> StoryClicked { get; set; }

        [Parameter]
        public Sprint Sprint { get; set; }

        public SprintForm Model;

        protected RemoveUserStoryModal RemoveUserStoryModal;

        private EditContext editContext;

        private DateOnly _now = DateOnly.FromDateTime(DateTime.Now);

        private bool _isNewSprint => Sprint.Id == default;

        private int _totalPoints => Model.Stories.Sum(story => story.Estimate);
        private TimeSpan _totalEstimate => Model.Stories.Sum(story => story.GetDurationEstimate());

        private Project _project;

        private bool _saveError = false;
        
        private bool _isCurrentlySubmitting = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();    
            _project = await ProjectRepository.GetByIdAsync(Sprint.Project?.Id ?? Sprint.SprintProjectId, ProjectIncludes.Sprints);
            SetContent();
        }

        protected override void OnParametersSet()
        {
            if (Sprint.Stage == SprintStage.Started) {
                Model.StoryStartForms = Model.Stories.Select(story => new UserStoryStartForm(story)).ToList();
            }
        }

        /// <summary> 
        /// Refreshes the current form editContext.
        /// </summary>
        private void SetContent() 
        {
            DateOnly? endDateLatestSprint = _project.GetLastSprintEndDate();            
            if (_isNewSprint) {
                Model = new SprintForm(endDateLatestSprint);                                           
            } else {
                Model = new SprintForm(endDateLatestSprint, Sprint);    
            }
            editContext = new(Model);
        }

        protected override bool Validate() => editContext.Validate();
        
        protected override async Task SubmitForm()
        {
            if (_isCurrentlySubmitting) {return;}
            _isCurrentlySubmitting = true;
            await Submit();
            _isCurrentlySubmitting = false;
        }
        
        private async Task Submit() {
            _saveError = false;

            // All story removals that will require moving stories into to do and worklogs deleted
            var storyRemovals = new List<UserStory>();
            if (!_isNewSprint && Sprint.Stage != SprintStage.Created)
            {
                storyRemovals =
                    Sprint.Stories.ExceptBy(Model.Stories.Select(story => story.Id), story => story.Id)
                        .ToList();
                if (storyRemovals.Any())
                {
                    var canRemoveStories = ProjectState.ProjectRole == ProjectRole.Leader;
                    var cancelled = await RemoveUserStoryModal.Show(Sprint, storyRemovals, canRemoveStories);
                    if (cancelled) return;
                }
            }

            List<SprintChangelogEntry> changes = Model.ApplyChanges(Self, Sprint);
            

            if (_isNewSprint) {
                Sprint.Created = DateTime.Now;
                Sprint.CreatorId = Self.Id;
            }

            var savedSprint = Sprint.CloneForPersisting();
            List<UserStory> sprintStories = new();   
            foreach (var story in Sprint.Stories) {  
                var updatedStory = story.CloneForPersisting();
                updatedStory.StoryGroupId = savedSprint.Id;
                sprintStories.Add(updatedStory);             
            }       
            if (_isNewSprint) {
                Logger.LogInformation($"Creating new sprint for project (Id={Sprint.SprintProjectId})");
                await SprintRepository.AddAsync(savedSprint);
                foreach (UserStory story in sprintStories) {
                    story.StoryGroupId = savedSprint.Id;                  
                }                
                await UserStoryRepository.UpdateAllAsync(sprintStories);
                changes = null;
            } else {
                try {
                    savedSprint.Stories = sprintStories;               
                    Logger.LogInformation($"Updating sprint (Id={Sprint.Id})");  
                    await SprintRepository.UpdateAsync(savedSprint);
                    await SprintChangelogRepository.AddAllAsync(changes);
                    
                    // Move all removed stories and tasks into Stage To do
                    Stage StageMapping(Stage stage) => Stage.Todo;
                    await UserStoryService.UpdateStages(Self, storyRemovals, StageMapping);
                    await UserStoryTaskService.UpdateStages(Self, storyRemovals.SelectMany(story => story.Tasks), StageMapping);
                } catch (DbUpdateConcurrencyException ex) {
                    Logger.LogInformation($"Update failed for sprint (name={savedSprint.Name}). Concurrency exception occurred: {ex.Message}");               
                    _saveError = true;
                    return;
                }
            }

            Sprint.Id = savedSprint.Id;            
            
            await OnSave.InvokeAsync();
        }

        /// <summary> 
        /// Gets the UserStoryStartForm from the model for the given story.
        /// </summary>
        /// <param name="story">A UserStory to get the form for</param>
        /// <returns>A UserStoryTaskForm for the given story</returns>
        private UserStoryStartForm GetStoryForm(UserStory story) {
            return Model.StoryStartForms.Where(form => form.Story == story).FirstOrDefault();
        }
    }
}