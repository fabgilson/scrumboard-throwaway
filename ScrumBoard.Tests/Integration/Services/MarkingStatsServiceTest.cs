using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class MarkingStatsServiceTest : BaseIntegrationTestFixture
{
    private readonly IMarkingStatsService _markingStatsService;
    private User _user, _secondUser;
    private Sprint _sprint, _secondSprint;
    private OverheadEntry _sprintOverheadEntry, _secondSprintOverheadEntry;
    private WorklogEntry _sprintWorkLogEntry, _secondSprintWorkLogEntry, _secondSprintSecondWorkLogEntry;
    private WorklogTag _testTag, _testManualTag;
    private TaggedWorkInstance _sprintTaggedWorkInstance, _secondSprintTaggedWorkInstance;
    private Project _project;
    private UserStory _userStory, _secondUserStory;
    private UserStoryTask _task, _secondTask;

    private IEnumerable<WorklogTag> _workLogTags;
    private const string TestTagName = "Test";
    private const string TestManualTagName = "Testmanual";
    private const int SprintLengthDays = 14;
    private static readonly DateOnly _weekOne = DateOnly.FromDayNumber(0);
    private static readonly DateOnly _weekTwo = DateOnly.FromDayNumber(7);
    private static readonly DateOnly _weekThree = DateOnly.FromDayNumber(14);
    private static readonly DateOnly _weekFour = DateOnly.FromDayNumber(21);
    private OverheadEntry _overheadEntry;

    public MarkingStatsServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _markingStatsService = ServiceProvider.GetRequiredService<IMarkingStatsService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _user = FakeDataGenerator.CreateFakeUser();
        _secondUser = FakeDataGenerator.CreateFakeUser();
        _project = FakeDataGenerator.CreateFakeProject(developers: new[] { _user, _secondUser });
        
        _workLogTags = new List<WorklogTag>
        {
            new() { Name = "Feature" },
            new() { Name = "Fix" },
            new() { Name = TestTagName },
            new() { Name = "Document" },
            new() { Name = "Chore" },
            new() { Name = "Spike" },
            new() { Name = "Refactor", },
            new() { Name = "Review" },
            new() { Name = TestManualTagName },
            new() { Name = "Reengineer" }
        };

        _testTag = _workLogTags.First(x => x.Name == TestTagName);
        _testManualTag = _workLogTags.First(x => x.Name == TestManualTagName);
        
        await SaveEntries(new List<User> { _user, _secondUser });
        await SaveEntries(new List<Project> { _project });
        await SaveEntries(_workLogTags);
    }
    
    [Fact]
    public async Task GetSprintEndOrLastLog_SprintHasNoLogs_ReturnsSprintEnd()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now).AddDays(SprintLengthDays));

        var endDate = await _markingStatsService.GetSprintEndOrLastLog(_sprint);
        endDate.Should().Be(_sprint.EndDate);
    }

    [Fact]
    public async Task GetSprintEndOrLastLog_SprintHasLogsBeforeEnd_ReturnsSprintEnd()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now).AddDays(SprintLengthDays));
        await SaveEntries([_sprint]);
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint);
        _secondSprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint);

        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry, _secondSprintOverheadEntry });
        
        var endDate = await _markingStatsService.GetSprintEndOrLastLog(_sprint);
        endDate.Should().Be(_sprint.EndDate);
    }

    [Fact]
    public async Task GetSprintEndOrLastLog_SprintHasManyLogsAfterEnd_ReturnsLastLogDate()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now).AddDays(SprintLengthDays));
        await SaveEntries([_sprint]);
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, _sprint.EndDate.AddDays(1).ToDateTime(new TimeOnly()));
        _secondSprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, _sprint.EndDate.AddDays(2).ToDateTime(new TimeOnly()));
        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry, _secondSprintOverheadEntry });

        var endDate = await _markingStatsService.GetSprintEndOrLastLog(_sprint);
        endDate.Should().Be(DateOnly.FromDateTime(_secondSprintOverheadEntry.Occurred));
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_SprintProvided_ReturnsSprintDateRanges()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(21), DateOnly.FromDayNumber(35)); // ISO week 3
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint}, _sprint.Id);
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(21), DateOnly.FromDayNumber(28), DateOnly.FromDayNumber(35) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_SprintLessThanOneWeekLong_ReturnsOneWeek()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(16));
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint}, _sprint.Id);
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(14) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_SprintPartiallyCoversTwoWeeks_ReturnsCorrectTwoWeeks()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(23));
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint}, _sprint.Id);
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(14), DateOnly.FromDayNumber(21) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_ProjectHasMultipleSprintsInSameWeek_ReturnsDuplicatedWeekOnce()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(23));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(24), DateOnly.FromDayNumber(25));
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint, _secondSprint});
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(14), DateOnly.FromDayNumber(21) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_ProjectHasWeeksWithoutSprints_ReturnsWeeksWithSprints()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(3));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(17));
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint, _secondSprint});
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(0), DateOnly.FromDayNumber(14) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_ProjectHasOngoingSprint_ReturnsDatesUntilEndOfSprint()
    {
        var start = DateOnly.FromDateTime(DateTime.Now);
        var end = start.AddDays(14);
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, start, end);
        
        var lastMondayDiff = IsoWeekCalculator.DayOfWeekToMondayStart(DayOfWeek.Monday) - IsoWeekCalculator.DayOfWeekToMondayStart(start.DayOfWeek);
        var lastMondayDate = start.AddDays(lastMondayDiff);
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> { _sprint });
        var expectedDateRanges = new List<DateOnly> { lastMondayDate, lastMondayDate.AddDays(7), lastMondayDate.AddDays(14) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    } 
    
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_SprintHasOverheadAfterEnd_ReturnsDatesUntilNow()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project,DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(3));
        await SaveEntries([_sprint]);
        var overheadDate = _sprint.EndDate.AddDays(100).ToDateTime(new TimeOnly());
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, overheadDate);
        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry });

        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> { _sprint});

        var overheadWeekMondayDiff = IsoWeekCalculator.DayOfWeekToMondayStart(DayOfWeek.Monday) - IsoWeekCalculator.DayOfWeekToMondayStart(overheadDate.DayOfWeek);
        var dateRangeEndMonday = DateOnly.FromDateTime(overheadDate.AddDays(overheadWeekMondayDiff));
        
        dateRanges.First().Should().Be(DateOnly.FromDayNumber(0));
        dateRanges.Last().Should().Be(dateRangeEndMonday);
    }
    
    [Theory]
    [MemberData(nameof(WeekDays))]
    public async Task CalculateDateRangesForSprintOrProject_StartDayIsEachDayOfTheWeek_ReturnsWeekDateIsInWithMondayStart(
        DateOnly startDate)
    {
        var mondayDate = DateOnly.FromDateTime(new DateTime(2023, 11, 27)); // is a Monday
        
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, startDate, startDate);
        
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> { _sprint });
        var expectedDateRanges = new List<DateOnly> { mondayDate };
        
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }
            
    [Fact]
    public async Task CalculateDateRangesForSprintOrProject_ProjectGoesAcrossYearEnd_datesOrderedWithEndOfPreviousYearBeforeStartOfNextYear()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(360), DateOnly.FromDayNumber(370));
    
        var dateRanges = await _markingStatsService.CalculateDateRangesForSprintOrSprints(new List<Sprint> {_sprint});
        var expectedDateRanges = new List<DateOnly> { DateOnly.FromDayNumber(357), DateOnly.FromDayNumber(364) };
    
        dateRanges.Should().ContainInOrder(expectedDateRanges);
    }

    [Fact]
    public async Task GetOverheadByWeek_SprintHasNoOverheadEntries_ReturnsEmptyTimeSpansForEachWeek()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedOverhead = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        overhead.Should().Contain(expectedOverhead);
    }

    [Fact]
    public async Task GetOverheadByWeek_SprintHasOverheadInSomeWeeks_ReturnsAllWeeksWithSomeEmpty()
    {
        var overheadDate = DateOnly.FromDayNumber(7);
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, overheadDate.ToDateTime(new TimeOnly()));

        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedOverhead = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = overheadDate,
                Ticks = _sprintOverheadEntry.DurationTicks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };

        overhead.Should().Contain(expectedOverhead);
    }

    [Fact]
    public async Task GetOverheadByWeek_ProjectHasMultipleSprints_ReturnsOverheadForMultipleSprints()
    {
        var overheadDate = DateOnly.FromDayNumber(7);
        var secondOverheadDate = DateOnly.FromDayNumber(21);
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(21), DateOnly.FromDayNumber(25));
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, overheadDate.ToDateTime(new TimeOnly()));
        _secondSprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _secondSprint, secondOverheadDate.ToDateTime(new TimeOnly()));

        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry, _secondSprintOverheadEntry });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id);
        var expectedOverhead = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = overheadDate,
                Ticks = _sprintOverheadEntry.DurationTicks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _secondSprint),
            new()
            {
                WeekStart = secondOverheadDate,
                Ticks = _secondSprintOverheadEntry.DurationTicks,
                SprintId = _secondSprint.Id,
                SprintName = _secondSprint.Name
            }
        };

        overhead.Should().Contain(expectedOverhead);
    }

    [Fact]
    public async Task GetOverheadByWeek_ProjectHasOverlappingSprintWeeks_ReturnsSeparateOverheadForEachWeek()
    {
        var overheadDate = DateOnly.FromDayNumber(14);
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(25));
        _sprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint, overheadDate.ToDateTime(new TimeOnly()));
        _secondSprintOverheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _secondSprint, overheadDate.AddDays(1).ToDateTime(new TimeOnly()));
        
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<OverheadEntry> { _sprintOverheadEntry, _secondSprintOverheadEntry });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id);
        var expectedOverhead = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            new()
            {
                WeekStart = overheadDate,
                Ticks = _sprintOverheadEntry.DurationTicks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _secondSprint),
            new()
            {
                WeekStart = overheadDate,
                Ticks = _secondSprintOverheadEntry.DurationTicks,
                SprintId = _secondSprint.Id,
                SprintName = _secondSprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _secondSprint),

        };

        overhead.Should().Contain(expectedOverhead);
    }
    
    [Fact]
    public async Task GetStoryHoursByWeek_SprintHasNoStoryHoursLogged_ReturnsEmptyTimeSpansForEachWeek()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var storyHours = await _markingStatsService.GetStoryHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedStoryHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        storyHours.Should().Contain(expectedStoryHours);
    }

    [Fact]
    public async Task GetStoryHoursByWeek_SprintHasStoryHoursInSomeWeeks_ReturnsAllWeeksWithSomeEmpty()
    {
        var workLogDate = DateOnly.FromDayNumber(7);
        
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, workLogDate.ToDateTime(new TimeOnly()));

        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<UserStory> { _userStory });
        await SaveEntries(new List<UserStoryTask> { _task });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry });

        var storyHours = await _markingStatsService.GetStoryHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedStoryHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = workLogDate,
                Ticks = _sprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };

        storyHours.Should().Contain(expectedStoryHours);
    }

    [Fact]
    public async Task GetStoryHoursByWeek_ProjectHasMultipleSprints_ReturnsStoryHoursForMultipleSprints()
    {
        var workLogDate = DateOnly.FromDayNumber(7);
        var secondWorkLogDate = DateOnly.FromDayNumber(21);
        
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(21), DateOnly.FromDayNumber(25));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _secondUserStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_secondSprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _secondTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_secondUserStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, workLogDate.ToDateTime(new TimeOnly()));
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _secondSprint, _secondTask, secondWorkLogDate.ToDateTime(new TimeOnly()));

        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<UserStory> { _userStory, _secondUserStory });
        await SaveEntries(new List<UserStoryTask> { _task, _secondTask });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry });

        var storyHours = await _markingStatsService.GetStoryHoursByWeek(_user.Id, _project.Id);
        var expectedStoryHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = workLogDate,
                Ticks = _sprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _secondSprint),
            new()
            {
                WeekStart = secondWorkLogDate,
                Ticks = _secondSprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _secondSprint.Id,
                SprintName = _secondSprint.Name
            }
        };

        storyHours.Should().Contain(expectedStoryHours);
    }

    [Fact]
    public async Task GetStoryHoursByWeek_ProjectHasOverlappingSprintWeeks_ReturnsSeparateStoryHoursForEachWeek()
    {
        var workLogDate = DateOnly.FromDayNumber(14);
        
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(15), DateOnly.FromDayNumber(25));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _secondUserStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_secondSprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _secondTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_secondUserStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, workLogDate.ToDateTime(new TimeOnly()));
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _secondSprint, _secondTask, workLogDate.AddDays(1).ToDateTime(new TimeOnly()));
        
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<UserStory> { _userStory, _secondUserStory });
        await SaveEntries(new List<UserStoryTask> { _task, _secondTask });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry });

        var storyHours = await _markingStatsService.GetStoryHoursByWeek(_user.Id, _project.Id);
        var expectedStoryHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            new()
            {
                WeekStart = workLogDate,
                Ticks = _sprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _secondSprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _secondSprint),
            new()
            {
                WeekStart = workLogDate,
                Ticks = _secondSprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _secondSprint.Id,
                SprintName = _secondSprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekFour, _secondSprint),

        };

        storyHours.Should().Contain(expectedStoryHours);
    }

    [Fact]
    public async Task GetTestHoursByWeek_SprintHasNoTestLogs_ReturnsEmptyTimeSpans()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }     
    
    [Fact]
    public async Task GetTestHoursByWeek_ProjectHasNoTestLogs_ReturnsEmptyTimeSpans()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id);
        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }

    private async Task SetupForTestHoursTests()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(0), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        
        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<UserStory> { _userStory });
        await SaveEntries(new List<UserStoryTask> { _task });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry });
    }
    
    [Theory]
    [InlineData(TestTagName)]
    [InlineData(TestManualTagName)]
    public async Task GetTestHoursByWeek_OneTestLog_TimeSpanHasDurationOfTestLog(string tagName)
    {
        await SetupForTestHoursTests();
        var tag = tagName == TestTagName ? _testTag : _testManualTag;
        
        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(tag, _sprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetTestHoursByWeek_OneTestAndOneManualTestLog_TimeSpanHasDurationOfCombinedLogs()
    {
        await SetupForTestHoursTests();
        
        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_testTag, _sprintWorkLogEntry);
        _secondSprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_testManualTag, _sprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance, _secondSprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks + _secondSprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Theory]
    [InlineData("Feature")]
    [InlineData("Fix")]
    [InlineData("Document")]
    [InlineData("Chore")]
    [InlineData("Spike")]
    [InlineData("Refactor")]
    [InlineData("Reengineer")]
    [InlineData("Review")]
    public async Task GetTestHoursByWeek_AllLogsAreNonTest_ReturnsEmptyTimeSpans(string worklogTagName)
    {
        await SetupForTestHoursTests();

        var worklogTag = _workLogTags.First(x => x.Name == worklogTagName);
        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(worklogTag, _sprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);

        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Theory]
    [InlineData("Feature")]
    [InlineData("Fix")]
    [InlineData("Document")]
    [InlineData("Chore")]
    [InlineData("Spike")]
    [InlineData("Refactor")]
    [InlineData("Reengineer")]
    [InlineData("Review")]
    public async Task GetTestHoursByWeek_SomeLogsAreNonTest_ReturnsDurationOfTestLogs(string worklogTagName)
    {
        await SetupForTestHoursTests();
        
        var worklogTag = _workLogTags.First(x => x.Name == worklogTagName);

        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_testTag, _sprintWorkLogEntry);
        _secondSprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(worklogTag, _secondSprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance, _secondSprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetTestHoursByWeek_OnlyOverheadHasBeenLogged_ReturnsEmptyTimeSpans()
    {
        await SetupForTestHoursTests();

        _overheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint);
        await SaveEntries(new List<OverheadEntry> { _overheadEntry });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }  
    
    [Fact]
    public async Task GetTestHoursByWeek_SomeLogsAreOverheadAndSomeAreTest_ReturnsDurationOfTestLogs()
    {
        await SetupForTestHoursTests();
        
        _overheadEntry = FakeDataGenerator.CreateFakeOverheadEntry(_user, _sprint);
        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_testTag, _sprintWorkLogEntry);
        await SaveEntries(new List<OverheadEntry> { _overheadEntry });
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetTestHoursByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }

    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForSprint_NoWorkLogs_ReturnsZeroForAllWeeks()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        overhead.Should().Contain(expectedAvgTimeLogged);
    }

    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForSprint_OneWorkLogInSomeWeeks_ReturnsCorrectForWeeksWithWorkLogsAndZeroOtherwise()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        
        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<UserStory> { _userStory });
        await SaveEntries(new List<UserStoryTask> { _task });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry });
    
        var averageTimeLogged= await _markingStatsService.GetAvgWorkLogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = DateOnly.FromDayNumber(0),
                Ticks = _sprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
    
        averageTimeLogged.Should().Contain(expectedAvgTimeLogged);
    }

    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForSprint_MultipleWorkLogsInSomeWeeks_ReturnsAverageDurationForEachWeekAndZeroOtherwise()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        
        var dateOfWorkLog = DateOnly.FromDayNumber(1).ToDateTime(TimeOnly.MaxValue);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, dateOfWorkLog);
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, dateOfWorkLog.AddDays(1));
        
        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<UserStory> { _userStory });
        await SaveEntries(new List<UserStoryTask> { _task });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry });
    
        var averageTimeLogged= await _markingStatsService.GetAvgWorkLogDurationByWeek(_user.Id, _project.Id, _sprint.Id);

        var averageOfSprintWorkLogs = (_sprintWorkLogEntry.GetTotalTimeSpent().Ticks + _secondSprintWorkLogEntry.GetTotalTimeSpent().Ticks) / 2;
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = DateOnly.FromDayNumber(0),
                Ticks = averageOfSprintWorkLogs,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
    
        averageTimeLogged.Should().Contain(expectedAvgTimeLogged);
    }
    
     [Fact]
    public async Task GetAvgWorkLogDurationByWeekForProject_NoWorkLogs_ReturnsZeroForAllWeeks()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(10));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(11), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });

        var overhead = await _markingStatsService.GetOverheadByWeek(_user.Id, _project.Id, null);
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        overhead.Should().Contain(expectedAvgTimeLogged);
    }

    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForProject_OneWorkLogInSomeWeeks_ReturnsCorrectForWeeksWithWorkLogsAndZeroOtherwise()
    {

        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(10));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(11), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _secondUserStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_secondSprint);        
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _secondTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_secondUserStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _secondSprint, _secondTask);
        
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<UserStory> { _userStory, _secondUserStory });
        await SaveEntries(new List<UserStoryTask> { _task, _secondTask });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry });
    
        var averageTimeLogged= await _markingStatsService.GetAvgWorkLogDurationByWeek(_user.Id, _project.Id, null);
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = DateOnly.FromDayNumber(0),
                Ticks = _sprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            new()
            {
                WeekStart = DateOnly.FromDayNumber(14),
                Ticks = _secondSprintWorkLogEntry.GetTotalTimeSpent().Ticks,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            }
        };
    
        averageTimeLogged.Should().Contain(expectedAvgTimeLogged);
    }

    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForProject_DifferentNumberOfWorklogsInSprintOverlappingWeeks_ReturnsAverageDurationAccountingForNumberOfWorklogsInEachSprint()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(0), DateOnly.FromDayNumber(14));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, _sprint.EndDate.AddDays(1), _sprint.EndDate.AddDays(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _secondUserStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_secondSprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _secondTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_secondUserStory);

        var dateOfWorkLog = _sprint.EndDate.ToDateTime(TimeOnly.MaxValue);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task, dateOfWorkLog);
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _secondSprint, _secondTask, dateOfWorkLog.AddDays(1));
        _secondSprintSecondWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _secondSprint, _secondTask, dateOfWorkLog.AddDays(2));
        
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<UserStory> { _userStory, _secondUserStory });
        await SaveEntries(new List<UserStoryTask> { _task, _secondTask });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry, _secondSprintWorkLogEntry, _secondSprintSecondWorkLogEntry });
    
        var averageTimeLogged= await _markingStatsService.GetAvgWorkLogDurationByWeek(_user.Id, _project.Id, null);

        var averageOfSprintWorkLogs = (_sprintWorkLogEntry.GetTotalTimeSpent().Ticks + _secondSprintWorkLogEntry.GetTotalTimeSpent().Ticks + _secondSprintSecondWorkLogEntry.GetTotalTimeSpent().Ticks) / 3;
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne, _sprint),
            new()
            {
                WeekStart = DateOnly.FromDayNumber(0),
                Ticks = averageOfSprintWorkLogs,
                SprintId = _sprint.Id,
                SprintName = _sprint.Name
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
    
        averageTimeLogged.Should().Contain(expectedAvgTimeLogged);
    }
    
    [Fact]
    public async Task GetAvgWorkLogDurationByWeekForProject_TasksHaveNoTimeLogged_ReturnsZeroForEachWeek()
    {

        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(10));
        _secondSprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(11), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _secondUserStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_secondSprint);        
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _secondTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_secondUserStory);
        
        await SaveEntries(new List<Sprint> { _sprint, _secondSprint });
        await SaveEntries(new List<UserStory> { _userStory, _secondUserStory });
        await SaveEntries(new List<UserStoryTask> { _task, _secondTask });
    
        var averageTimeLogged= await _markingStatsService.GetAvgWorkLogDurationByWeek(_user.Id, _project.Id, null);
        var expectedAvgTimeLogged = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
    
        averageTimeLogged.Should().Contain(expectedAvgTimeLogged);
    }
   
    public static IEnumerable<object[]> WeekDays => new List<object[]>
        {
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 11, 27)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 11, 28)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 11, 29)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 11, 30)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 12, 1)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 12, 2)),
            },
            new object[]
            {
                DateOnly.FromDateTime(new DateTime(2023, 12, 3)),
            },
        };

    [Fact]
    public async Task GetShortestWorklogDurationByWeek_SprintHasNoLogs_ReturnsEmptyTimeSpans()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetShortestWorklogDurationByWeek_ProjectHasNoLogs_ReturnsEmptyTimeSpans()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(1), DateOnly.FromDayNumber(14));
        await SaveEntries(new List<Sprint> { _sprint });

        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id);
        var expectedHours = new List<WeeklyTimeSpan>
        {
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekOne),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    private async Task SetupForShortestWorklogTests()
    {
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project, DateOnly.FromDayNumber(0), DateOnly.FromDayNumber(14));
        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _sprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        
        await SaveEntries(new List<Sprint> { _sprint });
        await SaveEntries(new List<UserStory> { _userStory });
        await SaveEntries(new List<UserStoryTask> { _task });
        await SaveEntries(new List<WorklogEntry> { _sprintWorkLogEntry });
        
        _sprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_workLogTags.First(), _sprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _sprintTaggedWorkInstance });
    }
    
    [Fact]
    public async Task GetShortestWorklogDurationByWeek_OneWorkLog_TimeSpanHasDurationOfLog()
    {
        await SetupForShortestWorklogTests();
        
        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetShortestWorklogDurationByWeek_TwoWorkLogs_TimeSpanHasDurationShortestLog()
    {
        await SetupForShortestWorklogTests();
        
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        await SaveEntries(new List<WorklogEntry> { _secondSprintWorkLogEntry });
        
        _secondSprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_workLogTags.First(), _secondSprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _secondSprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = long.Min(_sprintTaggedWorkInstance.Duration.Ticks, _secondSprintTaggedWorkInstance.Duration.Ticks),
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetShortestWorklogDurationByWeek_TwoWorkLogsInSeparateWeeks_TimeSpanHasLengthOfLogs()
    {
        await SetupForShortestWorklogTests();
        
        _secondSprintWorkLogEntry = FakeDataGenerator.CreateFakeWorkLogEntry(_user, _sprint, _task);
        _secondSprintWorkLogEntry.Occurred = _secondSprintWorkLogEntry.Occurred.AddDays(7);
        await SaveEntries(new List<WorklogEntry> { _secondSprintWorkLogEntry });
        
        _secondSprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_workLogTags.First(), _secondSprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _secondSprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _secondSprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate.AddDays(7)
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        
        hours.Should().Contain(expectedHours);
    }
    
    [Fact]
    public async Task GetShortestWorklogDurationByWeek_SeveralTaggedInstancesInOneLog_TimeSpanHasTotalLengthOfTaggedInstances()
    {
        await SetupForShortestWorklogTests();
        
        _secondSprintTaggedWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(_workLogTags.First(), _sprintWorkLogEntry);
        await SaveEntries(new List<TaggedWorkInstance> { _secondSprintTaggedWorkInstance });
        
        var hours = await _markingStatsService.GetShortestWorklogDurationByWeek(_user.Id, _project.Id, _sprint.Id);
        
        var expectedHours = new List<WeeklyTimeSpan>
        {
            new ()
            {
                SprintId = _sprint.Id,
                SprintName = _sprint.Name,
                Ticks = _sprintTaggedWorkInstance.Duration.Ticks + _secondSprintTaggedWorkInstance.Duration.Ticks,
                WeekStart = _sprint.StartDate
            },
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekTwo, _sprint),
            FakeDataGenerator.GenerateEmptyWeeklyTimespan(_weekThree, _sprint)
        };
        hours.Should().Contain(expectedHours);
    }

}