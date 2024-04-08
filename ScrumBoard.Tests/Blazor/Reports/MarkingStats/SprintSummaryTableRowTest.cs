using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared.Marking;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports.MarkingStats;

public class SprintSummaryTableRowTest: BaseProjectScopedComponentTestContext<SprintSummaryTableRow>
{
    private readonly Mock<IMarkingStatsService> _mockMarkingStatsService = new();
    private readonly Mock<IUsageDataService> _mockUsageDataService = new();
    private readonly Mock<IWorklogEntryService> _mockWorkLogEntryService = new();

    private const string NoTimeLoggedDisplay = "00:00:00";
    private const string OneMinuteDisplay = "00:01:00";
    private const string OneHourDisplay = "01:00:00";
    private const string OneHourThirtyDisplay = "01:30:00";
    private static readonly long OneMinute = TimeSpan.FromMinutes(1).Ticks;
    private static readonly DateOnly _weekOne = DateOnly.FromDayNumber(0);
    private static readonly DateOnly _weekTwo = DateOnly.FromDayNumber(7);
    private static readonly DateOnly _weekThree = DateOnly.FromDayNumber(14);

    public SprintSummaryTableRowTest()
    {
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
    
    private string GetMetricForTotal(MarkingTableMetric metric)
    {
        try
        {
            return ComponentUnderTest.Find($"#{metric}-total").Text();
        }
        catch
        {
            return null;
        }
    }

    private void CreateComponent(
        IList<DateOnly> dateRanges, 
        MarkingTableMetric metric, 
        IEnumerable<WeeklyTimeSpan> weeklyTimeSpans = null
    ) {
        var timeSpans = weeklyTimeSpans?.ToList();
        var returnedTimeSpans = timeSpans ?? dateRanges.Select(date => new WeeklyTimeSpan { WeekStart = date });
        
        switch (metric)
        {
            case MarkingTableMetric.Overhead:
                _mockMarkingStatsService
                    .Setup(x => x.GetOverheadByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
                    .ReturnsAsync(returnedTimeSpans);
                break;
            case MarkingTableMetric.StoryHours:
                _mockMarkingStatsService
                    .Setup(x => x.GetStoryHoursByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
                    .ReturnsAsync(returnedTimeSpans);
                break;
            case MarkingTableMetric.TestHours:
                _mockMarkingStatsService
                    .Setup(x => x.GetTestHoursByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
                    .ReturnsAsync(returnedTimeSpans);
                break;
            case MarkingTableMetric.AvgLogDuration:
                _mockMarkingStatsService
                    .Setup(x => x.GetAvgWorkLogDurationByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
                    .ReturnsAsync(returnedTimeSpans);
                break;
            case MarkingTableMetric.ShortestWorklogDuration:
                _mockMarkingStatsService
                    .Setup(x => x.GetShortestWorklogDurationByWeek(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long?>()))
                    .ReturnsAsync(returnedTimeSpans);
                break;
        }
        
        _mockMarkingStatsService.Setup(
                x => x.CalculateDateRangesForSprintOrSprints(It.IsAny<IList<Sprint>>(), It.IsAny<long?>()))
            .ReturnsAsync(dateRanges);
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.WeekStartDatesAscending, dateRanges)
            .Add(x => x.Metric, metric)
            .Add(x => x.ShowTotal, !(metric == MarkingTableMetric.AvgLogDuration || metric == MarkingTableMetric.ShortestWorklogDuration)));
    }

    
    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_AllZeroTimeSpans_NoTestTimeDisplays(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };

        CreateComponent(dateRanges, metric);
        
        GetMetricForWeek(metric, _weekOne).Text().Should().Be(NoTimeLoggedDisplay);
        GetMetricForWeek(metric, _weekTwo).Text().Should().Be(NoTimeLoggedDisplay);
        GetMetricForWeek(metric, _weekThree).Text().Should().Be(NoTimeLoggedDisplay);
    }

    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_TimeSpansNonZeroForOneWeek_CorrectTimeDisplaysInCorrectWeek(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne },
            new() { WeekStart = _weekTwo, Ticks = OneMinute },
            new() { WeekStart = _weekThree }
        };

        CreateComponent(dateRanges, metric, timeSpans);
        
        GetMetricForWeek(metric, _weekOne).Text().Should().Be(NoTimeLoggedDisplay);
        GetMetricForWeek(metric, _weekTwo).Text().Should().Be(OneMinuteDisplay);
        GetMetricForWeek(metric, _weekThree).Text().Should().Be(NoTimeLoggedDisplay);
    }
    
    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_NonAdjacentIsoWeeksIncluded_WeeksWithoutLogsAreShown(MarkingTableMetric metric)
    {
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne, Ticks = OneMinute },
            new() { WeekStart = _weekThree, Ticks = OneMinute }
        };
 
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        
        CreateComponent(dateRanges, metric, timeSpans);

        GetMetricForWeek(metric, _weekOne).Should().NotBeNull();
        GetMetricForWeek(metric, _weekTwo).Should().NotBeNull();
        GetMetricForWeek(metric, _weekThree).Should().NotBeNull();
    }

    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_ManyTimeSpansInManyWeeks_CorrectTimesDisplayInCorrectWeeks(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne, Ticks = OneMinute * 60 },
            new() { WeekStart = _weekTwo, Ticks = OneMinute },
            new() { WeekStart = _weekThree, Ticks = OneMinute * 90 }
        };

        CreateComponent(dateRanges, metric, timeSpans);
        
        GetMetricForWeek(metric, _weekOne).Text().Should().Be(OneHourDisplay);
        GetMetricForWeek(metric, _weekTwo).Text().Should().Be(OneMinuteDisplay);
        GetMetricForWeek(metric, _weekThree).Text().Should().Be(OneHourThirtyDisplay);
    }

    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_OverlappingSprintsBothHaveTimeLogged_TooltipIsRenderedAndTotalTimeDisplayed(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne},
            new() { WeekStart = _weekTwo, Ticks = OneMinute, SprintId = 1, SprintName = "sprint 1" },
            new() { WeekStart = _weekTwo, Ticks = OneMinute * 59, SprintId = 2, SprintName = "sprint 2" },
            new() { WeekStart = _weekThree}
        };

        CreateComponent(dateRanges, metric, timeSpans);
        
        GetMetricForWeek(metric, _weekOne).Text().Should().Be(NoTimeLoggedDisplay);
        GetMetricForWeek(metric, _weekTwo).Text().Should().Be(metric == MarkingTableMetric.ShortestWorklogDuration
            ? OneMinuteDisplay
            : OneHourDisplay);
        GetMetricForWeek(metric, _weekThree).Text().Should().Be(NoTimeLoggedDisplay);
        ComponentUnderTest.FindAll(".marking-summary-tooltip").Count.Should().Be(1);
    }

    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    private void Rendered_AllWeeksHaveZeroTimeSpans_NoTotalTimeDisplayed(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        CreateComponent(dateRanges, metric);
        GetMetricForTotal(metric).Should().Be(NoTimeLoggedDisplay);
    }
    
    [Theory]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_AllWeeksHaveZeroTimeSpans_TotalTimeCellEmptyForShortestWorkLogAndAverageWorkLog(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        CreateComponent(dateRanges, metric);
        GetMetricForTotal(metric).Should().BeNull();
    }
    
    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    private void Rendered_ManyTimeSpansInManyWeeks_CorrectTotalTimeDisplayed(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne},
            new() { WeekStart = _weekTwo, Ticks = OneMinute, SprintId = 1, SprintName = "sprint 1"},
            new() { WeekStart = _weekTwo, Ticks = OneMinute * 59, SprintId = 2, SprintName = "sprint 2"},
            new() { WeekStart = _weekThree, Ticks = OneMinute * 30, SprintId = 2, SprintName = "sprint 2"}
        };
        CreateComponent(dateRanges, metric, timeSpans);
        GetMetricForTotal(metric).Should().Be(OneHourThirtyDisplay);
    }
    
    [Theory]
    [InlineData(MarkingTableMetric.AvgLogDuration)]
    [InlineData(MarkingTableMetric.ShortestWorklogDuration)]
    private void Rendered_ManyTimeSpansInManyWeeks_TotalTimeCellEmptyForShortestWorkLogAndAverageWorkLog(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne},
            new() { WeekStart = _weekTwo, Ticks = OneMinute, SprintId = 1, SprintName = "sprint 1"},
            new() { WeekStart = _weekTwo, Ticks = OneMinute * 59, SprintId = 2, SprintName = "sprint 2"},
            new() { WeekStart = _weekThree, Ticks = OneMinute * 30, SprintId = 2, SprintName = "sprint 2"}
        };
        CreateComponent(dateRanges, metric, timeSpans);
        GetMetricForTotal(metric).Should().BeNull();
    }
    
    [Theory]
    [InlineData(MarkingTableMetric.Overhead)]
    [InlineData(MarkingTableMetric.StoryHours)]
    [InlineData(MarkingTableMetric.TestHours)]
    private void Rendered_TimeSpansNonZeroForOneWeek_CorrectTotalTimeDisplayed(MarkingTableMetric metric)
    {
        var dateRanges = new List<DateOnly> { _weekOne, _weekTwo, _weekThree };
        var timeSpans = new List<WeeklyTimeSpan>
        {
            new() { WeekStart = _weekOne },
            new() { WeekStart = _weekTwo, Ticks = OneMinute },
            new() { WeekStart = _weekThree }
        };
        CreateComponent(dateRanges, metric, timeSpans);
        GetMetricForTotal(metric).Should().Be(OneMinuteDisplay);
    }
}
