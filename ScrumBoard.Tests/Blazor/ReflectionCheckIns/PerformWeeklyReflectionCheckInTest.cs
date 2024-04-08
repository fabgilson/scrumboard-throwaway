using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EnumsNET;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Services;
using ScrumBoard.Shared.ReflectionCheckIns;
using ScrumBoard.Shared.Widgets.SaveStatus;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.ReflectionCheckIns;

public class PerformWeeklyReflectionCheckInTest : BaseProjectScopedComponentTestContext<PerformWeeklyReflectionCheckIn>
{
    private readonly Mock<IWeeklyReflectionCheckInService> _weeklyReflectionCheckInServiceMock = new (MockBehavior.Strict);

    private IElement CurrentWeekDisplay => ComponentUnderTest.Find("#current-week-display");
    private IElement GoBackOneWeekButton => ComponentUnderTest.Find("#go-to-previous-week-button");
    private IElement GoForwardsOneWeekButton => ComponentUnderTest.Find("#go-to-next-week-button");

    private Func<IElement> GetMarkDraftButton => () => ComponentUnderTest.Find("#mark-draft-button");
    private Func<IElement> GetMarkFinishedButton => () => ComponentUnderTest.Find("#mark-finished-button");
    private Func<IElement> GetBeginButton => () => ComponentUnderTest.Find("#begin-button");

    private IElement DidWellInput => ComponentUnderTest.Find("#did-well-input-area");
    private IElement DidNotDoWellInput => ComponentUnderTest.Find("#did-not-do-well-input-area");
    private IElement DoDifferentlyInput => ComponentUnderTest.Find("#will-do-differently-input-area");
    private IElement AnythingElseInput => ComponentUnderTest.Find("#anything-else-input-area");

    private Func<IElement> GetSaveIndicatorContainer => () => ComponentUnderTest.Find("#story-in-review-save-status-indicator");
    private Func<IElement> WaitForSaveStatusIndicatorComponent(FormSaveStatus status) => 
        () => ComponentUnderTest.WaitForElement($"#save-status-indicator-{status.GetName()}-display", TimeSpan.FromSeconds(5)); 
    
    private void CreateComponent(
        DateTime? currentDateTime=null, 
        WeeklyReflectionCheckIn mockCheckInValue=null, 
        bool skipSettingMockCheckIn=false, 
        DateTime? projectStartDate=null,
        bool isFeatureFlagEnabled=true
    ) {
        ClockMock.Setup(x => x.Now).Returns(currentDateTime ?? DateTime.Now);
        CurrentProject.StartDate = DateOnly.FromDateTime(projectStartDate ?? (currentDateTime ?? DateTime.Now).AddDays(-15));
        
        _weeklyReflectionCheckInServiceMock
            .Setup(x => x.SaveCheckInForUserAsync(It.IsAny<WeeklyReflectionCheckIn>(), ActingUser.Id, CurrentProject.Id, It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        
        if(!skipSettingMockCheckIn) SetMockValueForCheckIn(mockCheckInValue);
        SetTasksWorkedForCheckIn([]);
        
        Services.AddScoped(_ => _weeklyReflectionCheckInServiceMock.Object);

        CreateComponentUnderTest(featureFlagsEnabledOnProject: isFeatureFlagEnabled ? [FeatureFlagDefinition.WeeklyReflectionCheckIn] : []);
    }

    private void SetMockValueForCheckIn(WeeklyReflectionCheckIn checkIn, int? isoWeek=null, int? year=null)
    {
        _weeklyReflectionCheckInServiceMock.Setup(x => x.GetCheckInForUserForIsoWeekAndYear(
            ActingUser.Id,
            CurrentProject.Id,
            It.Is<int>(w => isoWeek == null || isoWeek == w),
            It.Is<int>(y => year == null || year == y)
        )).ReturnsAsync(checkIn);
    }

    private void SetTasksWorkedForCheckIn(ICollection<UserStoryTask> tasks, int? isoWeek=null, int? year=null)
    {
        _weeklyReflectionCheckInServiceMock.Setup(x => x.GetTasksWorkedOrAssignedToUserForIsoWeekAndYear(
            ActingUser.Id,
            CurrentProject.Id,
            It.Is<int>(w => isoWeek == null || isoWeek == w),
            It.Is<int>(y => year == null || year == y),
            It.IsAny<bool>()
        )).ReturnsAsync(tasks);
    }

    [Fact]
    private void Rendered_FeatureFlagNotSet_NothingShown()
    {
        CreateComponent(isFeatureFlagEnabled: false);
        ComponentUnderTest.Markup.Should().BeEmpty();
    }

    [Theory]
    [InlineData(2024, 2, 1, "Monday 29 Jan - Sunday 04 Feb", "(ISO week 5, 2024)")]
    [InlineData(2024, 1, 1, "Monday 01 Jan - Sunday 07 Jan", "(ISO week 1, 2024)")] 
    [InlineData(2024, 12, 31, "Monday 30 Dec - Sunday 05 Jan", "(ISO week 1, 2025)")] 
    [InlineData(2023, 12, 31, "Monday 25 Dec - Sunday 31 Dec", "(ISO week 52, 2023)")] 
    [InlineData(2020, 2, 29, "Monday 24 Feb - Sunday 01 Mar", "(ISO week 9, 2020)")]
    [InlineData(2024, 4, 29, "Monday 29 Apr - Sunday 05 May", "(ISO week 18, 2024)")] 
    [InlineData(2022, 1, 3, "Monday 03 Jan - Sunday 09 Jan", "(ISO week 1, 2022)")] 
    [InlineData(2022, 12, 26, "Monday 26 Dec - Sunday 01 Jan", "(ISO week 52, 2022)")]
    private void Rendered_DefaultState_WeekShownIsCurrentWeek(int year, int month, int day, string expectedRange, string expectedIsoWeek)
    {
        CreateComponent(new DateTime(year, month, day));
        
        CurrentWeekDisplay.TextContent.Trim().Should().StartWith(expectedRange);
        CurrentWeekDisplay.TextContent.Trim().Should().EndWith(expectedIsoWeek);
    }

    [Theory]
    [InlineData(2024, 2, 1, "Monday 22 Jan - Sunday 28 Jan", "(ISO week 4, 2024)")]
    [InlineData(2024, 1, 1, "Monday 25 Dec - Sunday 31 Dec", "(ISO week 52, 2023)")] 
    [InlineData(2024, 12, 31, "Monday 23 Dec - Sunday 29 Dec", "(ISO week 52, 2024)")] 
    [InlineData(2023, 12, 31, "Monday 18 Dec - Sunday 24 Dec", "(ISO week 51, 2023)")] 
    [InlineData(2020, 2, 29, "Monday 17 Feb - Sunday 23 Feb", "(ISO week 8, 2020)")]
    [InlineData(2024, 4, 29, "Monday 22 Apr - Sunday 28 Apr", "(ISO week 17, 2024)")] 
    [InlineData(2022, 1, 3, "Monday 27 Dec - Sunday 02 Jan", "(ISO week 52, 2021)")] 
    [InlineData(2022, 12, 26, "Monday 19 Dec - Sunday 25 Dec", "(ISO week 51, 2022)")]
    public void GoBackOneWeek_FromGivenDateTime_UpdatedWeekShownCorrectly(int year, int month, int day, string expectedPreviousRange, string expectedPreviousWeek)
    {
        CreateComponent(new DateTime(year, month, day));
        ComponentUnderTest.SaveSnapshot();
        
        GoBackOneWeekButton.Click();
        ComponentUnderTest.GetChangesSinceSnapshot().Should().NotBeEmpty();
        
        CurrentWeekDisplay.TextContent.Trim().Should().StartWith(expectedPreviousRange);
        CurrentWeekDisplay.TextContent.Trim().Should().EndWith(expectedPreviousWeek);
    }
    
    [Theory]
    [InlineData(2024, 2, 1, "Monday 29 Jan - Sunday 04 Feb", "(ISO week 5, 2024)")]
    [InlineData(2024, 1, 1, "Monday 01 Jan - Sunday 07 Jan", "(ISO week 1, 2024)")] 
    [InlineData(2024, 12, 31, "Monday 30 Dec - Sunday 05 Jan", "(ISO week 1, 2025)")] 
    [InlineData(2023, 12, 31, "Monday 25 Dec - Sunday 31 Dec", "(ISO week 52, 2023)")] 
    [InlineData(2020, 2, 29, "Monday 24 Feb - Sunday 01 Mar", "(ISO week 9, 2020)")]
    [InlineData(2024, 4, 29, "Monday 29 Apr - Sunday 05 May", "(ISO week 18, 2024)")] 
    [InlineData(2022, 1, 3, "Monday 03 Jan - Sunday 09 Jan", "(ISO week 1, 2022)")] 
    [InlineData(2022, 12, 26, "Monday 26 Dec - Sunday 01 Jan", "(ISO week 52, 2022)")]
    private void GoForwardsOneWeek_AfterHavingGoneBackOneWeek_WeekShownIsCurrentWeek(int year, int month, int day, string expectedRange, string expectedIsoWeek)
    {
        CreateComponent(new DateTime(year, month, day));
        ComponentUnderTest.SaveSnapshot();
        
        GoBackOneWeekButton.Click();
        ComponentUnderTest.GetChangesSinceSnapshot().Should().NotBeEmpty();
        ComponentUnderTest.SaveSnapshot();
        
        GoForwardsOneWeekButton.Click();
        ComponentUnderTest.GetChangesSinceSnapshot().Should().NotBeEmpty();
        
        CurrentWeekDisplay.TextContent.Trim().Should().StartWith(expectedRange);
        CurrentWeekDisplay.TextContent.Trim().Should().EndWith(expectedIsoWeek);
    }

    [Fact]
    private void Rendered_ViewingCurrentWeek_GoToNextWeekButtonIsDisabled()
    {
        CreateComponent();

        GoForwardsOneWeekButton.GetAttribute("disabled").Should().NotBeNull();
    }
    
    [Fact]
    private void GoBackOneWeek_NoLongerInCurrentWeek_GoToNextWeekButtonIsEnabled()
    {
        CreateComponent();
        GoBackOneWeekButton.Click();

        GoForwardsOneWeekButton.GetAttribute("disabled").Should().BeNull();
    }
    
    [Fact]
    private void Rendered_ViewingFirstWeekOfProject_GoToPreviousWeekButtonDisabled()
    {
        CreateComponent(new DateTime(2024, 2, 16), projectStartDate: new DateTime(2024, 2, 15));

        GoBackOneWeekButton.GetAttribute("disabled").Should().NotBeNull();
    }
    
    [Fact]
    private void Rendered_NotViewingFirstWeekOfProject_GoToPreviousWeekButtonEnabled()
    {
        CurrentProject.StartDate = DateOnly.FromDateTime(new DateTime(2024, 2, 1));
        
        CreateComponent(new DateTime(2024, 2, 16));

        GoBackOneWeekButton.GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    private void Rendered_NoChangesMade_SaveStatusIndicatorNotRendered()
    {
        CreateComponent();
        GetSaveIndicatorContainer.Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    private void ReflectionCommentsInput_TextInput_SavingIndicatorShown()
    {
        CreateComponent();
        DidWellInput.Input("Some reflective comment");
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saving).Should().NotThrow();
    }
    
    [Fact]
    private void ReflectionCommentsInput_ValidComments_SavedIndicatorShown()
    {
        CreateComponent();
        DidWellInput.Input("Some valid comment");
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saved).Should().NotThrow();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    private void ReflectionCommentsInput_InvalidComments_UnsavedIndicatorShown(string comments)
    {
        CreateComponent();
        DidWellInput.Input(comments);
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Unsaved).Should().NotThrow();
    }

    [Fact]
    private void Rendered_CheckInNotStarted_OnlyBeginButtonShownAndCommentsDisabled()
    {
        CreateComponent();

        GetBeginButton.Should().NotThrow();
        GetMarkDraftButton.Should().Throw<ElementNotFoundException>();
        GetMarkFinishedButton.Should().Throw<ElementNotFoundException>();
        DidWellInput.GetAttribute("disabled").Should().NotBeNull();
    }
    
    [Fact]
    private void Rendered_CheckInHasBeenStarted_OnlyMarkFinishedButtonShownAndCommentsEnabled()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, completionStatus: CheckInCompletionStatus.Incomplete);
        CreateComponent(mockCheckInValue: checkIn);

        GetMarkFinishedButton.Should().NotThrow();
        GetMarkDraftButton.Should().Throw<ElementNotFoundException>();
        GetBeginButton.Should().Throw<ElementNotFoundException>();
        DidWellInput.GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    private void Rendered_CheckInHasBeenFinished_OnlyMarkDraftButtonShownAndCommentsEnabled()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, completionStatus: CheckInCompletionStatus.Completed);
        CreateComponent(mockCheckInValue: checkIn);

        GetMarkDraftButton.Should().NotThrow();
        GetMarkFinishedButton.Should().Throw<ElementNotFoundException>();
        GetBeginButton.Should().Throw<ElementNotFoundException>();
        DidWellInput.GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    private void BeginButtonPressed_NoExistingCheckIn_ServiceLayerCalled()
    {
        CreateComponent(new DateTime(2024, 2, 6));
        
        GetBeginButton().Click();
        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saved).Should().NotThrow();

        _weeklyReflectionCheckInServiceMock.Verify(x => x.SaveCheckInForUserAsync(
                It.Is<WeeklyReflectionCheckIn>(checkIn =>
                    checkIn.IsoWeekNumber == 6
                    && checkIn.Year == 2024
                    && checkIn.WhatIDidWell == ""
                    && checkIn.WhatIDidNotDoWell == ""
                    && checkIn.WhatIWillDoDifferently == ""
                    && checkIn.AnythingElse == ""
                    && checkIn.CompletionStatus == CheckInCompletionStatus.Incomplete
                ),
                ActingUser.Id,
                CurrentProject.Id,
                It.IsAny<Guid>()), 
            Times.Once);
    }

    [Fact]
    private void MarkFinishedButtonPressed_AllValid_CheckInSaved()
    {
        var incompleteCheckIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, completionStatus: CheckInCompletionStatus.Incomplete);
        
        CreateComponent(mockCheckInValue: incompleteCheckIn);
        GetMarkFinishedButton().Click();
        
        _weeklyReflectionCheckInServiceMock.Verify(x => x.SaveCheckInForUserAsync(
                It.Is<WeeklyReflectionCheckIn>(checkIn => checkIn.CompletionStatus == CheckInCompletionStatus.Completed),
                ActingUser.Id,
                CurrentProject.Id,
                It.IsAny<Guid>())
            , Times.Once);
    }

    [Fact]
    private void MarkDraftButtonPressed_ExistingCheckIn_CheckInSaved()
    {
        var finishedCheckIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, completionStatus: CheckInCompletionStatus.Completed);
        
        CreateComponent(mockCheckInValue: finishedCheckIn);
        GetMarkDraftButton().Click();
        
        _weeklyReflectionCheckInServiceMock.Verify(x => x.SaveCheckInForUserAsync(
                It.Is<WeeklyReflectionCheckIn>(checkIn => checkIn.CompletionStatus == CheckInCompletionStatus.Incomplete),
                ActingUser.Id,
                CurrentProject.Id,
                It.IsAny<Guid>())
            , Times.Once);
    }

    [Fact]
    private void ReflectionCommentsChanged_CheckInCompleted_CheckInSavedAndMarkedIncomplete()
    {
        var finishedCheckIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, completionStatus: CheckInCompletionStatus.Completed);
        
        CreateComponent(mockCheckInValue: finishedCheckIn);
        DidWellInput.Input("Some new comments");

        WaitForSaveStatusIndicatorComponent(FormSaveStatus.Saved).Should().NotThrow();
        _weeklyReflectionCheckInServiceMock.Verify(x => x.SaveCheckInForUserAsync(
                It.Is<WeeklyReflectionCheckIn>(checkIn => 
                    checkIn.CompletionStatus == CheckInCompletionStatus.Incomplete 
                    && checkIn.WhatIDidWell == "Some new comments" 
                ),
                ActingUser.Id,
                CurrentProject.Id,
                It.IsAny<Guid>())
            , Times.Once);
    }

    [Fact]
    private void WeekChanged_NoLongerCurrentWeek_CheckInRequestedFromServiceLayer()
    {
        CreateComponent(new DateTime(2024, 2, 6));
        _weeklyReflectionCheckInServiceMock.Verify(
            x => x.GetCheckInForUserForIsoWeekAndYear(ActingUser.Id, CurrentProject.Id, 6, 2024), 
            Times.Once
        );
        
        GoBackOneWeekButton.Click();
        _weeklyReflectionCheckInServiceMock.Verify(
            x => x.GetCheckInForUserForIsoWeekAndYear(ActingUser.Id, CurrentProject.Id, 5, 2024), 
            Times.Once
        );
    }

    [Fact]
    private void Rendered_UnStartedCheckIn_UsageEventCreated()
    {
        CreateComponent();
        UsageDataServiceMock.Verify(x => x.AddUsageEvent(It.Is<ProjectViewLoadedUsageEvent>(e => 
            e.ProjectId == CurrentProject.Id &&
            e.LoadedViewUsageEventType == ViewLoadedUsageEventType.PerformWeeklyReflectionsPage &&
            e.ResourceId == -1
            )), Times.Once);
        UsageDataServiceMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    private void Rendered_ExistingCheckIn_UsageEventCreated()
    {
        var existing = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser);
        CreateComponent(mockCheckInValue: existing);
        UsageDataServiceMock.Verify(x => x.AddUsageEvent(It.Is<ProjectViewLoadedUsageEvent>(e => 
            e.ProjectId == CurrentProject.Id &&
            e.LoadedViewUsageEventType == ViewLoadedUsageEventType.PerformWeeklyReflectionsPage &&
            e.ResourceId == existing.Id
        )), Times.Once);
        UsageDataServiceMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    private void ChangeWeek_MultipleCheckIns_MultipleUsageEventsCreated()
    {
        var existing1 = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, isoWeekNum: 6, year: 2024);
        var existing2 = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, isoWeekNum: 5, year: 2024);
        
        SetMockValueForCheckIn(existing1, 6, 2024);
        SetMockValueForCheckIn(existing2, 5, 2024);
        SetMockValueForCheckIn(null, 4, 2024);
        
        CreateComponent(currentDateTime: new DateTime(2024, 2, 6), skipSettingMockCheckIn: true);
        
        UsageDataServiceMock.Verify(x => x.AddUsageEvent(It.Is<ProjectViewLoadedUsageEvent>(e => 
            e.ProjectId == CurrentProject.Id &&
            e.LoadedViewUsageEventType == ViewLoadedUsageEventType.PerformWeeklyReflectionsPage &&
            e.ResourceId == existing1.Id
        )), Times.Once);
        UsageDataServiceMock.VerifyNoOtherCalls();
        
        GoBackOneWeekButton.Click();
        UsageDataServiceMock.Verify(x => x.AddUsageEvent(It.Is<ProjectViewLoadedUsageEvent>(e => 
            e.ProjectId == CurrentProject.Id &&
            e.LoadedViewUsageEventType == ViewLoadedUsageEventType.PerformWeeklyReflectionsPage &&
            e.ResourceId == existing2.Id
        )), Times.Once);
        UsageDataServiceMock.VerifyNoOtherCalls();
        
        GoBackOneWeekButton.Click();
        UsageDataServiceMock.Verify(x => x.AddUsageEvent(It.Is<ProjectViewLoadedUsageEvent>(e => 
            e.ProjectId == CurrentProject.Id &&
            e.LoadedViewUsageEventType == ViewLoadedUsageEventType.PerformWeeklyReflectionsPage &&
            e.ResourceId == -1
        )), Times.Once);
        UsageDataServiceMock.VerifyNoOtherCalls();
    }
}