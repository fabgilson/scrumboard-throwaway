using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class ProjectStatsServiceTest : BaseIntegrationTestFixture
{    
    private readonly IProjectStatsService _projectStatsService;

    private Project _project;
    private User _user1;
    private WorklogTag _worklogTag;

    private UserStory _userStory;
    private UserStory _userStory2;

    private Sprint _sprint;
    private Sprint _sprint2;

    private UserStoryTask _task;
    private UserStoryTask _task2;

    private WorklogEntry _worklogEntry;
    private WorklogEntry _worklogEntry2; 
    private WorklogEntry _worklogEntryPair;

    public ProjectStatsServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _projectStatsService = ServiceProvider.GetRequiredService<IProjectStatsService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _project = FakeDataGenerator.CreateFakeProject();
        _user1 = new User { Id = 101, FirstName = "John", LastName = "Smith" };
        _worklogTag = FakeDataGenerator.CreateWorklogTag();
        
        await dbContext.Users.AddAsync(_user1);
        await dbContext.Projects.AddAsync(_project);
        await dbContext.WorklogTags.AddAsync(_worklogTag);

        await dbContext.ProjectUserMemberships.AddAsync(new ProjectUserMembership
        {
            Role = ProjectRole.Developer, UserId = _user1.Id, ProjectId = _project.Id, User = _user1, Project = _project
        });
        
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project);
        _sprint2 = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project);
        await dbContext.AddRangeAsync(_sprint, _sprint2);

        _userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        _userStory2 = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint2);
        await dbContext.AddRangeAsync(_userStory, _userStory2);

        _task = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory);
        _task2 = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(_userStory2);
        await dbContext.AddRangeAsync(_task, _task2);
        
        _worklogEntry = new WorklogEntry
        {
            Description = "Test Worklog Entry",
            UserId = _user1.Id,
            TaskId = _task.Id,
            TaggedWorkInstances = new [] { 
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_worklogTag, new TimeSpan(2)) 
            },
        };
        _worklogEntryPair = new WorklogEntry
        {
            Description = "This has a pair",
            UserId = _user1.Id,
            TaskId = _task.Id,
            TaggedWorkInstances = new [] { 
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_worklogTag, new TimeSpan(1)) 
            },
        };
        
        _worklogEntry2 = new WorklogEntry
        {
            Description = "This belongs in sprint 2",
            UserId = _user1.Id,
            TaskId = _task2.Id,
            TaggedWorkInstances = new [] { 
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_worklogTag, new TimeSpan(3)) 
            },
        };
        await dbContext.AddRangeAsync(_worklogEntry, _worklogEntryPair, _worklogEntry2);
        await dbContext.SaveChangesAsync();
    }
    
    [Fact]
    public async Task GetTimePerUserSprint_SprintHasTimeLogged_ReturnsTimePerUser() 
    {
        var result = await _projectStatsService.GetTimePerUser(_project.Id, _sprint.Id);
        var durationTotalHours = (_worklogEntry.GetTotalTimeSpent() + _worklogEntryPair.GetTotalTimeSpent()).TotalHours;
        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>> { new(0, _user1.GetFullName(), durationTotalHours) }, 
            durationTotalHours
        );
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetTimePerUserProject_ProjectHasTimeLogged_ReturnsTimePerUser() 
    {
        var result = await _projectStatsService.GetTimePerUser(_project.Id);
        var durationTotalHours = (_worklogEntry2.GetTotalTimeSpent() + _worklogEntry.GetTotalTimeSpent() + _worklogEntryPair.GetTotalTimeSpent()).TotalHours;
        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>> { new(0, _user1.GetFullName(), durationTotalHours) }, 
            durationTotalHours
        );
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetStoriesWorkedPerUserSprint_SprintHasUsersWorked_ReturnsStoriesWorkedPerUser() 
    {
        var result = await _projectStatsService.GetStoriesWorkedPerUser(_project.Id, _sprint.Id);
        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>> {  new(0, _user1.GetFullName(), 1) }, 
            1
        );
        
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetStoriesWorkedPerUserProject_ProjectHasUsersWorked_ReturnsStoriesWorkedPerUser()
    {
        var result = await _projectStatsService.GetStoriesWorkedPerUser(_project.Id);
        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>> { new(0, _user1.GetFullName(), 2) }, 
            2
        );
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetTasksWorkedOnPerUserSprint_SprintHasUsersWorked_ReturnsTasksWorkedPerUser() 
    {
        var result = await _projectStatsService.GetTasksWorkedOnPerUser(_project.Id, _sprint.Id);

        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>> { new(0, _user1.GetFullName(), 1) }, 
            1
        );
        result.Should().BeEquivalentTo(expected);
    }
}