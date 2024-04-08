using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class WeeklyReflectionCheckInServiceTest : BaseIntegrationTestFixture
{
    private static readonly DateTime TestNow = new (2024, 2, 6);
    private static readonly int CurrentIsoWeek = ISOWeek.GetWeekOfYear(TestNow);
    private static readonly int CurrentIsoYear = ISOWeek.GetYear(TestNow);
    
    private Project _project;
    private Sprint _sprintStartedTwoWeeksAgo;
    private UserStory _story;
    private UserStoryTask _userStoryTask;
    private WorklogTag _featureTag, _testTag;
    private WeeklyReflectionCheckIn _weeklyReflectionCheckIn;

    private readonly IWeeklyReflectionCheckInService _weeklyReflectionCheckInService;

    private static Func<EquivalencyAssertionOptions<WeeklyReflectionCheckIn>, EquivalencyAssertionOptions<WeeklyReflectionCheckIn>> IgnoreNavigations => 
        checkIn => checkIn.Excluding(x => x.Project).Excluding(x => x.User);

    public WeeklyReflectionCheckInServiceTest(TestWebApplicationFactory factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
    {
        _weeklyReflectionCheckInService = ServiceProvider.GetRequiredService<IWeeklyReflectionCheckInService>();
        ClockMock.Setup(x => x.Now).Returns(TestNow);
    }
    
    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _featureTag = new WorklogTag { Name = "Feature", Style = BadgeStyle.Light };
        _testTag = new WorklogTag { Name = "Test", Style = BadgeStyle.Light };
        await dbContext.AddRangeAsync(_featureTag, _testTag);
        
        _project = FakeDataGenerator.CreateFakeProject(developers: [DefaultUser]);
        await dbContext.AddAsync(_project);

        _sprintStartedTwoWeeksAgo = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project);
        _sprintStartedTwoWeeksAgo.StartDate = DateOnly.FromDateTime(TestNow.AddDays(-2 * 7));
        _sprintStartedTwoWeeksAgo.EndDate = DateOnly.FromDateTime(TestNow.AddDays(7));
        await dbContext.AddAsync(_sprintStartedTwoWeeksAgo);

        _story = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprintStartedTwoWeeksAgo);
        await dbContext.AddAsync(_story);

        _userStoryTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_story);
        await dbContext.AddAsync(_userStoryTask);
        
        _weeklyReflectionCheckIn = FakeDataGenerator.CreateWeeklyReflectionCheckIn(_project, DefaultUser, CurrentIsoWeek, CurrentIsoYear, true);
        await dbContext.AddAsync(_weeklyReflectionCheckIn);

        await dbContext.SaveChangesAsync();
    }

    private async Task<WeeklyReflectionCheckIn> GetOnlyCheckInFromDb()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.WeeklyReflectionCheckIns.SingleAsync();
    }
    
    private async Task<ICollection<WeeklyReflectionCheckInChangelogEntry>> GetAllWeeklyReflectionChangelogs()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.WeeklyReflectionCheckInChangelogEntries.ToListAsync();
    }

    private async Task ClearAnyExistingWeeklyOrTaskCheckIns()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.TaskCheckIns.RemoveRange(context.TaskCheckIns);
        context.WeeklyReflectionCheckIns.RemoveRange(context.WeeklyReflectionCheckIns);
        await context.SaveChangesAsync();
    }
    
    private async Task<ICollection<UserStoryTask>> GetTasksWorkedOn(bool includeTasksAssigned)
    {
        return await _weeklyReflectionCheckInService.GetTasksWorkedOrAssignedToUserForIsoWeekAndYear(
            DefaultUser.Id, 
            _project.Id,
            CurrentIsoWeek,
            CurrentIsoYear,
            includeTasksAssigned
        );
    }

    private async Task AssignUserToTask(User user, UserStoryTask task)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var association = new UserTaskAssociation { UserId = user.Id, TaskId = task.Id };
        await context.AddAsync(association);
        await context.SaveChangesAsync();
    }

    private async Task AddWorkToTask(User user, UserStoryTask task, DateTime workOccurred, double workHours=1.5, WorklogTag tag=null)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = FakeDataGenerator.CreateFakeWorkLogEntry(user, _sprintStartedTwoWeeksAgo, task, workOccurred);
        await context.AddAsync(worklog);
        await context.SaveChangesAsync();

        var taggedWork = FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(tag ?? _featureTag, worklog, TimeSpan.FromHours(workHours));
        await context.AddAsync(taggedWork);
        await context.SaveChangesAsync();
    }

    private async Task<WeeklyReflectionCheckIn> CreateWeeklyCheckIn(
        Project project,
        User user,
        string didWellComments,
        string didNotDoWellComments="",
        string willDoDifferentlyComments="",
        string anythingElseComments="",
        CheckInCompletionStatus completionStatus=CheckInCompletionStatus.Incomplete,
        int? isoWeek=null,
        int? year=null,
        bool saveToDb=true
    ) {
        var weeklyCheckIn = new WeeklyReflectionCheckIn
        {
            UserId = user?.Id ?? default,
            ProjectId = project?.Id ?? default,
            WhatIDidWell = didWellComments,
            WhatIDidNotDoWell = didNotDoWellComments,
            WhatIWillDoDifferently = willDoDifferentlyComments,
            AnythingElse = anythingElseComments,
            CompletionStatus = completionStatus,
            IsoWeekNumber = isoWeek ?? CurrentIsoWeek,
            Year = year ?? CurrentIsoYear
        };
        if (!saveToDb) return weeklyCheckIn;
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.AddAsync(weeklyCheckIn);
        await context.SaveChangesAsync();
        return weeklyCheckIn;
    }
    
    private async Task<TaskCheckIn> CreateTaskCheckIn(
        UserStoryTask task, 
        // ReSharper disable once SuggestBaseTypeForParameter
        WeeklyReflectionCheckIn weeklyReflectionCheckIn, 
        CheckInTaskDifficulty difficulty=CheckInTaskDifficulty.None,
        CheckInTaskStatus status=CheckInTaskStatus.None,
        bool saveToDb=true
    ) {
        var taskCheckIn = new TaskCheckIn
        {
            WeeklyReflectionCheckInId = weeklyReflectionCheckIn.Id,
            TaskId = task.Id,
            CheckInTaskDifficulty = difficulty,
            CheckInTaskStatus = status
        };
        if (!saveToDb) return taskCheckIn;
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.AddAsync(taskCheckIn);
        await context.SaveChangesAsync();
        taskCheckIn.WeeklyReflectionCheckIn = weeklyReflectionCheckIn;
        return taskCheckIn;
    }
    
    [Fact]
    private async Task GetCheckInForUserForIsoWeekAndYear_NoSuchCheckIn_NullReturned()
    {
        var actual = await _weeklyReflectionCheckInService.GetCheckInForUserForIsoWeekAndYear(
            DefaultUser.Id, 
            _project.Id, 
            1, 
            1
        );
        actual.Should().BeNull();
    }
    
    [Fact]
    private async Task GetCheckInForUserForIsoWeekAndYear_CheckInExists_CheckInReturned()
    { 
        var actual = await _weeklyReflectionCheckInService.GetCheckInForUserForIsoWeekAndYear(
            DefaultUser.Id, 
            _project.Id, 
            _weeklyReflectionCheckIn.IsoWeekNumber, 
            _weeklyReflectionCheckIn.Year
        );
        actual.Should().BeEquivalentTo(_weeklyReflectionCheckIn, IgnoreNavigations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private async Task GetAllCheckInsForUser_SomeCheckInsWithinSprintOthersNot_OnlyCorrectCheckInsReturned(bool limitToSprint)
    {
        await ClearAnyExistingWeeklyOrTaskCheckIns();
        var checkInThisWeek = await CreateWeeklyCheckIn(_project, DefaultUser, "'This' week", isoWeek: CurrentIsoWeek, year: CurrentIsoYear, saveToDb: true);
        var checkInLastWeek = await CreateWeeklyCheckIn(_project, DefaultUser, "'Last' week", isoWeek: CurrentIsoWeek - 1, year: CurrentIsoYear, saveToDb: true);
        var checkInBeforeSprint = await CreateWeeklyCheckIn(_project, DefaultUser, "Before sprint", isoWeek: CurrentIsoWeek - 3, year: CurrentIsoYear, saveToDb: true);
        var checkInAfterSprint = await CreateWeeklyCheckIn(_project, DefaultUser, "After sprint", isoWeek: CurrentIsoWeek + 3, year: CurrentIsoYear, saveToDb: true);

        var checkInsInSprint = (await _weeklyReflectionCheckInService
            .GetAllCheckInsForUserForProjectAsync(_project.Id, limitToSprint ? _sprintStartedTwoWeeksAgo.Id : null, DefaultUser.Id)).ToList();

        var excludingTaskCheckIns = (EquivalencyAssertionOptions<WeeklyReflectionCheckIn> e) => e.Excluding(x => x.TaskCheckIns);
        
        checkInsInSprint.Should().HaveCount(limitToSprint ? 2 : 4);
        
        checkInsInSprint.Should().ContainEquivalentOf(checkInThisWeek, excludingTaskCheckIns);
        checkInsInSprint.Should().ContainEquivalentOf(checkInLastWeek, excludingTaskCheckIns);
        
        if (limitToSprint)
        {
            checkInsInSprint.Should().NotContainEquivalentOf(checkInBeforeSprint, excludingTaskCheckIns);
            checkInsInSprint.Should().NotContainEquivalentOf(checkInAfterSprint, excludingTaskCheckIns);
        }
        else
        {
            checkInsInSprint.Should().ContainEquivalentOf(checkInBeforeSprint, excludingTaskCheckIns);
            checkInsInSprint.Should().ContainEquivalentOf(checkInAfterSprint, excludingTaskCheckIns);
        }
    }
    

    [Fact]
    private async Task GetTaskCheckInAsync_NoSuchTaskCheckIn_NullReturned()
    {
        var result = await _weeklyReflectionCheckInService.GetTaskCheckInAsync(_weeklyReflectionCheckIn.Id, _userStoryTask.Id);
        
        result.Should().BeNull();
    }

    [Fact]
    private async Task GetTaskCheckInAsync_TaskCheckInExists_TaskCheckInReturned()
    {
        await CreateTaskCheckIn(_userStoryTask, _weeklyReflectionCheckIn);
        
        var result = await _weeklyReflectionCheckInService.GetTaskCheckInAsync(_weeklyReflectionCheckIn.Id, _userStoryTask.Id);
        
        result.Should().NotBeNull();
        result.WeeklyReflectionCheckInId.Should().Be(_weeklyReflectionCheckIn.Id);
        result.TaskId.Should().Be(_userStoryTask.Id);
    }
    
    [Fact]
    private async Task GetTasksWorked_NoTasksHaveBeenWorkedOn_EmptyCollectionReturned()
    {
        var results = await GetTasksWorkedOn(false);
        results.Should().BeEmpty();
    }

    [Fact]
    private async Task GetTasksWorkedOrAssigned_NoneWorkedButOneAssigned_AssignedTaskReturned()
    {
        await AssignUserToTask(DefaultUser, _userStoryTask);
        var results = await GetTasksWorkedOn(true);
        results.Should().ContainSingle().Which.Id.Should().Be(_userStoryTask.Id);
    }

    [Fact]
    private async Task GetTasksWorked_OneWorkedOnThisWeek_WorkedOnTaskReturned()
    {
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow);
        var results = await GetTasksWorkedOn(false);
        results.Should().ContainSingle().Which.Id.Should().Be(_userStoryTask.Id);
    }
    
    [Fact]
    private async Task GetTimeSpentForTasksInCheckIn_NoCheckInsGiven_ZeroTimeReturned()
    {
        var timeSpent = await _weeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync([], DefaultUser.Id);
        timeSpent.Should().BeEmpty();
    }

    [Fact]
    private async Task GetTimeSpentForTasksInCheckIn_NoTimeSpent_ZeroTimeReturned()
    {
        var taskCheckIn = await CreateTaskCheckIn(_userStoryTask, _weeklyReflectionCheckIn);
        
        var timeSpent = await _weeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync([taskCheckIn], DefaultUser.Id);
        
        var (checkIn, time) = timeSpent.Should().ContainSingle().Which;
        checkIn.TaskId.Should().Be(_userStoryTask.Id);
        time.TotalTime.TotalHours.Should().Be(0);
        time.TimeSpentOnWorklogTags.Should().BeEmpty();
    }
    
    [Fact]
    private async Task GetTimeSpentForTasksInCheckIn_TimeSpentThisWeek_CorrectTimeAndTagsReturnedOnTask()
    {
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow, 1, _testTag);
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow, 2, _featureTag);
        var taskCheckIn = await CreateTaskCheckIn(_userStoryTask, _weeklyReflectionCheckIn);
        
        var timeSpent = await _weeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync([taskCheckIn], DefaultUser.Id);
        
        var (checkIn, time) = timeSpent.Should().ContainSingle().Which;
        checkIn.TaskId.Should().Be(_userStoryTask.Id);
        time.TotalTime.TotalHours.Should().Be(3);
        
        time.TimeSpentOnWorklogTags.Should().HaveCount(2);
        time.TimeSpentOnWorklogTags.Should().ContainSingle(x => x.Tag.Id == _testTag.Id && x.TimeSpent == TimeSpan.FromHours(1));
        time.TimeSpentOnWorklogTags.Should().ContainSingle(x => x.Tag.Id == _featureTag.Id && x.TimeSpent == TimeSpan.FromHours(2));
    }
    
    [Fact]
    private async Task GetTimeSpentForTasksInCheckIn_TimeOnlySpentLastWeek_ZeroTimeReturned()
    {
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow.AddDays(-7), 1, _testTag);
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow.AddDays(-7), 2, _featureTag);
        var taskCheckIn = await CreateTaskCheckIn(_userStoryTask, _weeklyReflectionCheckIn);
        
        var timeSpent = await _weeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync([taskCheckIn], DefaultUser.Id);
        
        var (checkIn, time) = timeSpent.Should().ContainSingle().Which;
        checkIn.TaskId.Should().Be(_userStoryTask.Id);
        time.TotalTime.TotalHours.Should().Be(0);
        time.TimeSpentOnWorklogTags.Should().BeEmpty();
    }
    
    [Fact]
    private async Task GetTimeSpentForTasksInCheckIn_TimeSpentLastWeekAndThisWeek_OnlyThisWeekTimeReturned()
    {
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow.AddDays(-7), 1, _testTag);
        await AddWorkToTask(DefaultUser, _userStoryTask, TestNow, 2, _featureTag);
        var taskCheckIn = await CreateTaskCheckIn(_userStoryTask, _weeklyReflectionCheckIn);
        
        var timeSpent = await _weeklyReflectionCheckInService.GetTimeSpentForTasksInCheckInsAsync([taskCheckIn], DefaultUser.Id);
        
        var (checkIn, time) = timeSpent.Should().ContainSingle().Which;
        checkIn.TaskId.Should().Be(_userStoryTask.Id);
        time.TotalTime.TotalHours.Should().Be(2);
        
        time.TimeSpentOnWorklogTags.Should().HaveCount(1);
        time.TimeSpentOnWorklogTags.Should().ContainSingle(x => x.Tag.Id == _featureTag.Id && x.TimeSpent == TimeSpan.FromHours(2));
    }

    [Fact]
    private async Task SaveWeeklyCheckIn_NewCheckIn_NewEntityCreated()
    {
        var newWeeklyCheckIn = await CreateWeeklyCheckIn(
            null, 
            null, 
            "did well comments",
            "did not do well comments",
            "do differently comments",
            "anything else comments",
            saveToDb: false
        );
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(newWeeklyCheckIn, DefaultUser.Id, _project.Id, null);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var dbWeeklyCheckIn = await context.WeeklyReflectionCheckIns.OrderBy(x => x.Id).LastAsync();

        dbWeeklyCheckIn.WhatIDidWell.Should().Be("did well comments");
        dbWeeklyCheckIn.WhatIDidNotDoWell.Should().Be("did not do well comments");
        dbWeeklyCheckIn.WhatIWillDoDifferently.Should().Be("do differently comments");
        dbWeeklyCheckIn.AnythingElse.Should().Be("anything else comments");
        dbWeeklyCheckIn.CompletionStatus.Should().Be(CheckInCompletionStatus.Incomplete);
        dbWeeklyCheckIn.UserId.Should().Be(DefaultUser.Id);
        dbWeeklyCheckIn.ProjectId.Should().Be(_project.Id);
        dbWeeklyCheckIn.Created.Should().Be(TestNow);
    }

    [Fact]
    private async Task SaveWeeklyCheckIn_ExistingCheckIn_ExistingEntityUpdated()
    {
        var newWeeklyCheckIn = await CreateWeeklyCheckIn(
            _project, 
            DefaultUser, 
            "did well comments",
            "did not do well comments",
            "do differently comments",
            "anything else comments"
        );
        newWeeklyCheckIn.WhatIDidWell = "Some new did well comments";
        newWeeklyCheckIn.CompletionStatus = CheckInCompletionStatus.Completed;
        var newNow = TestNow.AddDays(1);
        ClockMock.Setup(x => x.Now).Returns(newNow);
        
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(newWeeklyCheckIn, DefaultUser.Id, _project.Id, null);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var dbWeeklyCheckIn = await context.WeeklyReflectionCheckIns.OrderBy(x => x.Id).LastAsync();

        dbWeeklyCheckIn.WhatIDidWell.Should().Be("Some new did well comments");
        dbWeeklyCheckIn.WhatIDidNotDoWell.Should().Be("did not do well comments");
        dbWeeklyCheckIn.WhatIWillDoDifferently.Should().Be("do differently comments");
        dbWeeklyCheckIn.AnythingElse.Should().Be("anything else comments");
        dbWeeklyCheckIn.CompletionStatus.Should().Be(CheckInCompletionStatus.Completed);
        dbWeeklyCheckIn.UserId.Should().Be(DefaultUser.Id);
        dbWeeklyCheckIn.ProjectId.Should().Be(_project.Id);
        dbWeeklyCheckIn.LastUpdated.Should().Be(newNow);
    }

    [Fact]
    private async Task SaveTaskCheckIn_NewCheckIn_NewEntityCreated()
    {
        var newTaskCheckIn = await CreateTaskCheckIn(
            _userStoryTask, 
            _weeklyReflectionCheckIn, 
            CheckInTaskDifficulty.Medium,
            CheckInTaskStatus.CompletedPendingReview, 
            false
        );
        await _weeklyReflectionCheckInService.SaveTaskCheckInAsync(newTaskCheckIn, _weeklyReflectionCheckIn.Id);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var dbTaskCheckIn = await context.TaskCheckIns.SingleAsync();

        dbTaskCheckIn.CheckInTaskDifficulty.Should().Be(CheckInTaskDifficulty.Medium);
        dbTaskCheckIn.CheckInTaskStatus.Should().Be(CheckInTaskStatus.CompletedPendingReview);
        dbTaskCheckIn.WeeklyReflectionCheckInId.Should().Be(_weeklyReflectionCheckIn.Id);
        dbTaskCheckIn.Created.Should().Be(TestNow);
    }

    [Fact]
    private async Task SaveTaskCheckIn_ExistingCheckIn_ExistingEntityUpdated()
    {
        var existingTaskCheckIn = await CreateTaskCheckIn(
            _userStoryTask, 
            _weeklyReflectionCheckIn, 
            CheckInTaskDifficulty.Medium,
            CheckInTaskStatus.CompletedPendingReview
        );
        
        existingTaskCheckIn.CheckInTaskStatus = CheckInTaskStatus.Completed;
        existingTaskCheckIn.CheckInTaskDifficulty = CheckInTaskDifficulty.Hard;
        var newNow = TestNow.AddDays(1);
        ClockMock.Setup(x => x.Now).Returns(newNow);
        
        await _weeklyReflectionCheckInService.SaveTaskCheckInAsync(existingTaskCheckIn, _weeklyReflectionCheckIn.Id);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var dbTaskCheckIn = await context.TaskCheckIns.SingleAsync();

        dbTaskCheckIn.CheckInTaskDifficulty.Should().Be(CheckInTaskDifficulty.Hard);
        dbTaskCheckIn.CheckInTaskStatus.Should().Be(CheckInTaskStatus.Completed);
        dbTaskCheckIn.WeeklyReflectionCheckInId.Should().Be(_weeklyReflectionCheckIn.Id);
        dbTaskCheckIn.LastUpdated.Should().Be(newNow);
    }
    
    [Fact]
    private async Task SaveWeeklyReflection_NewlyCreatedReflection_ChangelogGeneratedCorrectly()
    {
        await ClearAnyExistingWeeklyOrTaskCheckIns();
        var newCheckIn = await CreateWeeklyCheckIn(_project, DefaultUser, "", "", "", saveToDb: false);
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(newCheckIn, DefaultUser.Id, _project.Id, null);

        var changelogs = await GetAllWeeklyReflectionChangelogs();

        var createdCheckIn = await GetOnlyCheckInFromDb();
        var soleChangelog = changelogs.Should().ContainSingle().Which;
        
        soleChangelog.WeeklyReflectionCheckInId.Should().Be(createdCheckIn.Id);
        soleChangelog.EntityType.Should().Be(typeof(WeeklyReflectionCheckIn));
        soleChangelog.FieldChanged.Should().BeNull();
        soleChangelog.Type.Should().Be(ChangeType.Create);
        soleChangelog.EditingSessionGuid.Should().BeNull();
    }

    [Theory]
    [CombinatorialData]
    private async Task SaveWeeklyReflection_AllPossibleCombinationsOfChanges_AllExpectedChangelogsGenerated(
        bool changeDidDoWell,
        bool changeDidNotDoWell,
        bool changeWillDoDifferently,
        bool changeAnythingElse,
        bool changeStatus
    ) {
        var checkIn = await CreateWeeklyCheckIn(_project, DefaultUser, "", "", "");
        
        if (changeDidDoWell) checkIn.WhatIDidWell = "Some new did do well comments";
        if (changeDidNotDoWell) checkIn.WhatIDidNotDoWell = "Some new did not do well comments";
        if (changeWillDoDifferently) checkIn.WhatIWillDoDifferently = "Some new will do differently comments";
        if (changeAnythingElse) checkIn.AnythingElse = "Some new anything else comments";
        if (changeStatus) checkIn.CompletionStatus = CheckInCompletionStatus.Completed;

        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, null);
        
        var expectedChangesCount = new[] { changeDidDoWell, changeDidNotDoWell, changeWillDoDifferently, changeAnythingElse, changeStatus }.Count(b => b);
        var changelogs = await GetAllWeeklyReflectionChangelogs();

        changelogs.Should().HaveCount(expectedChangesCount);
        if (changeDidDoWell) changelogs.Should().ContainSingle(x => x.FieldChanged == "WhatIDidWell");
        if (changeDidNotDoWell) changelogs.Should().ContainSingle(x => x.FieldChanged == "WhatIDidNotDoWell");
        if (changeWillDoDifferently) changelogs.Should().ContainSingle(x => x.FieldChanged == "WhatIWillDoDifferently");
        if (changeAnythingElse) changelogs.Should().ContainSingle(x => x.FieldChanged == "AnythingElse");
        if (changeStatus) changelogs.Should().ContainSingle(x => x.FieldChanged == "CompletionStatus");
    }
    
    [Fact]
    private async Task SaveWeeklyReflectionMultipleTimes_SameEditingSessionGuid_OnlyOneChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var checkIn = await CreateWeeklyCheckIn(_project, DefaultUser, "", "", "");

        checkIn.WhatIDidWell = "Some new comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        checkIn.WhatIDidWell = "Some even newer comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        checkIn.WhatIDidWell = "The newest comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        var changelogs = await GetAllWeeklyReflectionChangelogs();
        var soleChangelog = changelogs.Should().ContainSingle().Which;
        soleChangelog.FieldChanged.Should().Be("WhatIDidWell");
        soleChangelog.FromValue.Should().Be("");
        soleChangelog.ToValue.Should().Be("The newest comments");
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private async Task SaveWeeklyReflectionMultipleTimes_DifferentEditingSessionGuidsOrNoGuids_MultipleChangelogsCreated(bool guidIsNull)
    {
        var checkIn = await CreateWeeklyCheckIn(_project, DefaultUser, "", "", "");

        checkIn.WhatIDidWell = "Some new comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, guidIsNull ? null : Guid.NewGuid());

        checkIn.WhatIDidWell = "Some even newer comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, guidIsNull ? null : Guid.NewGuid());

        checkIn.WhatIDidWell = "The newest comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, guidIsNull ? null : Guid.NewGuid());

        var changelogs = await GetAllWeeklyReflectionChangelogs();
        changelogs.Should().HaveCount(3);

        changelogs.Should().ContainSingle(x => x.FromValue == "" && x.ToValue == "Some new comments");
        changelogs.Should().ContainSingle(x => x.FromValue == "Some new comments" && x.ToValue == "Some even newer comments");
        changelogs.Should().ContainSingle(x => x.FromValue == "Some even newer comments" && x.ToValue == "The newest comments");
    }
    
    [Fact]
    private async Task SaveWeeklyReflectionMultipleTimes_SameEditingSessionGuidAndFinishesOnOriginalValues_NoChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var checkIn = await CreateWeeklyCheckIn(_project, DefaultUser, "Starting comments", "", "");

        checkIn.WhatIDidWell = "Some new comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        checkIn.WhatIDidWell = "Some even newer comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        checkIn.WhatIDidWell = "Starting comments";
        await _weeklyReflectionCheckInService.SaveCheckInForUserAsync(checkIn, DefaultUser.Id, _project.Id, editingSessionGuid);

        var changelogs = await GetAllWeeklyReflectionChangelogs();
        var soleChangelog = changelogs.Should().BeEmpty();
    }
}