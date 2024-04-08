using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class ProjectViewComponentTest : BaseProjectScopedComponentTestContext<ProjectView>
    {
        private readonly Mock<StartSprintReviewModal> _mockStartSprintReviewModal = new();
        private readonly Mock<ManageReviewersModal> _mockSelectProjectReviewersModal = new();
        private readonly Mock<CancelSprintReviewModal> _mockCancelSprintReviewModal = new();
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();

        private readonly Mock<ISprintService> _mockSprintService = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
        private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
        private readonly Mock<IProjectChangelogRepository> _mockProjectChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
        private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);
        private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new(MockBehavior.Strict);
        private readonly Mock<IProjectMembershipService> _mockProjectMembershipService = new(MockBehavior.Strict);


        private readonly Project _currentProject;
        private readonly User _actingUser;
        private readonly Sprint _sprint;
        private readonly ProjectUserMembership _membership;
        private List<WorklogEntry> _recentWorklogs = new();

        public ProjectViewComponentTest()
        {
            _actingUser = new User
            {
                Id = 20,
                FirstName = "Jimmy",
                LastName = "Neutron",
            };
            _currentProject = new Project
            {
                Id = 32,
                Name = "Test project",
            };
            _sprint = new Sprint
            {
                Project = _currentProject,
                Name = "Test sprint",
            };
            _currentProject.Sprints.Add(_sprint);
            _membership = new ProjectUserMembership
            {
                Role = ProjectRole.Leader,
                User = _actingUser,
                UserId = _actingUser.Id,
                Project = _currentProject,
                ProjectId = _currentProject.Id,
            };
            _actingUser.ProjectAssociations.Add(_membership);
            _currentProject.MemberAssociations.Add(_membership);
            
            _mockProjectRepository
                .Setup(mock => mock.GetByIdAsync(_currentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(_currentProject);
            _mockSprintRepository
                .Setup(mock => mock.GetByIdAsync(_sprint.Id, SprintIncludes.Story))
                .ReturnsAsync(_sprint);

            _mockWorklogEntryService
                .Setup(mock => mock.GetMostRecentWorklogForProjectAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<long?>()))
                .ReturnsAsync(_recentWorklogs);
            
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockSprintService.Object);
            Services.AddScoped(_ => _mockSprintRepository.Object);
            Services.AddScoped(_ => _mockProjectChangelogRepository.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockStateStorageService.Object);
            Services.AddScoped(_ => _mockWorklogEntryService.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockProjectMembershipService.Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

            ComponentFactories.AddMockComponent(_mockStartSprintReviewModal);
            ComponentFactories.AddMockComponent(_mockCancelSprintReviewModal);
            ComponentFactories.AddMockComponent(_mockSelectProjectReviewersModal);
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
        }

        private void SetupComponent()
        {
            CreateComponentUnderTest(actingUser: _actingUser, currentProject: _currentProject);
        }

        /// <summary>
        /// Prepares the repository mocks for updating the project's memberships
        /// </summary>
        private void SetupUpdateProjectMemberships()
        {
            _mockProjectRepository
                .Setup(mock => mock.UpdateMemberships(_currentProject))
                .Returns(Task.CompletedTask);
            _mockProjectChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<ProjectChangelogEntry>>()))
                .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Prepares the repository mocks for updating a sprint
        /// </summary>
        private void SetupUpdateSprint(bool success)
        {
            _mockSprintService
                .Setup(mock => mock.UpdateStage(_actingUser, _sprint, It.IsAny<SprintStage>()))
                .ReturnsAsync(success);
        }

        [Theory]
        [InlineData(ProjectRole.Developer, false)]
        [InlineData(ProjectRole.Guest, false)]
        [InlineData(ProjectRole.Reviewer, false)]
        [InlineData(ProjectRole.Leader, true)]
        public void Rendered_WithRole_EditButtonShownBasedOnRole(ProjectRole role, bool showEditButton)
        {
            _membership.Role = role;
            SetupComponent();
            var editButtons = ComponentUnderTest.FindAll("#edit-project");
            if (showEditButton) {
                editButtons.Should().ContainSingle();
            }
            else
            {
                editButtons.Should().BeEmpty();
            }
        }

        [Theory] // All stages except for ready to review
        [InlineData(SprintStage.Created)]
        [InlineData(SprintStage.Started)]
        [InlineData(SprintStage.InReview)]
        [InlineData(SprintStage.Reviewed)]
        [InlineData(SprintStage.Closed)]
        public void Rendered_NoReviewableSprint_ReviewSprintButtonNotShown(SprintStage stage)
        {
            _sprint.Stage = stage;
            SetupComponent();
            ComponentUnderTest.FindAll("#start-sprint-review").Should().BeEmpty();
        }

        [Fact] 
        public void Rendered_SelectSprintReviewersPressedAndReviewCancelled_SelectReviewersModalShown()
        {
            _sprint.Stage = SprintStage.ReadyToReview;
            SetupComponent();
            
            _mockSelectProjectReviewersModal
                .Setup(mock => mock.Show(_currentProject))
                .ReturnsAsync(true);
            
            ComponentUnderTest.Find("#manage-sprint-reviewers").Click();
            
            _mockSelectProjectReviewersModal.Verify(mock => mock.Show(_currentProject), Times.Once);
        }

        [Fact]
        public void SprintReadyInReview_CancelPressed_AnotherUserCancelled_ErrorMessageDisplayed()
        {
            _sprint.Stage = SprintStage.InReview;
            SetupComponent();
            
            SetupUpdateSprint(false);
            
            ComponentUnderTest.Find("#cancel-sprint-review").Click();
            ComponentUnderTest.FindAll("#project-view-sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void SprintInReview_CancelPressedModalCancelled_NoChanges()
        {
            _sprint.Stage = SprintStage.InReview;
            SetupComponent();
            
            _mockCancelSprintReviewModal
                .Setup(mock => mock.Show(_sprint))
                .ReturnsAsync(true);

            ComponentUnderTest.Find("#cancel-sprint-review").Click();
            
            _mockCancelSprintReviewModal
                .Verify(mock => mock.Show(_sprint), Times.Once);

            _sprint.Stage.Should().Be(SprintStage.InReview);
        }
        
        [Fact]
        public void SprintInReview_CancelPressed_ReviewersRemovedAndStageUpdated()
        {
            var reviewer = new User
            {
                Id = 77,
            };
            _sprint.Stage = SprintStage.InReview;
            _currentProject.MemberAssociations.Add(new ProjectUserMembership
            {
                User = reviewer,
                Role = ProjectRole.Reviewer,
            });
            
            SetupComponent();
            
            _mockCancelSprintReviewModal
                .Setup(mock => mock.Show(_sprint))
                .ReturnsAsync(false);
            
            _mockProjectMembershipService
                .Setup(mock => mock.RemoveAllReviewersFromProject(_actingUser, _currentProject))
                .Returns(Task.CompletedTask);
            
            SetupUpdateSprint(true);
            SetupUpdateProjectMemberships();
            
            ComponentUnderTest.Find("#cancel-sprint-review").Click();
            
            _mockProjectMembershipService
                .Verify(mock => mock.RemoveAllReviewersFromProject(_actingUser, _currentProject), Times.Once);
            
            _mockSprintService
                .Verify(mock => mock.UpdateStage(_actingUser, _sprint, SprintStage.ReadyToReview));
        }

        [Fact]
        public void SprintInReview_CancelPressed_AnotherUserCancelled_ErrorMessageDisplayed()
        {
            var reviewer = new User
            {
                Id = 77,
            };
            _sprint.Stage = SprintStage.InReview;
            _currentProject.MemberAssociations.Add(new ProjectUserMembership
            {
                User = reviewer,
                Role = ProjectRole.Reviewer,
            });
            
            SetupComponent();
            
            _mockCancelSprintReviewModal
                .Setup(mock => mock.Show(_sprint))
                .ReturnsAsync(false);
            
            SetupUpdateSprint(false);
            SetupUpdateProjectMemberships();
            
            ComponentUnderTest.Find("#cancel-sprint-review").Click();
            
            ComponentUnderTest.FindAll("#project-view-sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void Rendered_NoRecentWorklogs_NoWorklogsDisplayed()
        {
            SetupComponent();
            ComponentUnderTest.FindAll(".recent-worklog").Should().BeEmpty();  
            ComponentUnderTest.FindAll("#no-recent-activity").Should().ContainSingle();           
        }

        [Fact]
        public void Rendered_HasRecentWorklogs_RecentWorklogsDisplayed()
        {
            List<WorklogEntry> someWorklogs = new() {
                new WorklogEntry
                { 
                    User = _actingUser,                
                    Description = "description1", 
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now, 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromMinutes(10)) },
                },
                new WorklogEntry
                { 
                    User = _actingUser,                
                    Description = "description2", 
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now, 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromMinutes(20)) },
                },
                new WorklogEntry
                { 
                    User = _actingUser,                
                    Description = "description3", 
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now, 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromMinutes(30)) },
                },
            };

            _mockWorklogEntryService
                .Setup(mock => mock.GetMostRecentWorklogForProjectAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<long?>()))
                .ReturnsAsync(someWorklogs);
            SetupComponent();

            ComponentUnderTest.FindAll(".recent-worklog").Should().HaveCount(3);  
            ComponentUnderTest.FindAll("#no-recent-activity").Should().BeEmpty();
        }
    }
}