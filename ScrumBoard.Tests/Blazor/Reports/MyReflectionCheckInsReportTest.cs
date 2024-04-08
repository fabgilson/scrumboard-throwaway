using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;
using ScrumBoard.Shared.ReflectionCheckIns;
using ScrumBoard.Shared.Report;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports;

public class MyReflectionCheckInsReportTest : BaseProjectScopedComponentTestContext<MyReflectionCheckIns>
{
    private readonly Mock<IWeeklyReflectionCheckInService> _weeklyReflectionCheckInServiceMock = new();

    private static readonly DateTime DefaultTestNow = new(2024, 2, 6);
    private Sprint _sprintStartedTwoWeeksAgo;

    private Func<IRenderedComponent<ReflectionCheckInSummaryDisplay>> GetSummaryDisplayComponent => ()
        => ComponentUnderTest.FindComponent<ReflectionCheckInSummaryDisplay>();

    private IElement ReportContainer => ComponentUnderTest.Find("#weekly-check-in-report-container");

    private Func<IElement> GetCheckInContainer(int isoWeek, int year) => ()
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year}");
    private Func<IElement> GetTitleRowOfCheckIn(int isoWeek, int year) => ()
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #check-in-title-row");
    private Func<IElement> GetStatusBadgeContainerOfCheckIn(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #check-in-status-badge");
    
    private Func<IElement> GetDidWellCommentsOfCheckIn(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #did-well-comments-display");
    private Func<IElement> GetDidNotDoSoWellCommentsOfCheckIn(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #did-not-do-so-well-comments-display");
    private Func<IElement> GetWillDoDifferentlyCommentsOfCheckIn(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #will-do-differently-comments-display");
    
    private Func<IElement> GetShowAnythingElseButton(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #show-anything-else-comments-button");
    private Func<IElement> GetShowAnythingElseComments(int isoWeek, int year) => () 
        => ComponentUnderTest.Find($"#check-in-display-container-{isoWeek}-{year} #show-anything-else-comments-display");
    
    
    private void CreateComponent(ICollection<FeatureFlagDefinition> enabledFeatureFlags = null, bool useTwoWeekSprint = false,
        DateTime? testNow = null, ICollection<WeeklyReflectionCheckIn> checkIns = null)
    {
        enabledFeatureFlags ??= [FeatureFlagDefinition.WeeklyReflectionCheckIn, FeatureFlagDefinition.WeeklyReflectionCheckInReportPage];
        testNow ??= DefaultTestNow;
        ClockMock.Setup(x => x.Now).Returns(testNow.Value);
        CurrentProject.StartDate = DateOnly.FromDateTime(testNow.Value.AddDays(-5 * 7));
        checkIns ??= [];
        _weeklyReflectionCheckInServiceMock
            .Setup(x => x.GetAllCheckInsForUserForProjectAsync(CurrentProject.Id, It.IsAny<long?>(), ActingUser.Id))
            .ReturnsAsync(checkIns);

        _sprintStartedTwoWeeksAgo = FakeDataGenerator.CreateFakeSprint(CurrentProject);
        _sprintStartedTwoWeeksAgo.StartDate = DateOnly.FromDateTime(testNow.Value.AddDays(-2 * 7));

        Services.AddScoped(_ => _weeklyReflectionCheckInServiceMock.Object);

        CreateComponentUnderTest(
            featureFlagsEnabledOnProject: enabledFeatureFlags,
            extendParameterBuilder: parameters =>
            {
                if (useTwoWeekSprint) parameters.AddCascadingValue("Sprint", _sprintStartedTwoWeeksAgo);
            });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    private void Rendered_MissingAnyOfRequiredFeatureFlags_NoMarkupGenerated(bool checkInsFeatureFlagEnabled, bool reportFeatureFlagEnabled)
    {
        var enabledFeatureFlags = new List<FeatureFlagDefinition>();
        if (checkInsFeatureFlagEnabled) enabledFeatureFlags.Add(FeatureFlagDefinition.WeeklyReflectionCheckIn);
        if (reportFeatureFlagEnabled) enabledFeatureFlags.Add(FeatureFlagDefinition.WeeklyReflectionCheckInReportPage);

        CreateComponent(enabledFeatureFlags: enabledFeatureFlags);
        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    private void Rendered_TaskCheckInsFeatureFlagNotEnabled_TaskSummaryDisplayNotRendered()
    {
        CreateComponent();
        GetSummaryDisplayComponent.Should().Throw<ComponentNotFoundException>();
    }

    [Fact]
    private void Rendered_TaskCheckInsFeatureFlagIsEnabled_TaskSummaryDisplayIsRendered()
    {
        CreateComponent(enabledFeatureFlags:
        [
            FeatureFlagDefinition.WeeklyReflectionCheckIn,
            FeatureFlagDefinition.WeeklyReflectionTaskCheckIns,
            FeatureFlagDefinition.WeeklyReflectionCheckInReportPage
        ]);
        GetSummaryDisplayComponent.Should().NotThrow();
    }

    [Fact]
    private void Rendered_SprintSelected_AllWeeksInSprintShown()
    {
        CreateComponent(useTwoWeekSprint: true);

        GetCheckInContainer(4, 2024).Should().NotThrow();
        GetCheckInContainer(5, 2024).Should().NotThrow();
        GetCheckInContainer(6, 2024).Should().NotThrow();
        ReportContainer.Children.Should().HaveCount(3);
    }

    [Fact]
    private void Rendered_NoSprintSelected_AllWeeksInProjectShown()
    {
        CreateComponent(useTwoWeekSprint: false);

        GetCheckInContainer(1, 2024).Should().NotThrow();
        GetCheckInContainer(2, 2024).Should().NotThrow();
        GetCheckInContainer(3, 2024).Should().NotThrow();
        GetCheckInContainer(4, 2024).Should().NotThrow();
        GetCheckInContainer(5, 2024).Should().NotThrow();
        GetCheckInContainer(6, 2024).Should().NotThrow();
        ReportContainer.Children.Should().HaveCount(6);
    }

    [Fact]
    private void Rendered_NoCheckInExistsForWeek_NotStartedCheckInShown()
    {
        CreateComponent();

        GetTitleRowOfCheckIn(6, 2024)().TextContent.Trim().Should().StartWith("Week 6, 2024");
        GetStatusBadgeContainerOfCheckIn(6, 2024)().TextContent.Trim().Should().Contain("Not started");
    }

    [Theory]
    [InlineData(CheckInCompletionStatus.Incomplete, "Incomplete")]
    [InlineData(CheckInCompletionStatus.Completed, "Completed")]
    private void Rendered_CheckInDoesExistForWeek_CorrectStatusShown(CheckInCompletionStatus status, string expectedBadgeStatus)
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, 6, 2024, completionStatus: status);
        CreateComponent(checkIns: [checkIn]);

        GetTitleRowOfCheckIn(6, 2024)().TextContent.Trim().Should().StartWith("Week 6, 2024");
        GetStatusBadgeContainerOfCheckIn(6, 2024)().TextContent.Trim().Should().Contain(expectedBadgeStatus);
    }

    [Fact]
    private void Rendered_NoCheckInExistsForWeek_NoCommentsRendered()
    {
        CreateComponent();
        GetDidWellCommentsOfCheckIn(6, 2024).Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    private void Rendered_CheckInDoesExistForWeek_CorrectCommentsShown()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, 6, 2024);
        checkIn.WhatIDidWell = "My did well comments";
        checkIn.WhatIDidNotDoWell = "My did not so well comments";
        checkIn.WhatIWillDoDifferently = "My will do differently comments";
        CreateComponent(checkIns: [checkIn]);

        GetDidWellCommentsOfCheckIn(6, 2024)().TextContent.Should().Be("My did well comments");
        GetDidNotDoSoWellCommentsOfCheckIn(6, 2024)().TextContent.Should().Be("My did not so well comments");
        GetWillDoDifferentlyCommentsOfCheckIn(6, 2024)().TextContent.Should().Be("My will do differently comments");
    }

    [Fact]
    private void Rendered_CheckInHasNoAnythingElseComments_NoButtonOrTextRenderedForAnythingElseSection()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, 6, 2024);
        checkIn.AnythingElse = "";
        CreateComponent(checkIns: [checkIn]);

        GetShowAnythingElseButton(6, 2024).Should().Throw<ElementNotFoundException>();
        GetShowAnythingElseComments(6, 2024).Should().Throw<ElementNotFoundException>();
    }

    [Fact]
    private void Rendered_CheckInHasSomeAnythingElseComments_ButtonIsRenderedButNoComments()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, 6, 2024);
        CreateComponent(checkIns: [checkIn]);

        GetShowAnythingElseButton(6, 2024).Should().NotThrow();
        GetShowAnythingElseComments(6, 2024).Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    private void ShowAnythingElseButtonClicked_CheckInHasSomeAnythingElseComments_ButtonIsHiddenAndTextIsRendered()
    {
        var checkIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(CurrentProject, ActingUser, 6, 2024);
        CreateComponent(checkIns: [checkIn]);
        
        GetShowAnythingElseButton(6, 2024)().Click();

        GetShowAnythingElseButton(6, 2024).Should().Throw<ElementNotFoundException>();
        GetShowAnythingElseComments(6, 2024)().TextContent.Should().Be(checkIn.AnythingElse);
    }
}