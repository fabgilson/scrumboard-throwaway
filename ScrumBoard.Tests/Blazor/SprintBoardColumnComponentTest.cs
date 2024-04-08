using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using Xunit;
using ScrumBoard.Shared.SprintBoard;
using System.Collections.Generic;
using ScrumBoard.Tests.Util;
using System.Linq;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Blazor.Modals;

namespace ScrumBoard.Tests.Blazor
{
    public class SprintBoardColumnComponentTest : TestContext
    {

        private IRenderedComponent<SprintBoardColumn> _component;

        private Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
        private Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
        private Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new(MockBehavior.Strict);
        private Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new(MockBehavior.Strict);
        private Mock<IJsInteropService> _mockJSInteropService = new();
        private Mock<ISortableService<UserStoryTask>> _mockSortableService = new();

        private User _actingUser;
        private UserStory _story;

        private Stage _stage = Stage.UnderReview;

        public SprintBoardColumnComponentTest()
        {
            _actingUser = new User()
            {
                Id = 13,
                FirstName = "Jimmy",
                LastName = "Neutron",
            };
            _story = new UserStory() { Id = 17, Project = new Project(), Stage = Stage.InProgress };
            
            _mockUserStoryRepository
                .Setup(mock => mock.GetByIdAsync(_story.Id, UserStoryIncludes.Tasks))
                .ReturnsAsync(_story);

            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);
            Services.AddScoped(_ => _mockJSInteropService.Object);
            Services.AddScoped(_ => _mockSortableService.Object);

            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());

            ComponentFactories.AddDummyFactoryFor<SprintBoardTask>();
        }

        private void CreateComponent(bool sprintReadOnly = false, bool projectReadOnly = false) {
                _component = RenderComponent<SprintBoardColumn>(parameters => parameters
                    .Add(cut => cut.Story, _story)
                    .Add(cut => cut.ColumnStage, _stage)
                    .Add(cut => cut.Tasks, _story.Tasks)
                    .AddCascadingValue("Self", _actingUser)
                    .AddCascadingValue("IsSprintReadOnly", sprintReadOnly)
                    .AddCascadingValue("ProjectState", new ProjectState{IsReadOnly = projectReadOnly})
                );
        }

        /// <summary>
        /// Prepares the mock repositories for updating a task
        /// </summary>
        private void SetupTaskUpdate()
        {
            _mockUserStoryTaskRepository
                .Setup(mock => mock.UpdateAsync(It.IsAny<UserStoryTask>()))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task ComponentLoaded_TaskAdded_TaskStageUpdated()
        {
            var task = new UserStoryTask()
            {
                Creator = _actingUser, 
                UserStoryId = _story.Id, 
                Stage = Stage.InProgress, 
                Name = "Test task",
                Tags = new List<UserStoryTaskTag>(),
            };
            CreateComponent();
            SetupTaskUpdate();
            
            await _component.InvokeAsync(() => _component.Instance.ItemAdded(task));
        
            task.Stage.Should().Be(_stage);
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateAsync(It.IsAny<UserStoryTask>()), Times.Once());
        }

        [Fact]
        public async Task ComponentLoaded_TaskAdded_ChangelogEntryAdded()
        {
            var oldStage = Stage.InProgress;
            var task = new UserStoryTask()
            {
                UserStoryId = _story.Id, 
                Stage = oldStage,
                Name = "Test task",
                Tags = new List<UserStoryTaskTag>(),
            };
            CreateComponent();
            SetupTaskUpdate();
            
            await _component.InvokeAsync(() => _component.Instance.ItemAdded(task));
        
            task.Stage.Should().Be(_stage);

            var arg = new ArgumentCaptor<IEnumerable<UserStoryTaskChangelogEntry>>();
            _mockUserStoryTaskChangelogRepository.Verify(mock => mock.AddAllAsync(arg.Capture()), Times.Once());
            var changes = arg.Value;
            changes.Should().HaveCount(1);
        
            var change = changes.First();
            change.FieldChanged.Should().Be(nameof(UserStoryTask.Stage));
            change.FromValueObject.Should().Be(oldStage);
            change.ToValueObject.Should().Be(_stage);
        }

        [Fact]
        public async Task ComponentLoaded_TaskAddedAndStoryStageNeedsUpdating_StoryStageUpdated()
        {
            // Moving first task from todo to in progress, should update story to in progress
            _stage = Stage.InProgress;
            _story.Stage = Stage.Todo;
            var task = new UserStoryTask()
            {
                UserStoryId = _story.Id, 
                Stage = Stage.Todo, 
                Name = "Test task", 
                Tags = new List<UserStoryTaskTag>(),
            };
            CreateComponent();
            SetupTaskUpdate();

            _mockUserStoryRepository
                .Setup(mock => mock.UpdateAsync(It.IsAny<UserStory>()))
                .Returns(Task.CompletedTask);
            _mockUserStoryChangelogRepository
                .Setup(mock => mock.AddAsync(It.IsAny<UserStoryChangelogEntry>()))
                .Returns(Task.CompletedTask);
            
            await _component.InvokeAsync(() => _component.Instance.ItemAdded(task));

            _story.Stage.Should().Be(Stage.InProgress);
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(It.IsAny<UserStory>()), Times.Once());
        }

        [Fact]
        public void ComponentLoaded_StoryWithTasks_OnlyTaskWithMatchingStageShown()
        {
            var matchingTask = new UserStoryTask() { Id = 4, UserStoryId = _story.Id, Stage = _stage, Tags = new List<UserStoryTaskTag>() };
            var mismatchingTask = new UserStoryTask() { Id = 7, UserStoryId = _story.Id, Stage = Stage.Todo, Tags = new List<UserStoryTaskTag>() };

            _story.Tasks = new List<UserStoryTask>() {
                matchingTask,
                mismatchingTask,
            };
            
            CreateComponent();

            var cards = _component.FindComponents<Dummy<SprintBoardTask>>();
            cards.Should().ContainSingle();
            var card = cards.Single();
            card.Instance.GetParam(x => x.TaskModel).Should().Be(matchingTask);
        }
        
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void ComponentRendered_IsReadOnlySet_SortableListDisabledBasedOnReadOnly(bool projectReadOnly, bool sprintReadOnly, bool listDisabled)
        {
            CreateComponent(sprintReadOnly, projectReadOnly);

            var sortableList = _component.FindComponents<SortableList<UserStoryTask>>();
            sortableList.Should().ContainSingle();
            sortableList.Should().OnlyContain(input => input.Instance.Disabled == listDisabled);
        }
    }
}
