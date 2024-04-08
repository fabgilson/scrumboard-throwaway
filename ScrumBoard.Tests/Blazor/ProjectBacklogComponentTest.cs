using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Repositories;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Pages;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Util;
using Xunit;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Blazor.Modals;

namespace ScrumBoard.Tests.Blazor
{
    public class ProjectBacklogComponentTest : BaseProjectScopedComponentTestContext<ProjectBacklog>
    {
        private readonly UserStoryTask _taskTodo     = new() { Id = 1, Stage = Stage.Todo};
        private readonly UserStoryTask _taskProgress = new() { Id = 2, Stage = Stage.InProgress};
        private readonly UserStoryTask _taskReview   = new() { Id = 3, Stage = Stage.UnderReview};
        private readonly UserStoryTask _taskDone     = new() { Id = 4, Stage = Stage.Done};
        private readonly UserStoryTask _taskDeferred = new() { Id = 5, Stage = Stage.Deferred };

        private readonly UserStory _storyTodo     = new() { Id = 6,  Stage = Stage.Todo};
        private readonly UserStory _storyProgress = new() { Id = 7,  Stage = Stage.InProgress};
        private readonly UserStory _storyReview   = new() { Id = 8,  Stage = Stage.UnderReview};
        private readonly UserStory _storyDone     = new() { Id = 9,  Stage = Stage.Done};
        private readonly UserStory _storyDeferred = new() { Id = 10, Stage = Stage.Deferred };

        private readonly Project _project = new() { Id = 101 };
        private readonly Sprint _sprint;

        private readonly User _self = new () { Id = 32, FirstName = "John", LastName = "Smith" };

        private IRenderedComponent<ProjectBacklog> _component;
        
        // Mocks
        private readonly Mock<IProjectChangelogRepository> _mockProjectChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
        private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
        private readonly Mock<IScrumBoardStateStorageService> _stateStorageService = new(MockBehavior.Strict);
        private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskChangelogRepository> _mockTaskChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryChangelogRepository> _mockStoryChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IBacklogRepository> _mockBacklogRepository = new(MockBehavior.Strict);
        private readonly Mock<ISprintService> _mockSprintService = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryService> _mockUserStoryService = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskService> _mockUserStoryTaskService = new(MockBehavior.Strict);
        private readonly Mock<IProjectMembershipService> _mockProjectMembershipService = new(MockBehavior.Strict);
        private readonly Mock<ILogger<ProjectBacklog>> _mockLogger = new();
        private readonly Mock<ISortableService<UserStory>> _mockSortableService = new();
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        private readonly Mock<CloseSprintModal> _mockCloseSprintModal = new();

        public ProjectBacklogComponentTest()
        {
            _storyTodo.Tasks = new List<UserStoryTask> {_taskTodo, _taskProgress, _taskReview, _taskDone};
            _sprint = new Sprint
            {
                Id = 14,
                Stories = new List<UserStory> {_storyTodo, _storyProgress, _storyReview, _storyDone},
                Project = _project,
                Creator = _self,
                Stage = SprintStage.Started
            };
            _project.Sprints = new List<Sprint> {_sprint};
            ProjectUserMembership membership = new()
                {User = _self, UserId = _self.Id, Project = _project, ProjectId = _project.Id};
            _self.ProjectAssociations.Add(membership);
            _project.MemberAssociations.Add(membership);
            
             _mockProjectRepository
                .Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(_project);
             _mockUserStoryRepository
                .Setup(mock => mock.GetByStoryGroupAsync(It.IsAny<Backlog>(), UserStoryIncludes.Tasks))
                .ReturnsAsync(new List<UserStory>());
            _mockSprintRepository
                .Setup(mock => mock.GetByIdAsync(_sprint.Id, SprintIncludes.Creator, SprintIncludes.Tasks))
                .ReturnsAsync(_sprint);
            
            // Used by ArchivedSprintView
            _mockUserStoryRepository
                .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
                .ReturnsAsync(0);
            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
                .ReturnsAsync(TimeSpan.Zero);
            
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockBacklogRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockProjectChangelogRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockLogger.Object);
            Services.AddScoped(_ => _mockSprintRepository.Object);
            Services.AddScoped(_ => _stateStorageService.Object);
            Services.AddScoped(_ => _mockSprintChangelogRepository.Object);
            Services.AddScoped(_ => _mockSortableService.Object);
            Services.AddScoped(_ => _mockStoryChangelogRepository.Object);
            Services.AddScoped(_ => _mockTaskChangelogRepository.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockSprintService.Object);
            Services.AddScoped(_ => _mockUserStoryService.Object);
            Services.AddScoped(_ => _mockUserStoryTaskService.Object);
            Services.AddScoped(_ => new Mock<IProjectMembershipService>().Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
            
            // Add mock components
            ComponentFactories.AddDummyFactoryFor<FullStory>();
            ComponentFactories.AddMockComponent(_mockCloseSprintModal);
            ComponentFactories.Add(new ModalTriggerComponentFactory());
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
        }

        private void CreateComponent(bool isReadOnly = false)
        {
            CreateComponentUnderTest(actingUser: _self, isReadOnly: isReadOnly, currentProject: _project);
        }
        
        /// <summary>
        /// Prepares the mock repositories for a sprint stage change with necessary story & task changes
        /// </summary>
        private void SetupSprintStageChange()
        {
            _mockUserStoryService
                .Setup(mock => mock.UpdateStages(_self, _sprint, It.IsAny<Func<Stage, Stage>>()))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskService
                .Setup(mock => mock.UpdateStages(_self, _sprint, It.IsAny<Func<Stage, Stage>>()))
                .Returns(Task.CompletedTask);
            _mockSprintChangelogRepository
                .Setup(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()))
                .Returns(Task.CompletedTask);
            _mockSprintRepository
                .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
                .Returns(Task.CompletedTask);
            _mockSprintRepository
                .Setup(mock => mock.ProjectHasCurrentSprintAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(false));
            _mockProjectMembershipService
                .Setup(mock => mock.RemoveAllReviewersFromProject(_self, _project))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public void ConfirmStartSprint_SprintIsNotStarted_SprintSetStarted() {
            CreateComponent();            

            Renderer.Dispatcher.InvokeAsync(() => ComponentUnderTest.Instance.StartCurrentSprint());
            
            ComponentUnderTest.Find("#confirm-Start-sprint").Click();
            _sprint.Stage.Should().Be(SprintStage.Started);
        }

        [Fact]
        public void ConfirmStartSprint_AnotherUserStarted_ErrorMessageDisplayed() {
            CreateComponent();          

            _mockSprintRepository.Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
                .Throws(new DbUpdateConcurrencyException("Concurrency Error")); 

            Renderer.Dispatcher.InvokeAsync(() => ComponentUnderTest.Instance.StartCurrentSprint());;
            ComponentUnderTest.Find("#confirm-Start-sprint").Click();

            ComponentUnderTest.FindAll("#backlog-sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void ConfirmEndSprint_SprintIsNotReadyToReview_SprintSetToReadyToReview() {
            CreateComponent();
            SetupSprintStageChange();

            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#confirm-Finish-sprint").Click();

            _sprint.Stage.Should().Be(SprintStage.ReadyToReview);
        }

        [Fact]
        public void ConfirmEndSprint_SprintEditedByAnotherUser_ErrorMessageDisplayed() {
            CreateComponent();
            SetupSprintStageChange();
            _mockSprintRepository.Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
                .Throws(new DbUpdateConcurrencyException("Concurrency Error"));

            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#confirm-Finish-sprint").Click();

            ComponentUnderTest.FindAll("#backlog-sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void ConfirmEndSprint_SprintHasTaskInDone_TaskNotDeferred()
        {
            CreateComponent();
            SetupSprintStageChange();
            _sprint.Stories.Where(s => s.Tasks.Any(t => t.Stage.Equals(Stage.Done))).Should().ContainSingle();
            
            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#confirm-Finish-sprint").Click();
            
            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once);
            arg.Value.Id.Should().Be(_sprint.Id);

            _sprint.Stories.Where(s => s.Tasks.Any(t => t.Stage.Equals(Stage.Done))).Should()
                .ContainSingle();
        }

        [Fact]
        public void ConfirmEndSprint_SprintIsNotReadyToReview_ChangelogEntryCreated() {
            CreateComponent();
            SetupSprintStageChange();

            var startingStage = _sprint.Stage;

            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#confirm-Finish-sprint").Click();

            var arg = new ArgumentCaptor<SprintChangelogEntry>();
            _mockSprintChangelogRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once);
            var entry = arg.Value;

            entry.CreatorId.Should().Be(_self.Id);
            entry.FieldChanged.Should().Be(nameof(Sprint.Stage));
            entry.FromValueObject.Should().Be(startingStage);
            entry.ToValueObject.Should().Be(SprintStage.ReadyToReview);
        }

        [Fact]
        public void ConfirmEndSprint_SprintIsNotReadyToReview_UpdatesStagesCorrectly() {
            CreateComponent();
            SetupSprintStageChange();
            
            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#confirm-Finish-sprint").Click();

            var stageMappingCaptor = new ArgumentCaptor<Func<Stage, Stage>>();
            _mockUserStoryService
                .Verify(mock => mock.UpdateStages(_self, _sprint, stageMappingCaptor.Capture()), Times.Once);
            _mockUserStoryTaskService
                .Verify(mock => mock.UpdateStages(_self, _sprint, stageMappingCaptor.Capture()), Times.Once);

            stageMappingCaptor.Values.Select(mapping => mapping(Stage.Done)).Should().AllBeEquivalentTo(Stage.Done);
            foreach (var mapping in stageMappingCaptor.Values)
            {
                foreach (var stage in Enum.GetValues<Stage>().Except(new[]{ Stage.Todo}))
                {
                    mapping(stage).Should().Be(stage);
                }
                mapping(Stage.Todo).Should().Be(Stage.Deferred);
            }
        }


        [Fact]
        public void CancelEndSprint_SprintIsNotReadyToReview_SprintDoesNotEnd() {
            CreateComponent();
            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#close-modal").Click();

            _mockSprintRepository.Verify(mock => mock.UpdateAsync(It.IsAny<Sprint>()), Times.Never);

            _sprint.Stage.Should().NotBe(SprintStage.ReadyToReview);
        }

        [Fact]
        public void CancelEndSprint_SprintIsNotReadyToReview_StagesNotUpdated() {
            CreateComponent();
            ComponentUnderTest.Find("#btn-end-sprint").Click();
            ComponentUnderTest.Find("#close-modal").Click();

            _mockUserStoryService
                .Verify(mock => mock.UpdateStages(_self, _sprint, It.IsAny<Func<Stage,Stage>>()), Times.Never);
            _mockUserStoryTaskService
                .Verify(mock => mock.UpdateStages(_self, _sprint, It.IsAny<Func<Stage,Stage>>()), Times.Never);
        }
        
        [Fact]
        public void CloseSprint_SprintReviewed_SprintIsClosed() {
            _sprint.Stage = SprintStage.Reviewed;
            CreateComponent();

            _mockSprintService
                .Setup(mock => mock.UpdateStage(_self, _sprint, SprintStage.Closed))
                .Returns(Task.FromResult(false));
            _mockCloseSprintModal
                .Setup(mock => mock.Show(_sprint))
                .ReturnsAsync(false);

            ComponentUnderTest.Find("#btn-close-sprint").Click();
            
            _mockSprintService.Verify(mock => mock.UpdateStage(_self, _sprint, SprintStage.Closed), Times.Once);
        }
        
        [Fact]
        public void CloseSprint_SprintReviewedAndCancelled_SprintNotClosed() {
            _sprint.Stage = SprintStage.Reviewed;
            CreateComponent();
            
            _mockCloseSprintModal
                .Setup(mock => mock.Show(_sprint))
                .ReturnsAsync(true);
            
            ComponentUnderTest.Find("#btn-close-sprint").Click();
        }

        [Fact]
        public void ConfirmReopenSprint_SprintIsReadyToReview_SprintIsReopened() {
            _sprint.Stage = SprintStage.ReadyToReview;
            CreateComponent();
            SetupSprintStageChange();

            _mockSprintRepository
                .Setup(mock => mock.ProjectHasCurrentSprintAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(false));

            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#confirm-Reopen-sprint").Click();

            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once);
            var entry = arg.Value;

            entry.Stage.Should().Be(SprintStage.Created);
        }

        [Fact]
        public void ConfirmReopenSprint_SprintIsReadyToReview_AnotherUserReopenedSprint_ErrorMessageDisplayed() {
            _sprint.Stage = SprintStage.ReadyToReview;
            CreateComponent();

            _mockSprintRepository
                .Setup(mock => mock.ProjectHasCurrentSprintAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(true));
                
            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#confirm-Reopen-sprint").Click();

            ComponentUnderTest.FindAll("#backlog-sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void CancelReopenSprint_SprintIsReadyToReview_SprintIsNotReopened() {
            _sprint.Stage = SprintStage.ReadyToReview;
            CreateComponent();
            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#close-modal").Click();

            _mockSprintRepository.Verify(mock => mock.UpdateAsync(It.IsAny<Sprint>()), Times.Never);
            _sprint.Stage.Should().Be(SprintStage.ReadyToReview);
        }

        [Fact]
        public void ConfirmReopenSprint_SprintIsReadyToReview_ChanglogEntryCreated() {
            _sprint.Stage = SprintStage.ReadyToReview;
            CreateComponent();
            SetupSprintStageChange();
            
            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#confirm-Reopen-sprint").Click();

            var arg = new ArgumentCaptor<SprintChangelogEntry>();
            _mockSprintChangelogRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once);
            var entry = arg.Value;

            entry.CreatorId.Should().Be(_self.Id);
            entry.FieldChanged.Should().Be(nameof(Sprint.Stage));
            entry.FromValueObject.Should().Be(SprintStage.ReadyToReview);
            entry.ToValueObject.Should().Be(SprintStage.Created);
        }

        [Fact]
        public void CancelReopenSprint_SprintIsReadyToReview_ChangelogEntryNotCreated() {
            _sprint.Stage = SprintStage.ReadyToReview;
            CreateComponent();
            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#close-modal").Click();

            _mockSprintChangelogRepository.Verify(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()), Times.Never);
        }

        [Fact]
        public void ConfirmReopenSprint_SprintHasDeferredStories_StoriesAndTasksMovedToTodo() { 
            _sprint.Stage = SprintStage.ReadyToReview;
            _storyDeferred.Tasks = new List<UserStoryTask> { _taskDeferred };
            _sprint.Stories = new List<UserStory> { _storyDeferred };
            CreateComponent();
            SetupSprintStageChange();

            ComponentUnderTest.Find("#btn-reopen-sprint").Click();
            ComponentUnderTest.Find("#confirm-Reopen-sprint").Click();

            var stageMappingCaptor = new ArgumentCaptor<Func<Stage, Stage>>();
            _mockUserStoryService
                .Verify(mock => mock.UpdateStages(_self, _sprint, stageMappingCaptor.Capture()), Times.Once);
            _mockUserStoryTaskService
                .Verify(mock => mock.UpdateStages(_self, _sprint, stageMappingCaptor.Capture()), Times.Once);

            stageMappingCaptor.Values.Select(mapping => mapping(Stage.Deferred)).Should().AllBeEquivalentTo(Stage.Todo);
            foreach (var mapping in stageMappingCaptor.Values)
                Enum.GetValues<Stage>().Except(new[]{ Stage.Deferred }).Should().OnlyContain(stage => mapping(stage) == stage);
        }

        [Fact]
        public void Backlog_DoNothing_FullStoryShouldNotBeShown()
        {
            CreateComponent();
            ComponentUnderTest.FindComponents<Dummy<FullStory>>().Should().BeEmpty();
        }
        
        [Fact]
        public void Backlog_ClickCreateStory_FullStoryShouldBeShownWithNewStory()
        {
            CreateComponent();
            
            ComponentUnderTest.Find("#add-story").Click();

            var fullStories = ComponentUnderTest.FindComponents<Dummy<FullStory>>();
            fullStories.Should().ContainSingle();
            var fullStoryComponent = fullStories.First();

            var story = (UserStory)fullStoryComponent.Instance.Parameters["Story"];
            story.Id.Should().Be(default);
            story.ProjectId.Should().Be(_project.Id);
            story.StoryGroupId.Should().Be(_project.Backlog.Id);
        }

        [Fact]
        public void Backlog_IsReadOnly_AddStoryButtonDoesNotExist()
        {
            CreateComponent(true);
            ComponentUnderTest.FindAll("#add-story").Should().BeEmpty();
        }

        [Fact]
        public void Backlog_IsReadOnly_EndSprintButtonDoesNotExist()
        {
            CreateComponent(true);
            ComponentUnderTest.FindAll("#btn-end-sprint").Should().BeEmpty();
        }
    }
}

