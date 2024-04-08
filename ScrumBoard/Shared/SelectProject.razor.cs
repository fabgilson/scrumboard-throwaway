using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Extensions;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared.Modals;
using System.Linq;
using System;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;

namespace ScrumBoard.Shared
{
    public partial class SelectProject
    {
        [Inject]
        protected IScrumBoardStateStorageService StateStorageService { get; set; } 

        [Inject]
        protected ILogger<SelectProject> Logger { get; set; }

        // Currently logged in user
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }

        [CascadingParameter(Name = "ProjectState")]
        public ProjectState ProjectState { get; set; }
        
        [Parameter]
        public EventCallback ToggleNav { get; set; }

        [Parameter]
        public EventCallback ToggleNavCreateProject { get; set; }

        [Parameter]
        public string SelectedProjectName { get; set; }
       
        [Parameter]
        public EventCallback<string> SelectedProjectNameChanged { get; set; }
        
        private bool _expandArchiveNav = false;
        
        private List<Project> MyProjects { get; set; } = new();

        private List<Project> ActiveProjects { get; set; } = new();

        private List<Project> ArchivedProjects { get; set; } = new();

        private IEnumerable<Project> ReviewingProjects => MyProjects.Where(project => project.GetRole(Self) == ProjectRole.Reviewer).OrderBy(project => project.Name);
        
        private SelectProjectModal _modal;

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IProjectRepository ProjectRepository { get; set; }    

        protected override async Task OnInitializedAsync()
        {
            MyProjects = await ProjectRepository.GetByUserAsync(Self);
            var nonReviewingProjects = MyProjects.Where(project => project.GetRole(Self) != ProjectRole.Reviewer).OrderBy(project => project.Name);
            
            foreach (var project in nonReviewingProjects)
            {
                if (DateOnly.FromDateTime(DateTime.Today) < project.EndDate)
                {
                    ActiveProjects.Add(project);
                }
                else
                {
                    ArchivedProjects.Add(project);
                }
            }
        }

        /// <summary> 
        /// Starts changing the current project. Will display relevant form save modal if there is an unsaved form.
        /// If no modal is displayed or the modal confirms the project change, the current project will be updated.
        /// All pages that rely on the ProjectStateContainer will then be notified of a project change.
        /// </summary>
        /// <param name="project">The project to start changing to.</param>
        /// <returns>A task</returns>
        private async Task StartChangingProject(Project project) {
            if (project.Id == ProjectState?.ProjectId) return;
            
            var role = await ProjectRepository.GetRole(project.Id, Self.Id);
            if (!role.HasValue) {
                Logger.LogWarning("User tried to select project they were not a member of UserId={SelfId} ProjectId={ProjectId}", Self.Id, project.Id);
                return;
            }

            await ToggleNav.InvokeAsync();
            
            Logger.LogDebug("Current project set to {SelectedProjectId}", project.Id);
            await StateStorageService.SetSelectedProjectIdAsync(project.Id);
            SelectedProjectName = MyProjects.Where(p => p.Id == project.Id).Select(p => p.Name).FirstOrDefault();
            await SelectedProjectNameChanged.InvokeAsync(SelectedProjectName);
            
            // Only change the part of the URL establishing current project (will keep subpage intact)
            var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);            
            if (!relativePath.StartsWith("project/")) {
                NavigationManager.NavigateTo(PageRoutes.ToProjectHome(project.Id), true);
            }
            else
            {
                var relativePathPieces = relativePath.Split('/');
                relativePathPieces[1] = project.Id.ToString();
                NavigationManager.NavigateTo($"./{string.Join('/', relativePathPieces)}", true);
            }         
        }

        /// <summary> 
        /// Navigates to the create project page and toggles the navbar.
        /// </summary>
        /// <returns>A task</returns>
        private async Task ViewCreateProject() 
        {
            await ToggleNavCreateProject.InvokeAsync();
            NavigationManager.NavigateTo(PageRoutes.ToCreateProject());
        }
    }
}
