using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services
{
    public class UserStoryTaskServiceTest
    {
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict); 
        private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IStandUpMeetingRepository> _mockStandUpMeetingRepository = new(MockBehavior.Strict);
        
        private readonly IUserStoryTaskService _userStoryTaskService;

        private readonly List<UserStoryTask> _tasks;
        private readonly User _actingUser;
        
        public UserStoryTaskServiceTest()
        {
            _actingUser = new User()
            {
                Id = 13,
                FirstName = "Jimmy",
                LastName = "Neutron",
            };
            _tasks = new List<UserStoryTask>()
            {
                new()
                {
                    Id = 1,
                    Stage = Stage.Todo
                },
                new()
                {
                    Id = 2,
                    Stage = Stage.InProgress
                },
                new()
                {
                    Id = 3,
                    Stage = Stage.UnderReview
                },
                new()
                {
                    Id = 4,
                    Stage = Stage.Done,
                },
                new()
                {
                    Id = 5,
                    Stage = Stage.Deferred
                },
            };
            
            _mockUserStoryTaskRepository
                .Setup(mock => mock.UpdateAllAsync(It.IsAny<IEnumerable<UserStoryTask>>()))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()))
                .Returns(Task.CompletedTask);
            
            _userStoryTaskService = new UserStoryTaskService(
                _mockUserStoryTaskRepository.Object, 
                _mockUserStoryTaskChangelogRepository.Object,
                _mockStandUpMeetingRepository.Object
            );
        }

        [Theory]
        [EnumData(typeof(Stage))]
        public async Task UpdateStages_MappedToStage_TaskStagesUpdated(Stage stage)
        {
            var mapping = new Mock<Func<Stage, Stage>>();
            mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(stage);
            await _userStoryTaskService.UpdateStages(_actingUser, _tasks, mapping.Object);
            mapping.Verify(mock => mock(It.IsAny<Stage>()), Times.Exactly(_tasks.Count));
            _tasks.Should().OnlyContain(story => story.Stage == stage);
        }
        
        [Theory]
        [EnumData(typeof(Stage))]
        public async Task UpdateStages_MappedToStage_MappingCalledWithStoryStage(Stage stage)
        {
            _tasks.Clear();
            _tasks.Add(new UserStoryTask()
            {
                Id = 1,
                Stage = stage,
            });
            
            var mapping = new Mock<Func<Stage, Stage>>();
            mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(Stage.Deferred);
            await _userStoryTaskService.UpdateStages(_actingUser, _tasks, mapping.Object);
            mapping.Verify(mock => mock(stage), Times.Once);
        }
        
        [Fact]
        public async Task UpdateStages_WithStories_ChangelogEntriesAdded()
        {
            var targetStage = Stage.Deferred;
            var updatedTasks = _tasks
                .Where(story => story.Stage != targetStage)
                .Select(story => story.CloneForPersisting())
                .ToList();
            
            var mapping = new Mock<Func<Stage, Stage>>();
            mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(targetStage);
            
            
            await _userStoryTaskService.UpdateStages(_actingUser, _tasks, mapping.Object);

            var changeCaptor = new ArgumentCaptor<IEnumerable<UserStoryTaskChangelogEntry>>();
            _mockUserStoryTaskChangelogRepository
                .Verify(mock => mock.AddAllAsync(changeCaptor.Capture()));
            var changes = changeCaptor.Value.ToList();
            changes.Should().HaveSameCount(updatedTasks);

            foreach (var (change, task) in changes.Zip(updatedTasks))
            {
                change.CreatorId.Should().Be(_actingUser.Id);
                change.UserStoryTaskChangedId.Should().Be(task.Id);
                change.FieldChanged.Should().Be(nameof(UserStoryTask.Stage));
                change.ToValueObject.Should().Be(targetStage);
                change.FromValueObject.Should().Be(task.Stage);
            }
        }
        
        [Fact]
        public async Task UpdateStages_WithStories_StoriesUpdated()
        {
            var targetStage = Stage.Deferred;
            var updatedStories = _tasks
                .Where(story => story.Stage != targetStage)
                .Select(story => story.CloneForPersisting())
                .ToList();
            
            var mapping = new Mock<Func<Stage, Stage>>();
            mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(targetStage);
            
            
            await _userStoryTaskService.UpdateStages(_actingUser, _tasks, mapping.Object);

            var savedTaskCaptor = new ArgumentCaptor<IEnumerable<UserStoryTask>>();
            _mockUserStoryTaskRepository
                .Verify(mock => mock.UpdateAllAsync(savedTaskCaptor.Capture()));
            var savedTasks = savedTaskCaptor.Value.ToList();
            savedTasks.Should().HaveSameCount(updatedStories);

            foreach (var (savedTask, task) in savedTasks.Zip(updatedStories))
            {
                savedTask.Id.Should().Be(task.Id);
                savedTask.Stage.Should().Be(targetStage);
            }
        }
    }
}

