using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Diffing.Extensions;
using Bogus.DataSets;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class FullStoryComponentTest : BaseProjectScopedComponentTestContext<FullStory>
{
    private readonly Mock<IJsInteropService> _mockJsInteropService = new();
    private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryTaskService> _mockUserStoryTaskService = new(MockBehavior.Strict);
    private readonly Mock<IUserStoryService> _mockUserStoryService = new(MockBehavior.Strict);
    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
        
    private readonly UserStory _story;
    private readonly Sprint _sprint;

    private RenderFragment _modalRenderFragmentContent;
    private IRenderedFragment RenderedModal => Render(_modalRenderFragmentContent);
    private bool _refreshStoryCalled;

    public FullStoryComponentTest()
    {
        _sprint = new Sprint()
        {
            Id = 10,
            Name = "Test sprint",
            Stage = SprintStage.Started,
        };
        _story = new UserStory()
        {
            Id = 5,
            Name = "test story name",
            Description = "test story description",
            Creator = ActingUser,
            StoryGroup = _sprint,
            AcceptanceCriterias = new List<AcceptanceCriteria>(),
        };

        _mockUserStoryChangelogRepository
            .Setup(mock => mock.GetByUserStoryAsync(_story, UserStoryChangelogIncludes.Display))
            .ReturnsAsync(new List<UserStoryChangelogEntry>());
        
        _mockUserStoryService.Setup(x => x.DeferStory(It.IsAny<User>(), It.IsAny<UserStory>())).Returns(Task.CompletedTask);
        _mockUserStoryTaskService.Setup(x => x.DeferAllIncompleteTasksInStory(It.IsAny<User>(), It.IsAny<UserStory>())).Returns(Task.CompletedTask);
        _mockWorklogEntryService.Setup(x => x.GetWorklogEntriesForStoryAsync(It.IsAny<long>())).ReturnsAsync(new List<WorklogEntry>());    
        
        Services.AddScoped(_ => _mockWorklogEntryService.Object);
        Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);
        Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
        Services.AddScoped(_ => _mockUserStoryRepository.Object);
        Services.AddScoped(_ => _mockUserStoryTaskService.Object);
        Services.AddScoped(_ => _mockUserStoryService.Object);
        
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
    }

    private void CreateComponent(bool isReadOnly=false)
    {
        CreateComponentUnderTest(isReadOnly: isReadOnly, extendParameterBuilder: parameters => parameters
            .Add(cut => cut.Story, _story)
            .AddCascadingValue("ModalCallback", new Action<RenderFragment>(fragment => _modalRenderFragmentContent = fragment))
            .Add(cut => cut.RefreshStory, () => _refreshStoryCalled = true));
    }

    [Fact]
    public void Rendered_IsNotReadOnly_EditButtonShown()
    {
        CreateComponent();
        ComponentUnderTest.FindAll("#edit-story-button").Should().ContainSingle();
    }
        
    [Fact]
    public void Rendered_IsReadOnly_EditButtonHidden()
    {
        CreateComponent(true);
        ComponentUnderTest.FindAll("#edit-story-button").Should().BeEmpty();
    }
        
    [Fact]
    public void Rendered_SprintInReview_EditButtonHidden()
    {
        _sprint.Stage = SprintStage.InReview;
        CreateComponent();
        ComponentUnderTest.FindAll("#edit-story-button").Should().BeEmpty();
    }

    [Theory]
    [InlineData(Stage.Deferred)]
    [InlineData(Stage.Done)]
    public void Rendered_StoryAlreadyDone_DeferButtonNotVisible(Stage stage)
    {
        _story.Stage = stage;
        CreateComponent();
        ComponentUnderTest.FindAll("#defer-story-button").Should().BeEmpty();
    }
    
    [Theory]
    [InlineData(Stage.Todo)]
    [InlineData(Stage.InProgress)]
    [InlineData(Stage.UnderReview)]
    public void Rendered_StoryNotAlreadyDone_DeferButtonVisible(Stage stage)
    {
        _story.Stage = stage;
        CreateComponent();
        ComponentUnderTest.FindAll("#defer-story-button").Should().ContainSingle();
    }

    [Theory]
    [InlineData(0, "tasks")]
    [InlineData(1, "task")]
    [InlineData(2, "tasks")]
    public void Rendered_DeferButtonClicked_ConfirmationModalShownWithCorrectText(int numTasks, string taskPlural)
    {
        _story.Tasks.AddRange(FakeDataGenerator.CreateMultipleFakeTasks(numTasks));
        CreateComponent();
        ComponentUnderTest.Find("#defer-story-button").Click();
        RenderedModal.Find("#confirm-deferral-modal-story-name").TextContent.Should().Be($"\"{_story.Name}\".");
        RenderedModal.Find("#confirm-deferral-modal-tasks-to-defer").TextContent.Should().Be($"This will move {numTasks} {taskPlural} to deferred.");
    }
    
    [Fact]
    public void ConfirmModalShowing_ConfirmButtonClicked_StoryIsDeferred()
    {
        CreateComponent();
        _refreshStoryCalled.Should().BeFalse();
        ComponentUnderTest.Find("#defer-story-button").Click();
        RenderedModal.Find("#confirm-modal-button").Click();
        _mockUserStoryService.Verify(x => x.DeferStory(ActingUser, _story), times: Times.Once);
        _mockUserStoryTaskService.Verify(x => x.DeferAllIncompleteTasksInStory(ActingUser, _story), times: Times.Once);
        _refreshStoryCalled.Should().BeTrue();
    }
    
    [Fact]
    public void ConfirmModalShowing_CancelButtonClicked_StoryIsNotDeferred()
    {
        CreateComponent();
        _refreshStoryCalled.Should().BeFalse();
        ComponentUnderTest.Find("#defer-story-button").Click();
        RenderedModal.Find("#cancel-modal-button").Click();
        _mockUserStoryService.Verify(x => x.DeferStory(ActingUser, _story), times: Times.Never);
        _mockUserStoryTaskService.Verify(x => x.DeferAllIncompleteTasksInStory(ActingUser, _story), times: Times.Never);
        _refreshStoryCalled.Should().BeFalse();
    }
    
    [Fact]
    public void ConfirmModalShowing_CloseButtonClicked_StoryIsNotDeferred()
    {
        CreateComponent();
        _refreshStoryCalled.Should().BeFalse();
        ComponentUnderTest.Find("#defer-story-button").Click();
        RenderedModal.Find("#close-modal-button").Click();
        _mockUserStoryService.Verify(x => x.DeferStory(ActingUser, _story), times: Times.Never);
        _mockUserStoryTaskService.Verify(x => x.DeferAllIncompleteTasksInStory(ActingUser, _story), times: Times.Never);
        _refreshStoryCalled.Should().BeFalse();
    }
}