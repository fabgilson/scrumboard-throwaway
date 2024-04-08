using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.BlackBoxReview;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;

namespace ScrumBoard.Tests.Blazor.BlackBoxReview;

public class InProgressSprintReviewComponentTest : BaseProjectScopedComponentTestContext<InProgressSprintReview>
{
    private readonly User _differentUser;
    private readonly Sprint _sprint;
    private IEnumerable<UserStory> _storiesInSprint;

    private readonly Mock<ISprintService> _sprintServiceMock = new();
    private readonly Mock<IUserStoryService> _userStoryServiceMock = new();
    private readonly Mock<IUserStoryChangelogRepository> _userStoryChangelogEntryRepository = new();
    private readonly Mock<IAcceptanceCriteriaService> _acceptanceCriteriaServiceMock = new();

    private readonly Mock<SubmitSprintReviewModal> _submitSprintReviewModalMock = new();
    private readonly Mock<SprintReviewFinishedByAnotherUserModal> _sprintReviewFinishedByOtherUserModalMock = new();
    
    private readonly Mock<Action<bool>> _onFinishedMock = new();

    private IElement SubmitReviewButton => ComponentUnderTest.Find("#submit-sprint-review");
    
    private Func<IElement> GetNoSuchSprintErrorText => () => ComponentUnderTest.Find("#no-such-sprint-error-text");

    private static Func<IRenderedComponent<StoryInReview>, Func<IElement>> GetNextButtonForStory => storyInReview => () => storyInReview.Find("#next-story-button");
    private static Func<IRenderedComponent<StoryInReview>, Func<IElement>> GetPreviousButtonForStory => storyInReview => () => storyInReview.Find("#previous-story-button");
    private static Func<IRenderedComponent<StoryInReview>, Func<IElement>> GetFinishButtonForStory => storyInReview => () => storyInReview.Find("#finish-review-button");
    
    private Func<int, IRenderedComponent<StoryInReview>> GetNthStoryInReview => index => ComponentUnderTest.FindComponents<StoryInReview>()[index];
    private Func<int, IElement> GetNthStoryInReviewContainer => index => ComponentUnderTest.Find($"#story-in-review-container-{index}");
    
    public InProgressSprintReviewComponentTest()
    {
        _sprint = FakeDataGenerator.CreateFakeSprint(CurrentProject);
        _differentUser = FakeDataGenerator.CreateFakeUser();
    }

    private void CreateComponent(
        bool returnNullSprint=false, 
        IEnumerable<UserStory> storiesInSprint=null, 
        bool waitForStoriesToDisplay=true,
        long? startFromStoryId=null
    ) {
        _storiesInSprint = storiesInSprint ?? new List<UserStory>();
        
        _sprintServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(returnNullSprint ? null : _sprint);
        
        _userStoryServiceMock.Setup(x => x.GetBySprintIdAsync(_sprint.Id))
            .ReturnsAsync(_storiesInSprint);
        _userStoryServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .Returns<long>(id => Task.FromResult(_storiesInSprint.First(x => x.Id == id)));

        UserRepositoryMock.Setup(x => x.GetByIdAsync(_differentUser.Id)).ReturnsAsync(_differentUser);
        
        Services.AddScoped(_ => _sprintServiceMock.Object);
        Services.AddScoped(_ => _userStoryServiceMock.Object);
        Services.AddScoped(_ => _userStoryChangelogEntryRepository.Object);
        Services.AddScoped(_ => _acceptanceCriteriaServiceMock.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        ComponentFactories.AddMockComponent(_submitSprintReviewModalMock);
        ComponentFactories.AddMockComponent(_sprintReviewFinishedByOtherUserModalMock);
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.SprintId, returnNullSprint ? 12345 : _sprint.Id)
            .Add(x => x.StartFromStoryId, startFromStoryId)
            .Add(x => x.OnFinished, _onFinishedMock.Object));
        
        if(!waitForStoriesToDisplay) return;
        ComponentUnderTest.WaitForElement("[id^=\"story-in-review-\"]");
    }

    [Fact]
    public void Rendered_SprintDoesNotExist_ErrorMessageShown()
    {
        CreateComponent(returnNullSprint: true, waitForStoriesToDisplay: false);
        GetNoSuchSprintErrorText.Should().NotThrow();
        GetNoSuchSprintErrorText().TextContent.Should().Be("Error: could not find the specified sprint");
    }

    [Fact]
    public void Rendered_SprintDoesExist_ErrorMessageNotShown()
    {
        CreateComponent(waitForStoriesToDisplay: false);
        GetNoSuchSprintErrorText.Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    public void Rendered_OnFirstPageOfMany_OnlyNextButtonShown()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3);
        CreateComponent(storiesInSprint: stories.ToList());

        var firstStory = GetNthStoryInReview(0);

        GetNextButtonForStory(firstStory).Should().NotThrow();
        GetPreviousButtonForStory(firstStory).Should().Throw<ElementNotFoundException>();
        GetFinishButtonForStory(firstStory).Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public void Rendered_OnLastPageOfMany_PreviousAndFinishButtonShown()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3);
        CreateComponent(storiesInSprint: stories.ToList());

        var lastStory = GetNthStoryInReview(2);

        GetNextButtonForStory(lastStory).Should().Throw<ElementNotFoundException>();
        GetPreviousButtonForStory(lastStory).Should().NotThrow();
        GetFinishButtonForStory(lastStory).Should().NotThrow();
    }
    
    [Fact]
    public void Rendered_OnFirstPageAndOnlyPage_OnlyFinishButtonShown()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(1, _sprint, acsPerStory: 3);
        CreateComponent(storiesInSprint: stories.ToList());

        var lastStory = GetNthStoryInReview(0);

        GetNextButtonForStory(lastStory).Should().Throw<ElementNotFoundException>();
        GetPreviousButtonForStory(lastStory).Should().Throw<ElementNotFoundException>();
        GetFinishButtonForStory(lastStory).Should().NotThrow();
    }

    [Fact]
    public void Rendered_OnSecondPageOfMany_PreviousAndNextButtonsShown()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3);
        CreateComponent(storiesInSprint: stories.ToList());

        var lastStory = GetNthStoryInReview(1);

        GetNextButtonForStory(lastStory).Should().NotThrow();
        GetPreviousButtonForStory(lastStory).Should().NotThrow();
        GetFinishButtonForStory(lastStory).Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    public void Rendered_ClickNext_MovesToNextPage()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3);
        CreateComponent(storiesInSprint: stories.ToList());

        // Verify that first story is visible, but second is not
        GetNthStoryInReviewContainer(0).Attributes["style"]?.TextContent.Should().Contain("display: block");
        GetNthStoryInReviewContainer(1).Attributes["style"]?.TextContent.Should().Contain("display: none");
        
        GetNextButtonForStory(GetNthStoryInReview(0))().Click();
        
        // Verify that first story now not visible, but second is
        GetNthStoryInReviewContainer(0).Attributes["style"]?.TextContent.Should().Contain("display: none");
        GetNthStoryInReviewContainer(1).Attributes["style"]?.TextContent.Should().Contain("display: block");
    }
        
    [Fact]
    public void Rendered_ClickPrevious_MovesToPreviousPage()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3).ToList();
        CreateComponent(storiesInSprint: stories.ToList(), startFromStoryId: stories[1].Id);

        // Verify that second story is visible, but first is not
        GetNthStoryInReviewContainer(0).Attributes["style"]?.TextContent.Should().Contain("display: none");
        GetNthStoryInReviewContainer(1).Attributes["style"]?.TextContent.Should().Contain("display: block");
        
        GetPreviousButtonForStory(GetNthStoryInReview(1))().Click();
        
        // Verify that first story now visible, but second is not
        GetNthStoryInReviewContainer(0).Attributes["style"]?.TextContent.Should().Contain("display: block");
        GetNthStoryInReviewContainer(1).Attributes["style"]?.TextContent.Should().Contain("display: none");
    }

    private List<UserStory> CreateStoriesWithAllValidAcs()
    {
        var stories = FakeDataGenerator.CreateMultipleFakeStories(3, _sprint, acsPerStory: 3).ToList();
        foreach (var ac in stories.SelectMany(x => x.AcceptanceCriterias))
        {
            ac.Status = AcceptanceCriteriaStatus.Pass;
        }

        return stories;
    }

    [Fact]
    public void FinishReviewButtonClicked_NotAllStoriesValid_ShowsFirstInvalidStory()
    {
        var stories = CreateStoriesWithAllValidAcs();
        stories[1].AcceptanceCriterias.First().Status = AcceptanceCriteriaStatus.Fail;
        CreateComponent(storiesInSprint: stories);
        
        SubmitReviewButton.Click();
        
        // Verify that only second story is visible
        GetNthStoryInReviewContainer(0).Attributes["style"]?.TextContent.Should().Contain("display: none");
        GetNthStoryInReviewContainer(1).Attributes["style"]?.TextContent.Should().Contain("display: block");
        GetNthStoryInReviewContainer(2).Attributes["style"]?.TextContent.Should().Contain("display: none");
    }
    
    [Fact]
    public void FinishReviewButtonClicked_NotAllStoriesValid_SprintReviewModalNotShown()
    {
        var stories = CreateStoriesWithAllValidAcs();
        stories[1].AcceptanceCriterias.First().Status = AcceptanceCriteriaStatus.Fail;
        CreateComponent(storiesInSprint: stories);
        
        SubmitReviewButton.Click();
        
        _submitSprintReviewModalMock.Verify(x => x.Show(It.IsAny<Sprint>()), Times.Never);
    }

    [Fact]
    public void FinishReviewButtonClicked_AllStoriesValid_SprintReviewModalShown()
    {
        var stories = CreateStoriesWithAllValidAcs();
        CreateComponent(storiesInSprint: stories);
        
        SubmitReviewButton.Click();
        
        _submitSprintReviewModalMock.Verify(x => x.Show(It.Is<Sprint>(sprint => sprint.Id == _sprint.Id)), Times.Once);
    }

    [Fact]
    public void FinishReviewButtonClicked_ModalConfirmed_ServiceLayerCalled()
    {
        _submitSprintReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(false);
        var stories = CreateStoriesWithAllValidAcs();
        CreateComponent(storiesInSprint: stories);
        
        SubmitReviewButton.Click();

        _sprintServiceMock.Verify(x => x.UpdateStage(ActingUser, _sprint, SprintStage.Reviewed), Times.Once);
    }
    
    [Fact]
    public void FinishReviewButtonClicked_ModalConfirmed_OnFinishedInvoked()
    {
        _submitSprintReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(false);
        var stories = CreateStoriesWithAllValidAcs();
        CreateComponent(storiesInSprint: stories);
        
        SubmitReviewButton.Click();

        _onFinishedMock.Verify(x => x(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void Rendered_ParametersSet_SingleEntityUpdateHandlerRegistered()
    {
        CreateComponent(waitForStoriesToDisplay: false);

        var updateHandlers = GetLiveUpdateEventsForEntity<Sprint>(_sprint.Id, LiveUpdateEventType.EntityUpdated);

        updateHandlers.Should().ContainSingle();
    }

    [Fact]
    public async Task OtherUserUpdatesSprintStage_StageSetToReviewing_ReviewFinishedWhileEditingModalNotShown()
    {
        CreateComponent(waitForStoriesToDisplay: false);

        var updateHandler = GetMostRecentLiveUpdateHandlerForEntity<Sprint>(_sprint.Id, LiveUpdateEventType.EntityUpdated);
        _sprint.Stage = SprintStage.InReview;
        await ComponentUnderTest.InvokeAsync(() => updateHandler.GetTypedEntityUpdateHandler<Sprint>().Invoke(_sprint, _differentUser.Id));
        
        _sprintReviewFinishedByOtherUserModalMock.Verify(x => x.Show(It.IsAny<User>()), Times.Never);
    }
    
    [Theory]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public async Task OtherUserUpdatesSprintStage_StageSetToReviewedOrClosed_ReviewFinishedWhileEditingModalShown(SprintStage stage)
    {
        _sprint.Stage = SprintStage.InReview;
        CreateComponent(waitForStoriesToDisplay: false);

        var updateHandler = GetMostRecentLiveUpdateHandlerForEntity<Sprint>(_sprint.Id, LiveUpdateEventType.EntityUpdated);
        var newSprint = new Sprint { Stage = stage };
        await ComponentUnderTest.InvokeAsync(() => updateHandler.GetTypedEntityUpdateHandler<Sprint>().Invoke(newSprint, _differentUser.Id));
        
        _sprintReviewFinishedByOtherUserModalMock.Verify(x => x.Show(_differentUser), Times.Once);
    }
    
    [Theory]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public async Task OtherUserUpdatesSprintStage_CurrentUserChoosesToContinueEditing_SprintStageSetBackToInReview(SprintStage stage)
    {
        _sprint.Stage = SprintStage.InReview;
        _sprintReviewFinishedByOtherUserModalMock.Setup(x => x.Show(It.IsAny<User>())).ReturnsAsync(true);
        CreateComponent(waitForStoriesToDisplay: false);

        var updateHandler = GetMostRecentLiveUpdateHandlerForEntity<Sprint>(_sprint.Id, LiveUpdateEventType.EntityUpdated);
        var newSprint = new Sprint { Stage = stage };
        await ComponentUnderTest.InvokeAsync(() => updateHandler.GetTypedEntityUpdateHandler<Sprint>().Invoke(newSprint, _differentUser.Id));
        
        _sprintServiceMock.Verify(x => x.UpdateStage(ActingUser, _sprint, SprintStage.InReview), Times.Once);
    }
    
    [Theory]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public async Task OtherUserUpdatesSprintStage_CurrentUserChoosesNotToContinueEditing_UserTakenBackToReviewSummaryPage(SprintStage stage)
    {
        _sprint.Stage = SprintStage.InReview;
        _sprintReviewFinishedByOtherUserModalMock.Setup(x => x.Show(It.IsAny<User>())).ReturnsAsync(false);
        CreateComponent(waitForStoriesToDisplay: false);

        var updateHandler = GetMostRecentLiveUpdateHandlerForEntity<Sprint>(_sprint.Id, LiveUpdateEventType.EntityUpdated);
        var newSprint = new Sprint { Stage = stage };
        await ComponentUnderTest.InvokeAsync(() => updateHandler.GetTypedEntityUpdateHandler<Sprint>().Invoke(newSprint, _differentUser.Id));
        
        _sprintServiceMock.Verify(x => x.UpdateStage(ActingUser, _sprint, SprintStage.InReview), Times.Never);

        var lastNavEvent = FakeNavigationManager.History.Last();
        lastNavEvent.Options.ForceLoad.Should().BeTrue();
        lastNavEvent.Uri.Should().Be(PageRoutes.ToProjectReview(CurrentProject.Id));
    }
}