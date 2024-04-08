using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Validators;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Blazor.Modals;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class ProjectBacklogComponentStartSprintTest : BaseProjectScopedComponentTestContext<ProjectBacklog>
{
    private static readonly UserStoryTask TaskWithEstimate     = new() { Estimate = TimeSpan.FromSeconds(10) };
    private static readonly UserStoryTask TaskWithoutEstimate  = new();
    private static readonly UserStory StoryWithEstimatedTasks  = new() { Estimate = 10, Tasks = new List<UserStoryTask> { TaskWithEstimate } };
    private static readonly UserStory StoryWithoutTask         = new() { Estimate = 10 };
    private static readonly UserStory StoryWithUnestimatedTask = new() { Estimate = 10, Tasks = new List<UserStoryTask> { TaskWithoutEstimate } };
    private static readonly UserStory UnestimatedStory         = new() { Tasks = new List<UserStoryTask> { TaskWithEstimate } };
    private readonly Sprint _sprint;
    private readonly User _self = new () { Id = 30, FirstName = "John", LastName = "Smith" };
    private readonly User _creator = new() { FirstName = "Sprint", LastName = "Creator" };
    private List<UserStory> _stories;
    private Project _project = new() { Id = 101 };

    // Mocks
    private readonly Mock<IProjectChangelogRepository> _mockProjectChangelogRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectMembershipService> _mockProjectMembershipService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ProjectBacklog>> _mockLogger = new(MockBehavior.Strict);
    private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
    private readonly Mock<IScrumBoardStateStorageService> _stateStorageService = new(MockBehavior.Strict);
    private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryChangelogRepository> _mockStoryChangelogService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryTaskChangelogRepository> _mockTaskChangelogService = new(MockBehavior.Strict);
    private readonly Mock<IBacklogRepository> _mockBacklogRepository = new(MockBehavior.Strict);
    private readonly Mock<ISprintService> _mockSprintService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryService> _mockUserStoryService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryTaskService> _mockUserStoryTaskService = new(MockBehavior.Strict);
    private readonly Mock<ISortableService<UserStory>> _mockSortableService = new();
    private readonly Mock<IJsInteropService> _mockJsInteropService = new();

    public ProjectBacklogComponentStartSprintTest() {
        _stories = new List<UserStory> { StoryWithEstimatedTasks };
        _sprint = new Sprint
        {
            Id = 101, 
            Created = DateTime.Now, 
            Creator = _creator, 
            Stories = _stories,
            Project = _project,
        };
        _project.Sprints = new List<Sprint> { _sprint };
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
            
        // Used by ArchivedSprintView
        _mockUserStoryRepository
            .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
            .ReturnsAsync(0);
        _mockUserStoryTaskRepository
            .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
            .ReturnsAsync(TimeSpan.Zero);

        _mockProjectMembershipService
            .Setup(mock => mock.RemoveAllReviewersFromProject(It.IsAny<User>(), It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => _mockProjectMembershipService.Object);
        Services.AddScoped(_ => _mockProjectChangelogRepository.Object);
        Services.AddScoped(_ => _mockUserStoryRepository.Object);
        Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
        Services.AddScoped(_ => _mockLogger.Object);
        Services.AddScoped(_ => _mockSprintRepository.Object);
        Services.AddScoped(_ => _stateStorageService.Object);
        Services.AddScoped(_ => _mockSprintChangelogRepository.Object);
        Services.AddScoped(_ => _mockSortableService.Object);
        Services.AddScoped(_ => _mockStoryChangelogService.Object);
        Services.AddScoped(_ => _mockTaskChangelogService.Object);
        Services.AddScoped(_ => _mockJsInteropService.Object);
        Services.AddScoped(_ => _mockSprintService.Object);
        Services.AddScoped(_ => _mockBacklogRepository.Object);
        Services.AddScoped(_ => _mockUserStoryService.Object);
        Services.AddScoped(_ => _mockUserStoryTaskService.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        // Add dummy ModalTrigger
        ComponentFactories.Add(new ModalTriggerComponentFactory());
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
    }

    private void CreateComponent(Sprint sprint, bool isReadOnly = false) {
        _mockSprintRepository
            .Setup(mock => mock.GetByIdAsync(sprint.Id, SprintIncludes.Creator, SprintIncludes.Tasks))
            .ReturnsAsync(sprint);
            
        CreateComponentUnderTest(isReadOnly: isReadOnly, currentProject: _project);
    }

    [Fact]
    public void StartSprint_SprintHasNoStories_ErrorMessageDisplayed() {
        var minlengthAttribute = typeof(SprintStartForm).GetAttribute<MinLengthAttribute>("Stories");

        _sprint.Stories.Clear();
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#stories-validation-message").Any());

        var errorLabel = ComponentUnderTest.Find("#stories-validation-message");

        var expectedErrorMessage = minlengthAttribute.ErrorMessage;
        errorLabel.TextContent.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public void StartSprint_SprintHasUnestimatedStories_ErrorMessageDisplayed() {
        var rangeAttribute = typeof(UserStoryStartForm).GetAttribute<RangeAttribute>("Estimate");

        _stories.Add(UnestimatedStory);
        _sprint.Stories = _stories;
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#story-estimate-validation-message").Any());
        
        var errorLabel = ComponentUnderTest.Find("#story-estimate-validation-message");

        var expectedErrorMessage = rangeAttribute.ErrorMessage;
        errorLabel.TextContent.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public void StartSprint_SprintHasUnestimatedTasks_ErrorMessageDisplayed() {

        _stories.Add(StoryWithUnestimatedTask);
        _sprint.Stories = _stories;
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#tasks-validation-message").Any());

        var errorLabel = ComponentUnderTest.Find("#tasks-validation-message");

        var expectedErrorMessage = "Some tasks are missing estimates";
        errorLabel.TextContent.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public void StartSprint_SprintHasNoTasks_ErrorMessageDisplayed() {
        var lengthAttribute = typeof(UserStoryStartForm).GetAttribute<MinLengthAttribute>("Tasks");

        _stories.Add(StoryWithoutTask);
        _sprint.Stories = _stories;
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#tasks-validation-message").Any());

        var errorLabel = ComponentUnderTest.Find("#tasks-validation-message");

        var expectedErrorMessage = lengthAttribute.ErrorMessage;
        errorLabel.TextContent.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public void StartSprint_NotPastStartDate_ErrorMessageDisplayed() {
        var dateInPastAttribute = typeof(SprintStartForm).GetAttribute<DateInPast>("StartDate");

        _sprint.StartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(2);
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();

        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#start-date-validation-message").Any());
        var errorLabel = ComponentUnderTest.Find("#start-date-validation-message");

        var expectedErrorMessage = dateInPastAttribute.ErrorMessage;
        errorLabel.TextContent.Should().Be(expectedErrorMessage);
    }
        
    [Fact]
    public void StartSprint_PreviousSprintNotClosed_ErrorMessageDisplayed() {
        var notEqualsAttribute = typeof(SprintStartForm).GetAttribute<NotEquals>(nameof(SprintStartForm.PreviousSprintClosed));

        var unclosedSprint = new Sprint
        {
            Stage = SprintStage.ReadyToReview,
            Name = "Previous not closed sprint",
        };
        _project.Sprints.Add(unclosedSprint);
            
        _mockUserStoryRepository
            .Setup(mock => mock.GetEstimateByStoryGroup(unclosedSprint))
            .ReturnsAsync(0);
        _mockUserStoryTaskRepository
            .Setup(mock => mock.GetEstimateByStoryGroup(unclosedSprint))
            .ReturnsAsync(TimeSpan.Zero);
            
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();

        ComponentUnderTest.WaitForState(() => ComponentUnderTest.FindAll("#previous-sprint-closed-validation-message").Any());
        var errorLabel = ComponentUnderTest.Find("#previous-sprint-closed-validation-message");

        var expectedErrorMessage = notEqualsAttribute.ErrorMessage;
        errorLabel.TextContent.Should().Be(expectedErrorMessage);

        _mockUserStoryRepository
            .Verify(mock => mock.GetEstimateByStoryGroup(unclosedSprint), Times.Once);
        _mockUserStoryTaskRepository
            .Verify(mock => mock.GetEstimateByStoryGroup(unclosedSprint), Times.Once);
    }

    [Theory]
    [InlineData("#story-estimate-validation-message")]
    [InlineData("#tasks-validation-message")]
    public void StartSprint_SprintIsValid_NoErrorMessagesDisplayed(string validationId) {
        _sprint.StartDate = DateOnly.FromDateTime(DateTime.Now.AddSeconds(1));
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();

        var errorLabels = ComponentUnderTest.FindAll(validationId);
        errorLabels.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmStartSprint_SprintIsValid_SprintIsStarted() {
        _sprint.Stage.Should().NotBe(SprintStage.Started);
        CreateComponent(_sprint);
            
        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()))
            .Returns(Task.CompletedTask);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.Find("#confirm-Start-sprint").Click();
            
        var arg = new ArgumentCaptor<Sprint>();
        _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once);
        var entry = arg.Value;

        entry.Stage.Should().Be(SprintStage.Started);
    }

    [Fact]
    public void ConfirmStartSprint_SprintIsValid_StartTimeIsRecorded() {
        _sprint.TimeStarted.Should().BeNull();
        CreateComponent(_sprint);
            
        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()))
            .Returns(Task.CompletedTask);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.Find("#confirm-Start-sprint").Click();
            
        var arg = new ArgumentCaptor<Sprint>();
        _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once);
        var entry = arg.Value;

        entry.TimeStarted.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmStartSprint_SprintIsValid_ChangelogEntryCreated() {
        _sprint.Stage = SprintStage.Created;
        CreateComponent(_sprint);

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()))
            .Returns(Task.CompletedTask);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.Find("#confirm-Start-sprint").Click();
            
        _mockSprintRepository
            .Verify(mock => mock.UpdateAsync(It.IsAny<Sprint>()), Times.Once);
        
        _mockProjectMembershipService
            .Verify(mock => mock.RemoveAllReviewersFromProject(It.IsAny<User>(), It.IsAny<Project>()), Times.Once);

        var arg = new ArgumentCaptor<SprintChangelogEntry>();
        _mockSprintChangelogRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once);
        var entry = arg.Value;

        entry.CreatorId.Should().Be(ActingUser.Id);
        entry.FieldChanged.Should().Be(nameof(Sprint.Stage));
        entry.FromValueObject.Should().Be(SprintStage.Created);
        entry.ToValueObject.Should().Be(SprintStage.Started);
    }

    [Fact]
    public void CancelStartSprint_SprintIsValid_SprintNotStarted() {
        _sprint.Stage = SprintStage.Created;
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.Find("#close-modal").Click();

        _mockSprintRepository.Verify(mock => mock.UpdateAsync(It.IsAny<Sprint>()), Times.Never);
        _sprint.Stage.Should().NotBe(SprintStage.Started);
    }

    [Fact]
    public void CancelStartSprint_SprintIsValid_NoChangelogEntry() {
        _sprint.Stage = SprintStage.Created;
        CreateComponent(_sprint);

        ComponentUnderTest.Find("#sprint-start-form").Submit();
        ComponentUnderTest.Find("#close-modal").Click();

        _mockSprintChangelogRepository.Verify(mock => mock.AddAsync(It.IsAny<SprintChangelogEntry>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Backlog_ReadOnly_StartSprintButtonExistsDependingOnReadOnly(bool isReadOnly)
    {
        _sprint.Stage = SprintStage.Created;
        CreateComponent(_sprint, isReadOnly);

        var startSprintButtons = ComponentUnderTest.FindAll("#btn-start-sprint");
        if (isReadOnly)
        {
            startSprintButtons.Should().BeEmpty();
        }
        else
        {
            startSprintButtons.Should().ContainSingle();
        }
    }
}