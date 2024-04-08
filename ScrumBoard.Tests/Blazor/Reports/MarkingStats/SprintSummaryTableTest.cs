using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared.Marking;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports.MarkingStats;

public class SprintSummaryTableTest : BaseProjectScopedComponentTestContext<SprintSummaryTable>
{
    private readonly Mock<IMarkingStatsService> _mockMarkingStatsService = new();
    private readonly Mock<IUsageDataService> _mockUsageDataService = new();
    private readonly Mock<IWorklogEntryService> _mockWorkLogEntryService = new();
    
    private const string NoTimeLoggedDisplay = "00:00:00";
    private const string OneMinuteDisplay = "00:01:00";
    private const string OneHourDisplay = "01:00:00";
    private const string OneHourThirtyDisplay = "01:30:00";
    private const long OneMinute = 600000000L;
    private static readonly DateOnly _weekOne = DateOnly.FromDayNumber(0);
    private static readonly DateOnly _weekTwo = DateOnly.FromDayNumber(7);
    private static readonly DateOnly _weekThree = DateOnly.FromDayNumber(14);

    public SprintSummaryTableTest()
    {
        Services.AddDbContextFactory<DatabaseContext>(options =>
            options.UseInMemoryDatabase("ScrumBoardInMemDbMyStatsTests"));

        Services.AddScoped(_ => new Mock<IUserRepository>().Object);
        Services.AddScoped(_ => _mockUsageDataService.Object);
        Services.AddScoped(_ => _mockMarkingStatsService.Object);
        Services.AddScoped(_ => _mockWorkLogEntryService.Object);
    }

    private IElement GetMetricForWeek(MarkingTableMetric metric, DateOnly weekStart)
    {
        var weekNum = IsoWeekCalculator.GetIsoWeekForDate(weekStart);
        return ComponentUnderTest.Find($"#{metric}-week-{weekNum}");
    }

    private void CreateComponent(
        IList<DateOnly> dateRanges,
        IEnumerable<WeeklyTimeSpan> overheadTimeSpans = null,
        IEnumerable<WeeklyTimeSpan> storyTimeSpans = null, 
        IEnumerable<WeeklyTimeSpan> testTimeSpans = null,
        IEnumerable<WeeklyTimeSpan> shortestWorklogTimeSpans = null,
        ICollection<Sprint> projectSprints = null
    )
    {
        CurrentProject.Sprints = projectSprints ?? [FakeDataGenerator.CreateFakeSprint(CurrentProject)];
        
        var returnedOverheadTimeSpans = overheadTimeSpans ?? dateRanges.Select(date => new WeeklyTimeSpan { WeekStart = date });
        var returnedStoryTimeSpans = storyTimeSpans ?? dateRanges.Select(date => new WeeklyTimeSpan { WeekStart = date });
        var returnedTestTimeSpans = testTimeSpans ?? dateRanges.Select(date => new WeeklyTimeSpan { WeekStart = date });
        var returnedShortestWorklogTimeSpans = shortestWorklogTimeSpans ?? dateRanges.Select(date => new WeeklyTimeSpan { WeekStart = date });
        
        _mockMarkingStatsService.Setup(
                x => x.CalculateDateRangesForSprintOrSprints(It.IsAny<IList<Sprint>>(), It.IsAny<long?>()))
            .ReturnsAsync(dateRanges);
        
        _mockMarkingStatsService
            .Setup(x => x.GetOverheadByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
            .ReturnsAsync(returnedOverheadTimeSpans);
   
        _mockMarkingStatsService
            .Setup(x => x.GetStoryHoursByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
            .ReturnsAsync(returnedStoryTimeSpans);
  
        _mockMarkingStatsService
            .Setup(x => x.GetTestHoursByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
            .ReturnsAsync(returnedTestTimeSpans);
        
        _mockMarkingStatsService
            .Setup(x => x.GetShortestWorklogDurationByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
            .ReturnsAsync(returnedShortestWorklogTimeSpans);
        
        CreateComponentUnderTest();
    }
    
    [Fact]
    public void Rendered_NoSprintsInProject_ServiceLayerNotCalled()
    {
        CreateComponent([], projectSprints: []);
        _mockMarkingStatsService.Verify(
            x => x.CalculateDateRangesForSprintOrSprints(It.IsAny<IEnumerable<Sprint>>(), It.IsAny<long?>()),
            Times.Never
        );
    }
    
    [Fact]
    public void Rendered_SprintsInProject_ServiceLayerCalled()
    {
        CreateComponent([], projectSprints: [ FakeDataGenerator.CreateFakeSprint(CurrentProject) ]);
        _mockMarkingStatsService.Verify(
            x => x.CalculateDateRangesForSprintOrSprints(It.IsAny<IEnumerable<Sprint>>(), It.IsAny<long?>()),
            Times.Once
        );
    }

    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_OnlyOverheadMetricHasTimeLogged_OnlyOverheadRowDisplaysNonZeroNumber(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne, Ticks = OneMinute }
        };
        
        switch (metric)
        {
            case MarkingTableMetric.Overhead:
                CreateComponent(dateRanges, overheadTimeSpans: timeSpans);
                break;
            case MarkingTableMetric.StoryHours:
                CreateComponent(dateRanges, storyTimeSpans: timeSpans);
                break;
            case MarkingTableMetric.TestHours:
                CreateComponent(dateRanges, testTimeSpans: timeSpans);
                break;
            case MarkingTableMetric.ShortestWorklogDuration:
                CreateComponent(dateRanges, shortestWorklogTimeSpans: timeSpans);
                break;
        }
        
        GetMetricForWeek(metric, _weekOne).Text().Should().Be(OneMinuteDisplay);
        GetMetricForWeek(metric, _weekTwo).Text().Should().Be(NoTimeLoggedDisplay);
        GetMetricForWeek(metric, _weekThree).Text().Should().Be(NoTimeLoggedDisplay);
        
        var allMetricsWithZeroWeeklyTimeSpans = Enum.GetValues(typeof(MarkingTableMetric))
            .Cast<MarkingTableMetric>()
            .Where(m => m != metric)
            .ToList();

        foreach (var zeroMetric in allMetricsWithZeroWeeklyTimeSpans)
        {
            GetMetricForWeek(zeroMetric, _weekOne).Text().Should().Be(NoTimeLoggedDisplay);
            GetMetricForWeek(zeroMetric, _weekTwo).Text().Should().Be(NoTimeLoggedDisplay);
            GetMetricForWeek(zeroMetric, _weekThree).Text().Should().Be(NoTimeLoggedDisplay);
        }
    }
}