using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Blazor.Modals;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{   
    public class SelectProjectComponentTest : TestContext
    {
        private readonly Project _currentProject = new() { Id= 100, Name="Test Project", EndDate = DateOnly.FromDateTime(DateTime.Today).AddDays(2) };

        private readonly Project _otherProject = new() { Id=101, Name="Test project2", EndDate = DateOnly.FromDateTime(DateTime.Today).AddDays(3) };

        private readonly Project _archiveProject = new() { Id=102, Name= "Archived Project", EndDate = new DateOnly(2023, 1, 26),};
        
        private readonly User _actingUser;

        private IRenderedComponent<SelectProject> _component;

        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);

        private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);
        
        public SelectProjectComponentTest()
        {
            _actingUser = new User() { Id = 33, FirstName = "Jeff", LastName="Geoff"};
            var otherUser = new User() { Id = 34, FirstName = "Jimmy", LastName = "Neutron" };

            var memberships = new List<ProjectUserMembership> 
            { 
                new() { 
                    ProjectId=_currentProject.Id,
                    UserId=_actingUser.Id, 
                    Project=_currentProject, 
                    User=_actingUser,
                    Role = ProjectRole.Developer,
                },
                new() { 
                    ProjectId = _currentProject.Id,
                    UserId = otherUser.Id, 
                    Project = _currentProject, 
                    User=otherUser,
                    Role = ProjectRole.Developer,
                },
                new() { 
                    ProjectId=_otherProject.Id,
                    UserId=_actingUser.Id, 
                    Project=_otherProject, 
                    User=_actingUser,
                    Role = ProjectRole.Developer,
                },
            };

            _actingUser.ProjectAssociations = memberships.Where(membership => membership.UserId == _actingUser.Id).ToList();
            _currentProject.MemberAssociations = memberships.Where(membership => membership.ProjectId == _currentProject.Id).ToList();
        }

        private void CreateComponent(bool archivePresent = true)
        {
            _mockProjectRepository.Setup(x => x.GetByIdAsync(_currentProject.Id)).ReturnsAsync(_currentProject);
            _mockProjectRepository.Setup(x => x.GetByIdAsync(_otherProject.Id)).ReturnsAsync(_otherProject);

            var projectList = new List<Project> { _currentProject, _otherProject };

            if (archivePresent)
            {
                _mockProjectRepository.Setup(x => x.GetByIdAsync(_archiveProject.Id)).ReturnsAsync(_archiveProject);
                projectList.Add(_archiveProject);
            }
            
            _mockProjectRepository.Setup(x => x.GetByUserAsync(_actingUser))
                .ReturnsAsync(projectList);
            Services.AddScoped(_ => _mockProjectRepository.Object);

            _mockStateStorageService.Setup(x => x.GetSelectedProjectIdAsync()).ReturnsAsync(_currentProject.Id);
            Services.AddScoped(_ => _mockStateStorageService.Object);
            
            var authContext = this.AddTestAuthorization();
            authContext.SetAuthorized("TEST USER");
            authContext.SetRoles("GlobalProjectAdmin");

            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory()); 
            
            _component = RenderComponent<SelectProject>(parameters => parameters
                .Add(cut => cut.Self, _actingUser)
            );              
        }
        
        [Fact]
        public void SelectProject_HasProject_SelectButtonDoesRenders()
        {
            CreateComponent();
            _component.FindAll($"#project-item-{_otherProject.Id}").Should().ContainSingle();
        }
        
        [Fact]
        public void SelectOtherProject_ChangeProjectWithDeveloperRole_ServiceCalledAndProjectNotReadonly()
        {
            var projectId = _otherProject.Id;
            _mockProjectRepository
                .Setup(mock => mock.GetRole(projectId, _actingUser.Id))
                .ReturnsAsync(ProjectRole.Developer);
            _mockStateStorageService
                .Setup(mock => mock.SetSelectedProjectIdAsync(projectId))
                .Returns(Task.CompletedTask);

            CreateComponent();
            var projectDropdownOption = _component.Find($"#project-item-{projectId}");
            projectDropdownOption.Click();
            
            _mockStateStorageService
                .Verify(mock => mock.SetSelectedProjectIdAsync(projectId), Times.Exactly(1));
        }
        
        [Fact]
        public void SelectOtherProject_ChangeProjectWithGuestRole_ServiceCalledAndProjectReadonly()
        {
            var projectId = _otherProject.Id;
            _mockProjectRepository
                .Setup(mock => mock.GetRole(projectId, _actingUser.Id))
                .ReturnsAsync(ProjectRole.Guest);
            _mockStateStorageService
                .Setup(mock => mock.SetSelectedProjectIdAsync(projectId))
                .Returns(Task.CompletedTask);

            CreateComponent();
            var projectDropdownOption = _component.Find($"#project-item-{projectId}");
            projectDropdownOption.Click();
            
            _mockStateStorageService
                .Verify(mock => mock.SetSelectedProjectIdAsync(projectId), Times.Exactly(1));
        }
        
        [Fact]
        public void SelectProject_ComponentInitialised_ProjectMemberImagesAreDisplayed() 
        {
            CreateComponent();
            
            const int expectedNumberOfElements = 2;
            _component.FindAll(".user-image-container").Count.Should().Be(expectedNumberOfElements);
        }
        
        [Fact]
        public void SelectProject_NoArchiveProjectPresent_ArchiveTabNotDisplayed()
        {
            CreateComponent(false);
            _component.FindAll("#toggle-select-archive-project").Count.Should().Be(0);
        }

        [Fact]
        public void SelectProject_ArchiveProjectPresent_ArchiveTabDisplayed()
        {
            CreateComponent();
            _component.FindAll("#toggle-select-archive-project").Count.Should().Be(1);
        }
        
        [Fact]
        public void SelectProject_HasTwoProjects_ExpectedNumberOfElementsDisplay()
        {
            CreateComponent();
            
            const int expectedNumberOfElements = 2;
            _component.FindAll(".project-item").Count.Should().Be(expectedNumberOfElements);
        }

        [Fact]
        public void SelectProject_ClickArchiveMenu_ExtraNavItemsDisplays()
        {
            CreateComponent();
            
            _component.Find("#toggle-select-archive-project").Click();
            
            //Two normal projects, and then 1 archived project
            const int expectedNumberOfElements = 3;
            _component.FindAll(".project-item").Count.Should().Be(expectedNumberOfElements);
        }
    }
}