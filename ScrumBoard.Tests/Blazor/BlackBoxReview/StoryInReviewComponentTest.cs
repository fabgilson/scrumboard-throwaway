using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bogus;
using Bunit;
using EnumsNET;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared.BlackBoxReview;
using ScrumBoard.Shared.Widgets.SaveStatus;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using ScrumBoard.Utils;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor.BlackBoxReview;

public class StoryInReviewComponentTest : BaseProjectScopedComponentTestContext<StoryInReview>
{
    private readonly Sprint _sprint;
    private readonly UserStory _story;
    private readonly AcceptanceCriteria _blankAc, _passingAcWithoutComment, _failingAcWithComment;

    private readonly Mock<Action> _onSuccessfulSubmitCallbackMock = new();
    private readonly Mock<Action> _onEditCallback = new();

    private readonly Mock<IUserStoryChangelogRepository> _userStoryChangelogRepositoryMock = new();
    private readonly Mock<ISprintService> _sprintServiceMock = new();
    private readonly Mock<IUserStoryService> _userStoryServiceMock = new();
    private readonly Mock<IAcceptanceCriteriaService> _acceptanceCriteriaServiceMock = new();

    private IElement ReviewCommentsReadonlyDisplay => ComponentUnderTest.Find("#review-comments-display");
    private Func<IElement> WaitForReviewCommentsErrorLabel => 
        () => ComponentUnderTest.WaitForElement("#story-comments-validation", TimeSpan.FromSeconds(5));

    private Func<IElement> GetSaveIndicatorContainer => () => ComponentUnderTest.Find("#story-in-review-save-status-indicator");
    private Func<IElement> WaitForSaveStatusIndicatorComponent(FormSaveStatus status) => 
        () => ComponentUnderTest.WaitForElement($"#save-status-indicator-{status.GetName()}-display", TimeSpan.FromSeconds(5)); 
    
    private IElement ReviewCommentsInputForStory => ComponentUnderTest.Find("#story-comments");
    private Func<IElement> GetCommentsValidationLabelForStory => () => ComponentUnderTest.Find("#story-comments-validation");

    private IElement FailButtonForAcWithId(long acId) => ComponentUnderTest.Find($"#btn-fail-{acId}");
    private IElement ReviewCommentsInputForAcWithIc(long acId) => ComponentUnderTest.Find($"#comments-{acId}");

    private Func<IElement> GetChangelogToggleButton => () => ComponentUnderTest.Find("#toggle-changelog");
    private IElement ChangelogContainer => ComponentUnderTest.Find("#review-changelog");

    private Func<IElement> GetEditButton => () => ComponentUnderTest.Find("#start-editing-button");

    private Func<IElement> GetStatusValidationLabelForAcWithId(long acId) => () => ComponentUnderTest.Find($"#status-validation-{acId}");
    private Func<IElement> GetCommentsValidationLabelForAcWithId(long acId) => () => ComponentUnderTest.Find($"#comments-validation-{acId}");

    public StoryInReviewComponentTest()
    {
        _sprint = FakeDataGenerator.CreateFakeSprint(CurrentProject);
        _story = FakeDataGenerator.CreateFakeUserStory(_sprint);
        _blankAc = FakeDataGenerator.CreateAcceptanceCriteria(_story, null, "");
        _passingAcWithoutComment = FakeDataGenerator.CreateAcceptanceCriteria(_story, AcceptanceCriteriaStatus.Pass, "");
        _failingAcWithComment = FakeDataGenerator.CreateAcceptanceCriteria(_story, AcceptanceCriteriaStatus.Fail, new Faker().Lorem.Sentence());
        _story.AcceptanceCriterias = new[] { _blankAc, _passingAcWithoutComment, _failingAcWithComment };

        Services.AddScoped(_ => _userStoryChangelogRepositoryMock.Object);
        Services.AddScoped(_ => _sprintServiceMock.Object);
        Services.AddScoped(_ => _userStoryServiceMock.Object);
        Services.AddScoped(_ => _acceptanceCriteriaServiceMock.Object);
    }

    private async Task ValidateComponentAsync()
    {
        await ComponentUnderTest.InvokeAsync(() => ComponentUnderTest.Instance.Validate());
    }

    private void CreateComponent(
        IEnumerable<UserStoryChangelogEntry> changelogEntries = null,
        bool isDisabled = false,
        SprintStage sprintStage = SprintStage.InReview,
        ProjectRole projectRole = ProjectRole.Developer,
        string initialReviewComments = null
    ) {
        _story.ReviewComments = initialReviewComments ?? _story.ReviewComments;
        _sprint.Stage = sprintStage;
        _userStoryServiceMock.Setup(x => x.GetByIdAsync(_story.Id)).ReturnsAsync(_story);
        _sprintServiceMock.Setup(x => x.GetByIdAsync(_sprint.Id)).ReturnsAsync(_sprint);

        _userStoryChangelogRepositoryMock
            .Setup(mock => mock.GetByUserStoryAsync(_story, UserStoryChangelogIncludes.Display))
            .ReturnsAsync(changelogEntries?.ToList() ?? new List<UserStoryChangelogEntry>());

        CreateComponentUnderTest(actingUserRoleInProject: projectRole, extendParameterBuilder: parameters => parameters
            .Add(x => x.StoryId, _story.Id)
            .Add(x => x.OnSuccessfulSubmit, _onSuccessfulSubmitCallbackMock.Object)
            .Add(x => x.Disabled, isDisabled)
            .Add(x => x.OnEdit, _onEditCallback.Object)
        );
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_NoStatusSelected_MessageRequiredShown()
    {
        CreateComponent();

        await ValidateComponentAsync();

        var expectedErrorText = typeof(AcceptanceCriteriaReviewForm).GetErrorMessage<RequiredAttribute>(nameof(AcceptanceCriteriaReviewForm.Status));
        GetStatusValidationLabelForAcWithId(_blankAc.Id)().TextContent.Should().Be(expectedErrorText);
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_FailSelectedAndCommentEmpty_MessageRequiredShown()
    {
        CreateComponent();
        FailButtonForAcWithId(_blankAc.Id).Click();

        await ValidateComponentAsync();

        GetCommentsValidationLabelForAcWithId(_blankAc.Id)().TextContent.Should().Be("Must provide reason why acceptance criteria failed");
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_FailSelectedAndCommentGiven_NoValidationErrorsShown()
    {
        CreateComponent();

        await ValidateComponentAsync();

        GetCommentsValidationLabelForAcWithId(_failingAcWithComment.Id).Should().Throw<ElementNotFoundException>();
        GetStatusValidationLabelForAcWithId(_failingAcWithComment.Id).Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_PassSelected_NoValidationErrorsShown()
    {
        CreateComponent();

        await ValidateComponentAsync();

        GetCommentsValidationLabelForAcWithId(_passingAcWithoutComment.Id).Should().Throw<ElementNotFoundException>();
        GetStatusValidationLabelForAcWithId(_passingAcWithoutComment.Id).Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_ReviewCommentTooLong_ErrorMessageShown()
    {
        CreateComponent();
        var attribute = typeof(AcceptanceCriteriaReviewForm).GetAttribute<MaxLengthAttribute>(nameof(AcceptanceCriteriaReviewForm.ReviewComments));
        ReviewCommentsInputForAcWithIc(_blankAc.Id).Input(new Faker().Random.String(attribute.Length + 1));

        await ValidateComponentAsync();

        GetCommentsValidationLabelForAcWithId(_blankAc.Id)().TextContent.Should().Be(attribute.ErrorMessage);
    }

    [Fact]
    public async Task AcceptanceCriteriaValidated_ReviewCommentOnlyNumbersAndSpecialCharacters_ErrorMessageShown()
    {
        CreateComponent();
        var attribute =
            typeof(AcceptanceCriteriaReviewForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(
                nameof(AcceptanceCriteriaReviewForm.ReviewComments));
        ReviewCommentsInputForAcWithIc(_blankAc.Id).Input("0123456789!@#$%^&*()_+");

        await ValidateComponentAsync();

        GetCommentsValidationLabelForAcWithId(_blankAc.Id)().TextContent.Should().Be(attribute.ErrorMessage);
    }

    [Fact]
    public async Task StoryValidated_ReviewCommentTooLong_ErrorMessageShown()
    {
        CreateComponent();
        var attribute = typeof(StoryReviewForm).GetAttribute<MaxLengthAttribute>(nameof(StoryReviewForm.ReviewComments));
        ReviewCommentsInputForStory.Input(new Faker().Random.String(attribute.Length + 1));

        await ValidateComponentAsync();

        GetCommentsValidationLabelForStory().TextContent.Should().Be(attribute.ErrorMessage);
    }

    [Fact]
    public async Task StoryValidated_ReviewCommentOnlyNumbersAndSpecialCharacters_ErrorMessageShown()
    {
        CreateComponent();
        var attribute = typeof(StoryReviewForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(StoryReviewForm.ReviewComments));
        ReviewCommentsInputForStory.Input("0123456789!@#$%^&*()_+");

        await ValidateComponentAsync();

        GetCommentsValidationLabelForStory().TextContent.Should().Be(attribute.ErrorMessage);
    }

    [Fact]
    public void Rendered_ComponentIsDisabled_ChangelogButtonShown()
    {
        CreateComponent(isDisabled: true);
        GetChangelogToggleButton.Should().NotThrow();
    }

    [Fact]
    public void Rendered_ComponentIsNotDisabled_ChangelogButtonNotShown()
    {
        CreateComponent(isDisabled: false);
        GetChangelogToggleButton.Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public void ShowChangelogButtonClicked_NoChangelogs_ChangelogsRequestedFromServiceLayer()
    {
        CreateComponent(isDisabled: true);

        GetChangelogToggleButton().Click();

        _userStoryChangelogRepositoryMock.Verify(
            x => x.GetByUserStoryAsync(_story, UserStoryChangelogIncludes.Display),
            Times.Once
        );
    }

    [Fact]
    public void ShowChangelogButtonClicked_NoChangelogs_NoReviewChangesMessageShown()
    {
        CreateComponent(isDisabled: true);

        GetChangelogToggleButton().Click();

        ChangelogContainer.TextContent.Should().Contain("This story has no review changes");
    }

    [Fact]
    public void ShowChangelogButtonClicked_ChangelogExists_ChangelogShown()
    {
        CreateComponent(isDisabled: true, changelogEntries: new[]
        {
            new UserStoryChangelogEntry(
                ActingUser.Id,
                _story.Id,
                nameof(UserStory.ReviewComments),
                Change<object>.Update("Old comments", "New comments")
            ) { Creator = ActingUser }
        });

        GetChangelogToggleButton().Click();

        ChangelogContainer.TextContent.Should().NotContain("This story has no review changes");
        ChangelogContainer.TextContent.Should().Contain($"{ActingUser.GetFullName()} changed the story");
    }
    
    [Fact]
    public void ShowChangelogButtonClicked_ToggledOffAgain_ChangelogHidden()
    {
        CreateComponent(isDisabled: true);

        GetChangelogToggleButton().Click();
        GetChangelogToggleButton().Click();

        var action = () => ChangelogContainer;
        action.Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public void Rendered_NotDisabled_EditButtonNotShown()
    {
        CreateComponent(isDisabled: false);
        GetEditButton.Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public void Rendered_DisabledAndSprintInReview_EditButtonShown()
    {
        CreateComponent(isDisabled: true);
        GetEditButton.Should().NotThrow();
    }

    [Theory]
    [InlineData(ProjectRole.Guest, false)]
    [InlineData(ProjectRole.Developer, false)]
    [InlineData(ProjectRole.Reviewer, false)]
    [InlineData(ProjectRole.Leader, true)]
    public void Rendered_DisabledAndSprintReviewed_EditButtonShownForLeadersOnly(ProjectRole role, bool shouldBeShown)
    {
        CreateComponent(isDisabled: true, sprintStage: SprintStage.Reviewed, projectRole: role);
        if (shouldBeShown)
            GetEditButton.Should().NotThrow();
        else
            GetEditButton.Should().Throw<ElementNotFoundException>();
    }

    [Theory]
    [InlineData(ProjectRole.Guest, false)]
    [InlineData(ProjectRole.Developer, false)]
    [InlineData(ProjectRole.Reviewer, false)]
    [InlineData(ProjectRole.Leader, true)]
    public void Rendered_DisabledAndSprintClosed_EditButtonShownForLeadersOnly(ProjectRole role, bool shouldBeShown)
    {
        CreateComponent(isDisabled: true, sprintStage: SprintStage.Closed, projectRole: role);
        if (shouldBeShown)
            GetEditButton.Should().NotThrow();
        else
            GetEditButton.Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    public void EditButtonClicked_EditButtonVisible_OnEditCallbackInvoked()
    {
        CreateComponent(isDisabled: true);
        GetEditButton().Click();
        _onEditCallback.Verify(x => x());
    }

    [Fact]
    public void ReviewCommentsFocused_FocusIn_BroadcastSent()
    {
        CreateComponent();
        ReviewCommentsInputForStory.FocusIn();
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
    }

    [Fact]
    public void ReviewCommentsFocused_FocusOut_BroadcastSent()
    {
        CreateComponent();
        ReviewCommentsInputForStory.FocusOut();
        EntityUpdateHubMock.VerifyBroadcastUpdateEndedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
    }

    [Fact]
    public void ReviewCommentsInput_JustFocused_BroadcastNotSentAgain()
    {
        CreateComponent();
        ReviewCommentsInputForStory.FocusIn();
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
        ReviewCommentsInputForStory.Input("abc");
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
    }

    [Fact]
    public void ReviewCommentsInput_SingleInput_BroadcastSent()
    {
        CreateComponent();
        ReviewCommentsInputForStory.Input("abc");
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
    }

    [Fact]
    public void ReviewCommentsInput_MultipleInputs_BroadcastSentOnlyOnce()
    {
        CreateComponent();
        ReviewCommentsInputForStory.Input("abc");
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
        ReviewCommentsInputForStory.Input("abcdef");
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
        ReviewCommentsInputForStory.Input("abcdefghi");
        EntityUpdateHubMock.VerifyBroadcastUpdateStartedOnEntity<UserStory>(_story.Id, CurrentProject.Id, ActingUser.Id, Times.Once);
    }

    [Fact]
    public void ReviewCommentsInput_OnlyNumbersAndSpecialCharacters_ErrorMessageShown()
    {
        CreateComponent();
        ReviewCommentsInputForStory.Input("1234567890!@#$%^&");

        var expectedErrorMessage = typeof(StoryReviewForm)
            .GetErrorMessage<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(StoryReviewForm.ReviewComments));
        
        WaitForReviewCommentsErrorLabel().TextContent.Should().Be(expectedErrorMessage);
    }
    
    [Fact]
    public void ReviewCommentsInput_OnlyNumbersAndSpecialCharacters_UnsavedStatusShownAndServiceLayerNotCalled()
    {
        CreateComponent();
        ReviewCommentsInputForStory.Input("1234567890!@#$%^&");

        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Unsaved).Should().NotThrow();
        _userStoryServiceMock.Verify(acService => acService.SetReviewCommentsForIdAsync(
            _story.Id, 
            ActingUser.Id, 
            It.IsAny<string>(), 
            It.IsAny<Guid>()
        ), Times.Never);
    }
    
    [Fact]
    public void ReviewCommentsInput_OverMaxLength_UnsavedStatusShownAndServiceLayerNotCalled()
    {
        CreateComponent();
        var maxLengthAttribute = typeof(StoryReviewForm).GetAttribute<MaxLengthAttribute>(nameof(StoryReviewForm.ReviewComments));
        
        ReviewCommentsInputForStory.Input(new Faker().Random.String(maxLengthAttribute.Length+1));
        
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Unsaved).Should().NotThrow();
        _userStoryServiceMock.Verify(acService => acService.SetReviewCommentsForIdAsync(
            _story.Id, 
            ActingUser.Id, 
            It.IsAny<string>(), 
            It.IsAny<Guid>()
        ), Times.Never);
    }
    
    [Fact]
    public void ReviewCommentsInput_OverMaxLength_ErrorMessageShown()
    {
        CreateComponent();
        var maxLengthAttribute = typeof(StoryReviewForm).GetAttribute<MaxLengthAttribute>(nameof(StoryReviewForm.ReviewComments));
        
        ReviewCommentsInputForStory.Input(new Faker().Random.String(maxLengthAttribute.Length+1));
        
        WaitForReviewCommentsErrorLabel().TextContent.Should().Be(maxLengthAttribute.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Valid review comments")]
    public void ReviewCommentsInput_ValidData_ServiceLayerCalledAndSavingIndicatorsCorrectlyShown(string validReviewComments)
    {
        CreateComponent();
        GetSaveIndicatorContainer.Should().Throw<ElementNotFoundException>();
        
        ReviewCommentsInputForStory.Input(validReviewComments);
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saving).Should().NotThrow();
        
        ComponentUnderTest.WaitForAssertion(() => _userStoryServiceMock.Verify(acService =>
            acService.SetReviewCommentsForIdAsync(
                _story.Id, 
                ActingUser.Id, 
                validReviewComments ?? "", 
                It.IsAny<Guid>()
            ), Times.Once), 
            TimeSpan.FromSeconds(5)
        );
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saved).Should().NotThrow();
    }
    
    [Fact]
    public void Rendered_ParametersSet_LiveUpdateHandlerRegisteredOnce()
    {
        CreateComponent();
        var handlerRegistrations = GetLiveUpdateEventsForEntity<UserStory>(_story.Id, LiveUpdateEventType.EntityUpdated);
        handlerRegistrations.Should().ContainSingle();
    }
    
    [Fact]
    public async Task LiveUpdateHandlerTriggeredWithNewReviewComments_EditingIsDisabled_ReviewCommentsDisplayUpdatesCorrectly()
    {
        CreateComponent(isDisabled: true, initialReviewComments: "Some initial review comments");
        ReviewCommentsReadonlyDisplay.TextContent.Should().Be("Some initial review comments");
        
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<UserStory>(_story.Id, LiveUpdateEventType.EntityUpdated);
        _story.ReviewComments = "Some new review comments";
        await ComponentUnderTest.InvokeAsync(() => liveUpdateHandler.GetTypedEntityUpdateHandler<UserStory>().Invoke(_story, ActingUser.Id));
        
        ReviewCommentsReadonlyDisplay.TextContent.Should().Be("Some new review comments");
    }
    
    [Fact]
    public async Task LiveUpdateHandlerTriggeredWithNewReviewComments_EditingInProgress_ReviewTextInputValueUpdatesCorrectly()
    {
        CreateComponent(isDisabled: false, initialReviewComments: "Some initial review comments");
        ReviewCommentsInputForStory.GetAttribute("value").Should().Be("Some initial review comments");
        
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<UserStory>(_story.Id, LiveUpdateEventType.EntityUpdated);
        _story.ReviewComments = "Some new review comments";
        await ComponentUnderTest.InvokeAsync(() => liveUpdateHandler.GetTypedEntityUpdateHandler<UserStory>().Invoke(_story, ActingUser.Id));
        
        ReviewCommentsInputForStory.GetAttribute("value").Should().Be("Some new review comments");
    }
}