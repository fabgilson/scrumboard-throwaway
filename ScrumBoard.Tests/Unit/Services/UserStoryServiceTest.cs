// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Moq;
// using ScrumBoard.Models.Entities;
// using ScrumBoard.Models.Entities.Changelog;
// using ScrumBoard.Repositories;
// using ScrumBoard.Repositories.Changelog;
// using ScrumBoard.Services;
// using ScrumBoard.Tests.Util;
// using Xunit;
//
// namespace ScrumBoard.Tests.Unit.Services
// {
//     public class UserStoryServiceTest
//     {
//         private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict); 
//         private readonly Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new(MockBehavior.Strict); 
//         
//         private readonly IUserStoryService _userStoryService;
//
//         private readonly List<UserStory> _stories;
//         private readonly User _actingUser;
//         
//         public UserStoryServiceTest()
//         {
//             _actingUser = new User()
//             {
//                 Id = 13,
//                 FirstName = "Jimmy",
//                 LastName = "Neutron",
//             };
//             _stories = new List<UserStory>()
//             {
//                 new()
//                 {
//                     Id = 1,
//                     Stage = Stage.Todo
//                 },
//                 new()
//                 {
//                     Id = 2,
//                     Stage = Stage.InProgress
//                 },
//                 new()
//                 {
//                     Id = 3,
//                     Stage = Stage.UnderReview
//                 },
//                 new()
//                 {
//                     Id = 4,
//                     Stage = Stage.Done,
//                 },
//                 new()
//                 {
//                     Id = 5,
//                     Stage = Stage.Deferred
//                 },
//             };
//             
//             _mockUserStoryRepository
//                 .Setup(mock => mock.UpdateAllAsync(It.IsAny<IEnumerable<UserStory>>()))
//                 .Returns(Task.CompletedTask);
//             _mockUserStoryChangelogRepository
//                 .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryChangelogEntry>>()))
//                 .Returns(Task.CompletedTask);
//             
//             _userStoryService = new UserStoryService(_mockUserStoryRepository.Object, _mockUserStoryChangelogRepository.Object);
//         }
//
//         [Theory]
//         [EnumData(typeof(Stage))]
//         public async Task UpdateStages_MappedToStage_StoryStagesUpdated(Stage stage)
//         {
//             var mapping = new Mock<Func<Stage, Stage>>();
//             mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(stage);
//             await _userStoryService.UpdateStages(_actingUser, _stories, mapping.Object);
//             mapping.Verify(mock => mock(It.IsAny<Stage>()), Times.Exactly(_stories.Count));
//             _stories.Should().OnlyContain(story => story.Stage == stage);
//         }
//         
//         [Theory]
//         [EnumData(typeof(Stage))]
//         public async Task UpdateStages_MappedToStage_MappingCalledWithStoryStage(Stage stage)
//         {
//             _stories.Clear();
//             _stories.Add(new UserStory()
//             {
//                 Id = 1,
//                 Stage = stage,
//             });
//             
//             var mapping = new Mock<Func<Stage, Stage>>();
//             mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(Stage.Deferred);
//             await _userStoryService.UpdateStages(_actingUser, _stories, mapping.Object);
//             mapping.Verify(mock => mock(stage), Times.Once);
//         }
//         
//         [Fact]
//         public async Task UpdateStages_WithStories_ChangelogEntriesAdded()
//         {
//             var targetStage = Stage.Deferred;
//             var updatedStories = _stories
//                 .Where(story => story.Stage != targetStage)
//                 .Select(story => story.CloneForPersisting())
//                 .ToList();
//             
//             var mapping = new Mock<Func<Stage, Stage>>();
//             mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(targetStage);
//             
//             
//             await _userStoryService.UpdateStages(_actingUser, _stories, mapping.Object);
//
//             var changeCaptor = new ArgumentCaptor<IEnumerable<UserStoryChangelogEntry>>();
//             _mockUserStoryChangelogRepository
//                 .Verify(mock => mock.AddAllAsync(changeCaptor.Capture()));
//             var changes = changeCaptor.Value.ToList();
//             changes.Should().HaveSameCount(updatedStories);
//
//             foreach (var (change, story) in changes.Zip(updatedStories))
//             {
//                 change.CreatorId.Should().Be(_actingUser.Id);
//                 change.UserStoryChangedId.Should().Be(story.Id);
//                 change.FieldChanged.Should().Be(nameof(UserStory.Stage));
//                 change.ToValueObject.Should().Be(targetStage);
//                 change.FromValueObject.Should().Be(story.Stage);
//             }
//         }
//         
//         [Fact]
//         public async Task UpdateStages_WithStories_StoriesUpdated()
//         {
//             var targetStage = Stage.Deferred;
//             var updatedStories = _stories
//                 .Where(story => story.Stage != targetStage)
//                 .Select(story => story.CloneForPersisting())
//                 .ToList();
//             
//             var mapping = new Mock<Func<Stage, Stage>>();
//             mapping.Setup(mock => mock(It.IsAny<Stage>())).Returns(targetStage);
//             
//             
//             await _userStoryService.UpdateStages(_actingUser, _stories, mapping.Object);
//
//             var savedStoryCaptor = new ArgumentCaptor<IEnumerable<UserStory>>();
//             _mockUserStoryRepository
//                 .Verify(mock => mock.UpdateAllAsync(savedStoryCaptor.Capture()));
//             var savedStories = savedStoryCaptor.Value.ToList();
//             savedStories.Should().HaveSameCount(updatedStories);
//
//             foreach (var (savedStory, story) in savedStories.Zip(updatedStories))
//             {
//                 savedStory.Id.Should().Be(story.Id);
//                 savedStory.Stage.Should().Be(targetStage);
//             }
//         }
//     }
// }
//
