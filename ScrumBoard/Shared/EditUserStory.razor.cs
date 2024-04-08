using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Shared
{
    public partial class EditUserStory : BaseProjectScopedForm
    {
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }
        
        [Parameter]
        public UserStory Story { get; set; }

        private EditContext editContext;

        [Parameter]
        public EventCallback OnCancel { get; set; }

        private bool _cannotEdit => Story.Stage.Equals(Stage.UnderReview) || Story.Stage.Equals(Stage.Deferred) || Story.Stage.Equals(Stage.Done);

        ///<summary> Callback for when the story is saved, the boolean argument indicates whether the story is a new story</summary>
        [Parameter]
        public EventCallback<bool> OnSave { get; set; }

        [Inject]
        public IUserStoryRepository UserStoryRepository { get; set; }

        [Inject]
        public IProjectRepository ProjectRepository { get; set; }

        [Inject]
        public ILogger<EditUserStory> Logger { get; set; }

        [Inject]
        protected IUserStoryChangelogRepository UserStoryChangelogRepository { get; set; }

        public UserStoryForm Model { get; set; } = new UserStoryForm();

        private int MaxAcceptanceCriteriaIdWidth => Model.AcceptanceCriterias.Count.ToString().Length;

        private bool _newACMade;

        private bool _isNewStory => Story.Id == default;

        private bool _saveError = false;
        
        private bool _isCurrentlySubmitting = false;
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _newACMade = true;
            if (!_isNewStory) {
                Model = new(Story);
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
            var changes = Model.ApplyChanges(Self, Story);

            bool isNewStory = _isNewStory; // Make sure to cache the computed property, since it may change when saved
            if (isNewStory) {
                Story.Created = DateTime.Now;
                Story.Creator = Self;
            }

            // Create a new entity to avoid clobbering the UserStory parameter (as related entities need to be severed)
            var savedStory = Story.CloneForPersisting();

            if (isNewStory) {
                Logger.LogInformation("Creating new user story for project (Id={StoryProjectId})", Story.Project?.Id ?? Story.ProjectId);
                await UserStoryRepository.AddAsync(savedStory);
            } else {
                try {
                    Logger.LogInformation("Updating user story (Id={SavedStoryId})", savedStory.Id);
                    await UserStoryRepository.UpdateAsync(savedStory);
                    changes.Reverse(); // Ensure that AC changes show up in the correct order
                    await UserStoryChangelogRepository.AddAllAsync(changes);
                } catch (DbUpdateConcurrencyException ex) {
                    Logger.LogInformation("Update failed for story (name={SavedStoryName}). Concurrency exception occurred: {ExMessage}", savedStory.Name, ex.Message);               
                    _saveError = true;                   
                    return;
                }                
            }

            Story.Id = savedStory.Id; // Make sure to fetch the (maybe) new Id from the saved entity
            Story.RowVersion = savedStory.RowVersion;
            await OnSave.InvokeAsync(isNewStory);
        }

        /// <summary> 
        /// Adds a new empty acceptance criteria to the current model.
        /// </summary>
        private void AddAcceptanceCriteria() {
            _newACMade = false;
            Model.AcceptanceCriterias.Add(new());            
        }
        
        /// <summary> 
        /// Removes an acceptance criteria from the current model using its index.
        /// </summary>
        /// <param name="index">The index of the acceptance criteria to remove.</param>
        private void RemoveAcceptanceCriteria(int index) {
            Model.AcceptanceCriterias.RemoveAt(index);            
        }
    }
}