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
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;

namespace ScrumBoard.Shared
{
    public partial class ProjectEditView : BaseProjectScopedForm
    {
        [CascadingParameter]
        public Project Project { get; set; }

        private EditContext _editContext;
        
        private bool _lastCredentialsValid = false;

        private bool CredentialsValid => _lastCredentialsValid && !Model.GitlabCredentialsForm.NeedsChecking;
        
        public ProjectEditForm Model { get; set; }

        [Inject]
        protected IUserRepository UserRepository { get; set; }

        [Inject]
        protected IProjectRepository ProjectRepository { get; set; }

        [Inject]
        protected IProjectChangelogRepository ProjectChangelogRepository { get; set; }        

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected ILogger<ProjectEditView> Logger { get; set; }

        [Inject]
        protected IGitlabService GitlabService { get; set; }

        [Inject]
        protected IConfigurationService ConfigurationService { get; set; }

        [Inject]
        protected IJsInteropService JsInteropService { get; set; }

        private GitlabCredentialsForm _gitlabCredentialsForm;

        private bool _isCheckingCredentials = false;

        // Currently logged in user
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }

        private List<User> _allUsers = new();
        
        // A list of currently selected users, updated by UpdateSelectedUsers
        private List<User> _currentUsers = new();

        private DateOnly _now = DateOnly.FromDateTime(DateTime.Now);

        [Parameter]
        public EventCallback CancelEditEvent { get; set; }

        private bool _hasLeader = true;

        private bool _saveError = false;

        private bool _gitlabSaveError = false;

        private string _webhooksUrl;
        
        private bool _isCurrentlySubmitting = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            CreateModel();
            await UpdateWebhookUrl();
        }

        /// <summary> 
        /// Updates the webhook URL to display when editing GitLab credentials.
        /// The URL is of the current deployement with the push webhook path appended.
        /// </summary>
        /// <returns>A task</returns>
        private async Task UpdateWebhookUrl()
        {
            var basePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var documentUri = await JsInteropService.GetDocumentUrl();
            if (!documentUri.EndsWith(basePath)) return;
            
            documentUri = documentUri[..^basePath.Length];
            _webhooksUrl = documentUri + "webhooks/gitlab/push";
        }

        /// <summary> 
        /// Creates a new model for the current edit context including a GitlabCredentialsForm.
        /// Also sets the resets currentUsers.
        /// </summary>
        protected void CreateModel()
        {
            Model = new ProjectEditForm(Project, ConfigurationService.WebhooksEnabled);
            _gitlabCredentialsForm = Model.GitlabCredentialsForm ?? new GitlabCredentialsForm(ConfigurationService.WebhooksEnabled);

            _currentUsers = Project.GetMembers().ToList();    
            _editContext = new(Model);
        }

        /// <summary> 
        /// Invokes the CancelEditEvent EventCallback.
        /// </summary>
        protected void CancelEdit() 
        {
            CancelEditEvent.InvokeAsync();
        }

        protected override bool Validate() {
            return _editContext.Validate() && _hasLeader && Model.GitlabCredentialsForm?.NeedsChecking != true;
        }

        protected override async Task SubmitForm()
        {
            if (_isCurrentlySubmitting) {return;}
            _isCurrentlySubmitting = true;
            await Submit();
            _isCurrentlySubmitting = false;
        }

        private async Task Submit()
        {   
            _saveError = false;
            List<ProjectChangelogEntry> changes = Model.ApplyChanges(Self, Project);
            
            Logger.LogInformation("Updating project (name={ProjectName})", Project.Name);
            // Create a new entity to avoid clobbering the Project parameter (as related entities need to be severed)
            Project updatedProject = new() {
                Id = Project.Id,
                CreatorId = Project.Creator?.Id ?? Project.CreatorId,
                Name = Project.Name,
                Description = Project.Description,
                StartDate = Project.StartDate,
                EndDate = Project.EndDate,
                GitlabCredentials = Project.GitlabCredentials,
                Sprints = null,
                Backlog = null,
                MemberAssociations = null,
                RowVersion = Project.RowVersion,
                IsSeedDataProject = Project.IsSeedDataProject,
                Created = Project.Created,
            };            
            var updatedMemberships = Project.MemberAssociations.ToList();            
            try {
                await ProjectRepository.UpdateProjectAndMemberships(updatedProject, updatedMemberships);  
                // Persist each type of changelog entry once the project is updated
                await ProjectChangelogRepository.AddAllAsync(changes);
                NavigationManager.NavigateTo(PageRoutes.ToProjectHome(Project.Id), true);       
            } catch (Exception ex)
            {
                switch (ex)
                {
                    case DbUpdateConcurrencyException:
                        Logger.LogInformation("Update failed for project (name={ProjectName}). Concurrency exception occurred: {ExMessage}", Project.Name, ex.Message);               
                        _saveError = true;
                        return;
                    case DbUpdateException:
                        Logger.LogInformation("Update failed for project (name={ProjectName}). Update exception occurred: {ExMessage}", Project.Name, ex.Message);               
                        _gitlabSaveError = true;
                        return;
                    default:
                        throw;
                }
            }            
        }

        /// <summary> 
        /// Tries to save the current project details in the database.
        /// Will not save if the project does not have a leader in its member list.
        /// Also validates the current GitLab credentials (if any) before saving.
        /// </summary>
        /// <returns>A task</returns>
        protected async Task OnSubmit() 
        {
            if (!_hasLeader) return;
            if (Model.GitlabCredentialsForm?.NeedsChecking == true) {
                await CheckGitlab();
                if (Model.GitlabCredentialsForm.AuthFailure != null) return;
            }
            await SubmitForm();            
        }

        /// <summary> 
        /// Sets the member associations of the model to the given list and updates current users to the given list of users.
        /// Also checks if there is a leader in the Model's member associations.
        /// </summary>
        /// <param name="users">A list of users to set currentUsers to.</param>
        /// <param name="users">A list of ProjectUserMembership set the model's associations.</param>
        public void UpdateSelectedUsers(List<User> users, ICollection<ProjectUserMembership> associations) 
        {
            Model.MemberAssociations.Clear();
            Model.MemberAssociations.AddRange(associations);
            _currentUsers = users;
            CheckForLeader();
        }

        /// <summary> 
        /// Searches the database for any users that match the given keyword string.
        /// </summary>
        /// <param name="keywords">A keyword string to match against user details.</param>        
        private async Task SearchUsers(string keywords) 
        {
            _allUsers = await UserRepository.GetUsersByKeyword(keywords, UserIncludes.Project);
        }

        /// <summary> 
        /// Updates the roles of the current project members by settings the model's association list to the given list.
        /// Also checks the associations contains an association for a leader.
        /// </summary>
        /// <param name="associations">An updated ICollection of ProjectUserMemberships to set the model's association list to.</param>   
        protected void UpdateRoles(ICollection<ProjectUserMembership> associations) {            
            Model.MemberAssociations.Clear();
            Model.MemberAssociations.AddRange(associations);
            CheckForLeader();
        }

        /// <summary> 
        /// Checks there is a leader in the model's member associations. If there is, sets _hasLeader to true.
        /// Otherwise sets it to false.
        /// </summary>
        public void CheckForLeader() {
            _hasLeader = false;
            foreach (ProjectUserMembership association in Model.MemberAssociations) {
                if (association.Role == ProjectRole.Leader) {
                    _hasLeader = true;
                }
            }
        }

        /// <summary> 
        /// Returns a string containing the minimum date a project start date can be set to.
        /// The date is either the current project start date, or the current date, whichever is earlier.
        /// </summary>
        /// <returns>A string containing a date.</returns>
        private string FindMinDate() 
        {
            return Project.StartDate < DateOnly.FromDateTime(DateTime.Now) ? Project.StartDate.ToString() : DateOnly.FromDateTime(DateTime.Now).ToString();
        }

        /// <summary> 
        /// Checks whether saving of the project should be disabled. 
        /// Is only disabled if there is no project leader.
        /// </summary>
        /// <returns>A boolean, true if there is no leader in the project's member list.</returns>
        private bool SaveDisabled() {
            return !_hasLeader;
        }

        /// <summary> 
        /// Validates the project's GitLab credentials and checks the credentials for validity by performing a request.
        /// </summary>
        /// <returns>A task</returns>
        public async Task CheckGitlab() {
            var credentialsModel = Model.GitlabCredentialsForm;
            if (credentialsModel == null) return;

            _editContext.Validate();

            if (
                _editContext.GetValidationMessages(() => Model.GitlabCredentialsForm.URL).Any() || 
                _editContext.GetValidationMessages(() => Model.GitlabCredentialsForm.ProjectId).Any() ||
                _editContext.GetValidationMessages(() => Model.GitlabCredentialsForm.AccessToken).Any()
            ) return;
            _lastCredentialsValid = false;

            RequestFailure? requestFailure = null;
            try
            {
                _isCheckingCredentials = true;
                await GitlabService.TestCredentials(credentialsModel.GetCredentials());
            } catch (GitlabRequestFailedException ex)
            {
                requestFailure = ex.FailureType;
            } finally {
                _isCheckingCredentials = false;
            }
            NotifyStateChange();

            _lastCredentialsValid = requestFailure == null;
            credentialsModel.SetAuthFailure(requestFailure);
            _editContext.Validate();
        }

        /// <summary> 
        /// Updates the model's GitlabCredentials form when the gitlab credentials checkbox is clicked.
        /// </summary>
        /// <param name="args">The ChangeEventArgs for the event that triggered this function.</param>
        private void UpdateGitlabEnabled(ChangeEventArgs args)
        {
            var value = (bool)args.Value;
            _lastCredentialsValid = false;
            if (value) {
                Model.GitlabCredentialsForm = _gitlabCredentialsForm;
            } else {
                Model.GitlabCredentialsForm = null;
            }
            _editContext.NotifyValidationStateChanged();
        }

        // Wrapper for StateHasChanged so it can be overidden by integration test
        protected virtual void NotifyStateChange()
        {
            StateHasChanged();
        }
    }
}