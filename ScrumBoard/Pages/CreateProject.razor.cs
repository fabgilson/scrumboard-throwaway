using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Services.StateStorage;

namespace ScrumBoard.Pages
{
    public partial class CreateProject : ComponentBase
    {
        [Inject]
        protected IUserRepository _userRepository { get; set; }

        [Inject]
        protected IProjectRepository _projectRepository { get; set; }

        [Inject]
        protected NavigationManager _navigationManager { get; set; }

        [Inject]
        protected ILogger<CreateProject> _logger { get; set; }

        public ProjectCreateForm _model { get; set; } = new ProjectCreateForm();

        [Inject]
        public IScrumBoardStateStorageService StateStorageService { get; set; }
        
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }

        private DateOnly _now = DateOnly.FromDateTime(DateTime.Now);

        private List<User> _allUsers = new();
        // A list of currently selected users, updated by UpdateSelectedUsers
        private List<User> _currentUsers = new();

        public void UpdateSelectedUsers(List<User> users, ICollection<ProjectUserMembership> _) 
        {
            _currentUsers = users;
        }

        protected override void OnInitialized() 
        {
            // _allUsers = _userRepository.GetAllUsers().ToList();
            _currentUsers.Add(Self);
        }

        protected async Task OnCreate() 
        {
            Project project = new() {
                Name = _model.Name,
                Description = _model.Description,
                StartDate = _model.StartDate,
                EndDate = _model.EndDate.Value,
                Created = DateTime.Now,
                CreatorId = Self.Id,
            };

            foreach (var user in _currentUsers) {
                ProjectRole role = ProjectRole.Developer;
                if (user.Id == Self.Id) {
                    role = ProjectRole.Leader;
                }
                project.MemberAssociations.Add(new ProjectUserMembership() { UserId = user.Id, Role = role});                
            }            

            _logger.LogInformation($"Creating new project (name={_model.Name})");

            await _projectRepository.AddAsync(project);
            await StateStorageService.SetSelectedProjectIdAsync(project.Id);   

            _navigationManager.NavigateTo(PageRoutes.ToProjectHome(project.Id), true);
        }

        private void CancelCreate() {
            _navigationManager.NavigateTo("", true);
        }

        private async Task SearchUsers(string keywords) 
        {
            _allUsers = await _userRepository.GetUsersByKeyword(keywords);
        }
    }
}