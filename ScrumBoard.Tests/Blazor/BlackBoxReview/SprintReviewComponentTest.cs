using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared.BlackBoxReview;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.BlackBoxReview;

public class SprintReviewComponentTest : BaseProjectScopedComponentTestContext<SprintReview>
{
    private readonly Mock<ISprintService> _sprintServiceMock = new();
    private readonly Mock<IUserStoryService> _userStoryServiceMock = new();
    private readonly Mock<IAcceptanceCriteriaService> _acceptanceCriteriaServiceMock = new();
    private readonly Mock<IUserStoryChangelogRepository> _userStoryChangelogRepositoryMock = new();
    private readonly Mock<IUsageDataService> _usageDataServiceMock = new();

    private readonly Mock<SkipSprintReviewModal> _skipReviewModalMock = new();

    private ICollection<Sprint> _sprints;
    private IEnumerable<UserStory> _stories;

    private Func<IElement> GetSelectSprintLabel => () => ComponentUnderTest.Find("#select-sprint-label");
    private Func<IElement> GetCurrentViewingSprintDisplay => () => ComponentUnderTest.Find("#current-viewing-sprint-display");

    private Func<IElement> GetSelectButtonForSprint(IId sprint) => () => ComponentUnderTest.Find($"#sprint-select-{sprint.Id}");
    private Func<IElement> GetBadgeForCurrentViewingSprint => () => ComponentUnderTest.Find("#current-viewing-sprint-display .badge");
    
    private Func<IElement> GetStartReviewButton => () => ComponentUnderTest.Find("#start-sprint-review");
    private Func<IElement> GetSkipReviewButton => () => ComponentUnderTest.Find("#skip-sprint-review");

    private Func<IElement> GetGeneralErrorLabel => () => ComponentUnderTest.Find("#general-error-message");
    private Func<IElement> GetNoViewingSprintErrorLabel => () => ComponentUnderTest.Find("#no-viewing-sprint-error-message");
    private Func<IElement> GetSprintStillInProgressErrorLabel => () => ComponentUnderTest.Find("#sprint-still-in-progress-error-message");
    
    private Func<IRenderedComponent<InProgressSprintReview>> GetInProgressSprintReview => () => ComponentUnderTest.FindComponent<InProgressSprintReview>();
    private Func<IRenderedComponent<StoryInReview>> GetReadonlyStoryInReview(IId story) => () => ComponentUnderTest.FindComponents<StoryInReview>()
        .First(x => x.FindAll($"#readonly-story-{story.Id}").Any());
    
    private void CreateComponent(
        SprintStage currentSprintStage = SprintStage.Reviewed,
        ICollection<Sprint> overrideSprints = null,
        int numberOfStoriesInCurrentSprint = 5,
        bool updatingSprintStageSucceeds = true,
        bool storiesHaveReviewComments = false,
        ProjectRole actingUserRole = ProjectRole.Developer
    ) {
        Services.AddScoped(_ => _sprintServiceMock.Object);
        Services.AddScoped(_ => _userStoryServiceMock.Object);
        Services.AddScoped(_ => _acceptanceCriteriaServiceMock.Object);
        Services.AddScoped(_ => _userStoryChangelogRepositoryMock.Object);
        Services.AddScoped(_ => _usageDataServiceMock.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        _sprints = overrideSprints ?? new[] { FakeDataGenerator.CreateFakeSprint(CurrentProject, stage: currentSprintStage) };
        CurrentProject.Sprints = _sprints.ToList();

        if (_sprints.Count != 0)
        {
            _sprintServiceMock.Setup(x => x.UpdateStage(It.IsAny<User>(), It.IsAny<Sprint>(), It.IsAny<SprintStage>()))
                .ReturnsAsync(updatingSprintStageSucceeds);
            _sprintServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => _sprints.First(x => x.Id == id));
            
            _stories = FakeDataGenerator.CreateMultipleFakeStories(
                numberOfStoriesInCurrentSprint, 
                _sprints.First(), 
                3,
                generateReviewComments: storiesHaveReviewComments
            ).ToList();
            
            _userStoryServiceMock.Setup(x => x.GetStoriesForSprintReviewAsync(It.IsAny<long>()))
                .ReturnsAsync(_stories.ToList());
            _userStoryServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => _stories.First(x => x.Id == id));
        }

        ComponentFactories.AddMockComponent(_skipReviewModalMock);
        
        CreateComponentUnderTest(actingUserRoleInProject: actingUserRole);
    }
    
    [Theory]
    [InlineData(SprintStage.Created)]
    [InlineData(SprintStage.Started)]
    [InlineData(SprintStage.Closed)]
    public void PageLoaded_NoSprintInReviewableState_SelectSprintTextShown(SprintStage nonReviewSprintStage)
    {
        CreateComponent(currentSprintStage: nonReviewSprintStage);
        GetSelectSprintLabel().TextContent.Trim().Should().Be("Select Sprint");
        GetNoViewingSprintErrorLabel().TextContent.Trim().Should().Be("Select a sprint to review");
        GetCurrentViewingSprintDisplay.Should().Throw<ElementNotFoundException>();
    }
    
    [Theory]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    public void PageLoaded_CurrentSprintInReviewableState_CurrentSprintShownInSelector(SprintStage reviewableSprintStage)
    {
        CreateComponent(currentSprintStage: reviewableSprintStage);
        GetSelectSprintLabel.Should().Throw<ElementNotFoundException>();
        GetNoViewingSprintErrorLabel.Should().Throw<ElementNotFoundException>();
        GetCurrentViewingSprintDisplay().TextContent.Should().Contain(_sprints.First().Name);
    }
        
    [Fact]
    public void PageLoaded_SprintReadyToReview_StartSprintReviewButtonShown()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        GetStartReviewButton().TextContent.Trim().Should().Be("Start Review");
    }

    [Fact]
    public void PageLoaded_ParametersSetAgain_SprintOnlyRefreshedOnce()
    {
        CreateComponent();
        ProjectRepositoryMock.Verify(x => x.GetByIdAsync(
            CurrentProject.Id, 
            It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()
            ), Times.Once);
        ComponentUnderTest.SetParametersAndRender();
        ProjectRepositoryMock.Verify(x => x.GetByIdAsync(
            CurrentProject.Id, 
            It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()
        ), Times.Once);
    }

    [Theory]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    public void PageLoaded_SprintIsInReviewableState_ReviewSummaryShown(SprintStage nonInProgressSprintStage)
    {
        CreateComponent(currentSprintStage: nonInProgressSprintStage, storiesHaveReviewComments: true);
        foreach (var story in _stories)
        {
            var storyElement = GetReadonlyStoryInReview(story)();
            storyElement.Find("#review-comments-display").TextContent.Trim().Should().Be(story.ReviewComments);
        }
    }

    [Fact]
    public void SprintSelected_SprintIsClosed_ReviewSummaryShown()
    {
        CreateComponent(currentSprintStage: SprintStage.Closed, storiesHaveReviewComments: true);
        GetSelectButtonForSprint(_sprints.First())().Click();
        foreach (var story in _stories)
        {
            var storyElement = GetReadonlyStoryInReview(story)();
            storyElement.Find("#review-comments-display").TextContent.Trim().Should().Be(story.ReviewComments);
        }
    }

    [Theory]
    [InlineData(SprintStage.Created)]
    [InlineData(SprintStage.Started)]
    public void SprintSelected_SprintIsInProgress_ErrorMessageShown(SprintStage inProgressSprintStage)
    {
        CreateComponent(currentSprintStage: inProgressSprintStage);
        GetSelectButtonForSprint(_sprints.First())().Click();
        GetSprintStillInProgressErrorLabel().TextContent.Trim().Should().Be($"This sprint is still in progress. End date: {_sprints.First().EndDate}");
    }
        
    [Fact]
    public void StartSprintReviewButtonClicked_ValidState_SprintUpdatedAndProjectRefreshed()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        _sprintServiceMock.Invocations.Clear();
        ProjectRepositoryMock.Invocations.Clear();
        
        GetStartReviewButton().Click();
        
        _sprintServiceMock.Verify(
            x => x.UpdateStage(ActingUser, _sprints.First(), SprintStage.InReview),
            times: Times.Once
        );
        ProjectRepositoryMock.Verify(
            x => x.GetByIdAsync(CurrentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()),
            times: Times.Once
        );
    }
    
    [Fact]
    public void StartSprintReviewButtonClicked_ValidState_InProgressSprintReviewComponentShown()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        GetInProgressSprintReview.Should().Throw<ComponentNotFoundException>();
        GetStartReviewButton().Click();
        GetInProgressSprintReview.Should().NotThrow();
    }
        
    [Fact]
    public void StartSprintReviewButtonClicked_NoStoriesInSprint_ErrorMessageShown()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview, numberOfStoriesInCurrentSprint: 0);
        GetStartReviewButton().Click();
        GetGeneralErrorLabel().TextContent.Should().Be("There are no stories to review");
    }
    
    [Fact]
    public void StartSprintReviewButtonClicked_NoStoriesInSprint_SprintReviewNotStarted()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview, numberOfStoriesInCurrentSprint: 0);
        GetStartReviewButton().Click();
        GetInProgressSprintReview.Should().Throw<ComponentNotFoundException>();
    }

    [Fact]
    public void StartSprintReviewButtonClicked_SprintWasUpdatedElsewhereSincePageLoad_ErrorMessageShown()
    { 
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview, updatingSprintStageSucceeds: false);
        GetStartReviewButton().Click();
        GetGeneralErrorLabel().TextContent.Should().Be("Sprint was already updated elsewhere. Page has been refreshed.");
    }
    
    [Fact]
    public void StartSprintReviewButtonClicked_SprintWasUpdatedElsewhereSincePageLoad_SprintReviewNotStarted()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview, updatingSprintStageSucceeds: false);
        GetStartReviewButton().Click();
        GetInProgressSprintReview.Should().Throw<ComponentNotFoundException>();
    }
    
    [Fact]
    public void PageLoadedWhenSprintReadyToReview_ReviewerRole_SkipReviewNotShown()
    {
        CreateComponent(actingUserRole: ProjectRole.Reviewer, currentSprintStage: SprintStage.ReadyToReview);
        GetSkipReviewButton.Should().Throw<ElementNotFoundException>();
    }
        
    [Fact]
    public void PageLoadedWhenSprintReadyToReview_LeaderRole_SkipReviewShown()
    {
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        GetSkipReviewButton.Should().NotThrow();
    }

    [Fact]
    public void SkipReviewClicked_LeaderRole_SkipReviewModalShown()
    {
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        GetSkipReviewButton().Click();
        _skipReviewModalMock.Verify(x => x.Show(_sprints.First()), Times.Once);
    }
        
    [Fact]
    public void SkipReviewClicked_ModalReturnsCancelResult_ServiceLayerNotCalled()
    {
        _skipReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(true);
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        GetSkipReviewButton().Click();
        _sprintServiceMock.Verify(
            x => x.UpdateStage(It.IsAny<User>(), It.IsAny<Sprint>(), It.IsAny<SprintStage>()),
            Times.Never
        );
    }
        
    [Fact]
    public void SkipReviewClicked_ModalReturnsSkipResult_ServiceLayerCalledCorrectly()
    {
        _skipReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(false);
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        GetSkipReviewButton().Click();
        _sprintServiceMock.Verify(
            x => x.UpdateStage(ActingUser, _sprints.First(), SprintStage.Closed), 
            Times.Once
        );
    }
    
    [Fact]
    public void SkipReviewClicked_ModalReturnsSkipResult_SprintRefreshed()
    {
        _skipReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(false);
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        ProjectRepositoryMock.Invocations.Clear();
        
        GetSkipReviewButton().Click();
        
        ProjectRepositoryMock.Verify(
            x => x.GetByIdAsync(CurrentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()), 
            Times.Once
        );
    }
    
    [Fact]
    public void SkipReviewClicked_SprintStageUpdated_CurrentViewingSprintDisplayIsEmpty()
    {
        _skipReviewModalMock.Setup(x => x.Show(It.IsAny<Sprint>())).ReturnsAsync(false);
        CreateComponent(actingUserRole: ProjectRole.Leader, currentSprintStage: SprintStage.ReadyToReview);
        GetBadgeForCurrentViewingSprint().TextContent.Should().Be("Preparing For Review");
        
        ProjectRepositoryMock
            .Setup(x => x.GetByIdAsync(CurrentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(new Project { Sprints = [new Sprint { Stage = SprintStage.Closed}]});
        GetSkipReviewButton().Click();
        
        GetBadgeForCurrentViewingSprint.Should().Throw<ElementNotFoundException>();
        GetSelectSprintLabel().TextContent.Trim().Should().Be("Select Sprint");
    }
    
    [Theory]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Leader)]
    public void PageLoaded_DeveloperOrLeaderRole_AllSprintsShown(ProjectRole role)
    {
        var sprints = new List<Sprint>([
            FakeDataGenerator.CreateFakeSprint(CurrentProject, stage: SprintStage.Closed), 
            FakeDataGenerator.CreateFakeSprint(CurrentProject, stage: SprintStage.InReview)
        ]);
        CreateComponent(actingUserRole: role, overrideSprints: sprints);
        
        foreach (var sprint in sprints)
        {
            GetSelectButtonForSprint(sprint).Should().NotThrow();
        }
    }
    
    [Theory]
    [InlineData(SprintStage.InReview, true)]
    [InlineData(SprintStage.Reviewed, true)]
    [InlineData(SprintStage.ReadyToReview, true)]
    [InlineData(SprintStage.Started, false)]
    [InlineData(SprintStage.Created, false)]
    [InlineData(SprintStage.Closed, false)]
    public void PageLoaded_ReviewerRole_OnlyCurrentReviewingSprintShown(SprintStage sprintStage, bool shouldSeeSprint)
    {
        var sprint = FakeDataGenerator.CreateFakeSprint(CurrentProject, stage: sprintStage);
        CreateComponent(actingUserRole: ProjectRole.Reviewer, overrideSprints: [sprint]);

        if (shouldSeeSprint)
        {
            GetSelectButtonForSprint(sprint).Should().NotThrow();
        }
        else
        {
            GetSelectButtonForSprint(sprint).Should().Throw<ElementNotFoundException>();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SprintReviewFinished_SuccessfullyOrUnsuccessfully_SprintRefreshed(bool isSuccess)
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        GetStartReviewButton().Click();
        ProjectRepositoryMock.Invocations.Clear();
        
        await ComponentUnderTest.InvokeAsync(() => GetInProgressSprintReview().Instance.OnFinished.InvokeAsync(isSuccess));

        ProjectRepositoryMock.Verify(
            x => x.GetByIdAsync(CurrentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()), 
            Times.Once
        );
    }
    
    [Fact]
    public async Task SprintReviewFinished_Successfully_ReadonlyStoriesShown()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        GetStartReviewButton().Click();
        GetReadonlyStoryInReview(_stories.First()).Should().Throw<InvalidOperationException>();
        
        await ComponentUnderTest.InvokeAsync(() => GetInProgressSprintReview().Instance.OnFinished.InvokeAsync(true));

        GetReadonlyStoryInReview(_stories.First()).Should().NotThrow();
    }
    
        
    [Fact]
    public async Task SprintReviewFinished_Unsuccessfully_ErrorMessageShown()
    {
        CreateComponent(currentSprintStage: SprintStage.ReadyToReview);
        GetStartReviewButton().Click();
        
        await ComponentUnderTest.InvokeAsync(() => GetInProgressSprintReview().Instance.OnFinished.InvokeAsync(false));

        GetGeneralErrorLabel().TextContent.Trim().Should().Be("Sprint had already been updated elsewhere. Page has been refreshed.");
    }
}