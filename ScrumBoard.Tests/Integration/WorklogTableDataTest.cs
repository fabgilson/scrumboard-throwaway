using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Filters;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration;

/// <summary>
/// Make a local class that inherits EditSprint so we can access its protected fields
/// </summary>
internal class WorklogTableDataUnderTest : WorklogTableData
{
    public WorklogTableDataUnderTest(       
        User self,      
        IWorklogEntryService worklogEntryService,
        WorklogEntryFilter worklogEntryFilter,
        Sprint sprint,
        ProjectState projectState,
        IProjectRepository projectRepository,
        ILogger<WorklogTableDataUnderTest> logger
    ) {   
        Self = self;
        CurrentSprint = sprint;   
        WorklogEntryFilter = worklogEntryFilter;
        WorklogEntryService = worklogEntryService;
        ProjectState = projectState;
        ProjectRepository = projectRepository;
        Logger = logger;
    }

    public async Task TriggerGetSummaryTimeSpent() {
        await GetSummaryTimeSpent();
    }

    public async Task TriggerOnParametersSetAsync()
    {
        await OnParametersSetAsync();
    }
}

public class WorklogTableDataTest : BaseIntegrationTestFixture
{
    private WorklogTableDataUnderTest _worklogTableDataUnderTest;

    private User _actingUser;

    private Sprint _currentSprint;

    private Sprint _anotherSprint;

    private Project _project;

    private UserStory _story;

    private UserStory _anotherStory;

    private UserStoryTask _userStoryTask;

    private UserStoryTask _anotherUserStoryTask;

    private readonly WorklogEntryFilter _worklogEntryFilter = new();

    public WorklogTableDataTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _actingUser = FakeDataGenerator.CreateFakeUser();
        _project = FakeDataGenerator.CreateFakeProject(developers: [_actingUser]);
        
        _currentSprint = FakeDataGenerator.CreateFakeSprint(_project);
        _anotherSprint = FakeDataGenerator.CreateFakeSprint(_project);
        
        _worklogTableDataUnderTest = ActivatorUtilities.CreateInstance<WorklogTableDataUnderTest>(
            ServiceProvider, _currentSprint, _actingUser, 
            new ProjectState{ ProjectId = _project.Id }, _worklogEntryFilter,
            ServiceProvider.GetRequiredService<IProjectRepository>(),
            ServiceProvider.GetRequiredService<ILogger<WorklogTableDataUnderTest>>()
        );
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        var testTag = FakeDataGenerator.CreateWorklogTag();
        dbContext.WorklogTags.Add(testTag);
        await dbContext.SaveChangesAsync();
        
        _story = new UserStory
        {     
            Id = 99,           
            CreatorId = _actingUser.Id,
            Created = DateTime.Now,
            Name = "First Story",
            Description = "A description",
            Estimate = 1,
            Priority = Priority.Normal,
            Stage = Stage.Todo,
            ProjectId = _project.Id,
            StoryGroupId = _currentSprint.Id
        };

        _userStoryTask = new UserStoryTask
        {
            Id = 202,
            Name= "Test task",                        
            Created = DateTime.Now, 
            CreatorId = _actingUser.Id,
            Priority= Priority.High,
            Stage= Stage.InProgress,
            Estimate = TimeSpan.FromHours(2),
            UserStoryId = _story.Id,           
            OriginalEstimate = TimeSpan.FromHours(2)
        };

        for (var i=0; i < 6; i++) {
            _userStoryTask.Worklog.Add(new WorklogEntry { 
                User = _actingUser,                
                TaskId = _userStoryTask.Id, 
                Description = "description",                  
                Created = DateTime.Now, 
                Occurred = DateTime.Now.AddHours(i), 
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(testTag, TimeSpan.FromMinutes(10)) }
            });
        }

        _anotherStory = new UserStory
        {  
            Id = 100,              
            CreatorId = _actingUser.Id,
            Created = DateTime.Now,
            Name = "First Story",
            Description = "A description",
            Estimate = 1,
            Priority = Priority.Normal,
            Stage = Stage.Todo,
            ProjectId = _project.Id,
            StoryGroupId = _anotherSprint.Id
        };

        _anotherUserStoryTask = new UserStoryTask {
            Id = 203,
            Name= "Test task 2",                        
            Created = DateTime.Now, 
            CreatorId = _actingUser.Id,
            Priority= Priority.Normal,
            Stage= Stage.InProgress,
            Estimate = TimeSpan.FromHours(3),
            UserStoryId = _anotherStory.Id,           
            OriginalEstimate = TimeSpan.FromHours(3)
        };

        for (var i=0; i < 6; i++) {
            _anotherUserStoryTask.Worklog.Add(new WorklogEntry() { 
                User = _actingUser,                
                TaskId = _anotherUserStoryTask.Id, 
                Description = "description",                  
                Created = DateTime.Now, 
                Occurred = DateTime.Now.AddHours(i), 
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(testTag, TimeSpan.FromMinutes(20)) }
            });
        }         
            
        dbContext.Users.Add(_actingUser);
        dbContext.Projects.Add(_project); 
        dbContext.Sprints.AddRange(_currentSprint, _anotherSprint);
        dbContext.UserStories.AddRange(_story, _anotherStory);    
        dbContext.UserStoryTasks.AddRange(_userStoryTask, _anotherUserStoryTask);
        await dbContext.SaveChangesAsync();
        _worklogTableDataUnderTest.TriggerOnParametersSetAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SprintSummaryLoaded_OnlyCurrentSprintSummarised() 
    {
        await _worklogTableDataUnderTest.TriggerGetSummaryTimeSpent();
        _worklogTableDataUnderTest.PersonalTime.Should().Contain("1h");
        _worklogTableDataUnderTest.FilteredUserTime.Should().Contain("1h");
    }

    [Fact]
    public async Task ProjectSummaryLoaded_WholeProjectSummarised() 
    {
        _worklogTableDataUnderTest.CurrentSprint = null;
        await _worklogTableDataUnderTest.TriggerGetSummaryTimeSpent();
        _worklogTableDataUnderTest.PersonalTime.Should().Contain("3h");
        _worklogTableDataUnderTest.FilteredUserTime.Should().Contain("3h");
    }
}