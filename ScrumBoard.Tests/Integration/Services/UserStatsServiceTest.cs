using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class UserStatsServiceTest : BaseIntegrationTestFixture
{
    private readonly WorklogTag _feature  = new() { Name = "Feature",  Id = 1 };
    private readonly WorklogTag _test     = new() { Name = "Test",     Id = 2 };
    private readonly WorklogTag _chore    = new() { Name = "Chore",    Id = 3 };
    private readonly WorklogTag _document = new() { Name = "Document", Id = 4 };
    private readonly WorklogTag _fix      = new() { Name = "Fix",      Id = 5 };
    private readonly WorklogTag _review      = new() { Name = "Review",      Id = 6 };

    private readonly OverheadSession _planning1 = new() {Name = "Planning 1", Id = 8};
    private readonly OverheadSession _planning2 = new() {Name = "Planning 2", Id = 9};
    
    private readonly User _johnSmith = new() {Id = 101, FirstName = "John", LastName = "Smith"};
    private readonly User _pairAndWorkUser = new() {Id = 102};
    private readonly User _pairUser = new() {Id = 103};
    private readonly User _guest = new() { Id = 1003 };
    private readonly User _reviewer = new() { Id = 1002 };

    private readonly Project _theProject;
    private readonly Project _theSecondProject;
    
    private readonly Sprint _firstSprint;
    private readonly Sprint _secondSprint;
    private readonly Sprint _secondProjectFirstSprint;

    private readonly Backlog _backlog;
    private readonly Backlog _backlogSecondProject;

    private readonly WorklogTag[] _tags;
    private readonly WorklogEntry[] _worklog;
    private readonly UserStoryTask[] _tasks;
    private readonly UserStory[] _stories;
    private readonly User[] _users;
    private readonly UserTaskAssociation[] _associations;
    private readonly ProjectUserMembership[] _memberships;
    private readonly OverheadEntry[] _overheadEntries;
    private readonly OverheadSession[] _overheadSessions;
    
    private readonly IUserStatsService _userStatsService;

    public UserStatsServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _theProject = FakeDataGenerator.CreateFakeProject();
        _theSecondProject = FakeDataGenerator.CreateFakeProject();
        
        _firstSprint = FakeDataGenerator.CreateFakeSprint(_theProject);
        _secondSprint = FakeDataGenerator.CreateFakeSprint(_theProject);
        _backlog = new Backlog { Id = FakeDataGenerator.NextId, BacklogProjectId = _theProject.Id };
        _backlogSecondProject = new Backlog { Id = FakeDataGenerator.NextId, BacklogProjectId = _theSecondProject.Id };
        _secondProjectFirstSprint = FakeDataGenerator.CreateFakeSprint(_theSecondProject);

        UserStory secondProjectFirstSprintStoryOne = new() 
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Story 1 (Second Project)", 
            Description = "", 
            StoryGroupId = _secondProjectFirstSprint.Id, 
            ProjectId = _theSecondProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory firstSprintStoryOne = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Story 1", 
            Description = "", 
            StoryGroupId = _firstSprint.Id, 
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory firstSprintStoryTwo = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Story 2", 
            Description = "", 
            StoryGroupId = _firstSprint.Id, 
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory firstSprintStoryThree = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Story 3",
            Description = "",
            StoryGroupId = _firstSprint.Id,
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory secondSprintStoryOne = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Second story 1",
            Description = "",
            StoryGroupId = _secondSprint.Id,
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory secondSprintStoryTwo = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Second Story 2", 
            Description = "", 
            StoryGroupId = _secondSprint.Id, 
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStory secondSprintStoryThree = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Second Story 3",
            Description = "",
            StoryGroupId = _secondSprint.Id,
            ProjectId = _theProject.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskWorked = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Task 1", 
            UserStoryId = firstSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask secondProjectTaskWorked = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Task 1 (Second project)", 
            UserStoryId = secondProjectFirstSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskWorkedTwo = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Task 2", 
            UserStoryId = firstSprintStoryTwo.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskReviewedToDo = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 4",
            Stage = Stage.Deferred,
            UserStoryId = firstSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskReviewedInProgress = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 3",
            Stage = Stage.Done,
            UserStoryId = firstSprintStoryThree.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskReviewedDone = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 3",
            Stage = Stage.Done,
            UserStoryId = firstSprintStoryThree.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskReviewedDeferred = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 4",
            Stage = Stage.Deferred,
            UserStoryId = firstSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };

        UserStoryTask taskWorkedSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Task 1", 
            UserStoryId = secondSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };       
        UserStoryTask taskWorkedTwoSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId, 
            Name = "Task 2", 
            UserStoryId = secondSprintStoryTwo.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };        
        UserStoryTask taskReviewedDoneSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 3",
            Stage = Stage.Done,
            UserStoryId = secondSprintStoryThree.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };
        UserStoryTask taskReviewedDeferredSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Name = "Task 4",
            Stage = Stage.Deferred,
            UserStoryId = secondSprintStoryOne.Id,
            CreatorId = FakeDataGenerator.DefaultUserId
        };

        WorklogEntry worklogJohnFeatureSecondProjectFirstSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "First sprint worklog 1 (Second Project)",
            UserId = _johnSmith.Id, 
            TaskId = secondProjectTaskWorked.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_feature, TimeSpan.FromHours(3)) }, 
            PairUserId = null
        };
        WorklogEntry worklogJohnFeatureSecondProjectFirstSprintWithPair = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "First paired sprint worklog 1 (Second Project)",
            UserId = _johnSmith.Id, 
            TaskId = secondProjectTaskWorked.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_feature, TimeSpan.FromHours(2.5)) }, 
            PairUserId = _pairUser.Id
        };
        WorklogEntry worklogJohnChoreSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog 1",
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedSecondSprint.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(1)) }, 
            PairUserId = _pairAndWorkUser.Id
        };
        WorklogEntry worklogJohnReviewSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog Review",
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedSecondSprint.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(1)) }, 
        };
        WorklogEntry worklogJohnReviewDocumentSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog Review & Document",
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedSecondSprint.Id,
            TaggedWorkInstances = new []
            {
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(0.5)),
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_document, TimeSpan.FromHours(0.5))
            }, 
        };
        var worklogJohnReviewFirstSprintDeferred = new WorklogEntry()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog Review",
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDeferred.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(1)) }, 
        };
        var worklogJohnReviewFirstSprintDone = new WorklogEntry()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog Review & Document",
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDone.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(1)) }, 
        };
        var worklogJohnReviewFirstSprintInProgress = new WorklogEntry()
        {
            Id = FakeDataGenerator.NextId,
            Description = "First sprint review, back to in progress",
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedInProgress.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(1)) }, 
        };
        var worklogJohnReviewFirstSprintToDo = new WorklogEntry()
        {
            Id = FakeDataGenerator.NextId,
            Description = "First sprint review, back to todo",
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedToDo.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_review, TimeSpan.FromHours(1)) }, 
        };
        
        WorklogEntry worklogJohnDocumentSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint worklog Document",
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedSecondSprint.Id,
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_document, TimeSpan.FromHours(1)) }, 
        };
        WorklogEntry worklogJohnTestChoreSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId, 
            Description = "Second sprint Worklog 2", 
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDoneSecondSprint.Id, 
            TaggedWorkInstances = new []
            {
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_document, TimeSpan.FromHours(1.5)),
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(1.5))
            }, 
            PairUserId = _pairUser.Id
        };
        WorklogEntry worklogJohnTaskTwoFeatureTestSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint Worklog 3", 
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedTwoSecondSprint.Id, 
            TaggedWorkInstances = new []
            {
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_test, TimeSpan.FromHours(1)),
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_feature, TimeSpan.FromHours(1))
            }, 
            PairUserId = null,
        };
        WorklogEntry worklogNotJohnSecondSprint = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Second sprint Worklog 5", 
            UserId = _pairUser.Id,
            TaskId = taskWorkedTwoSecondSprint.Id, 
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(8)) }, 
            PairUserId = null
        };
        
       WorklogEntry worklogJohnChore = new()
       {
           Id = FakeDataGenerator.NextId,
           Description = "Worklog 1",
           UserId = _johnSmith.Id, 
           TaskId = taskWorked.Id,
           TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(2)) }, 
           PairUserId = _pairAndWorkUser.Id
       };
        WorklogEntry worklogJohnTestChore = new()
        {
            Id = FakeDataGenerator.NextId, 
            Description = "Worklog 2", 
            UserId = _johnSmith.Id, 
            TaskId = taskWorked.Id, 
            TaggedWorkInstances = new []
            {
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_test, TimeSpan.FromHours(2.5)),
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(2.5))
            }, 
            PairUserId = _pairUser.Id
        };
        WorklogEntry worklogJohnTaskTwoFeatureTest = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Worklog 3", 
            UserId = _johnSmith.Id, 
            TaskId = taskWorkedTwo.Id, 
            TaggedWorkInstances = new []
            {
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_test, TimeSpan.FromHours(1.5)),
                FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_feature, TimeSpan.FromHours(1.5))
            }, 
            PairUserId = null,
        };
        WorklogEntry worklogNotJohn = new()
        {
            Id = FakeDataGenerator.NextId,
            Description = "Worklog 4", 
            UserId = _pairUser.Id,
            TaskId = taskWorkedTwo.Id, 
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_chore, TimeSpan.FromHours(10)) }, 
            PairUserId = null
        };
        
        
        OverheadEntry overheadNotJohn = new()
        {
            Description = "Planned a bit",
            UserId = _pairUser.Id,
            SprintId = _firstSprint.Id,
            Duration = TimeSpan.FromHours(2),
            SessionId  = _planning1.Id,
        };
        OverheadEntry overheadFirstSprint1 = new() 
        {
            Description = "Planned a bit",
            UserId = _johnSmith.Id,
            SprintId = _firstSprint.Id,
            Duration = TimeSpan.FromHours(1),
            SessionId  = _planning2.Id,
        };
        OverheadEntry overheadFirstSprint2 = new() 
        {
            Description = "Planned a little",
            UserId = _johnSmith.Id,
            SprintId = _firstSprint.Id,
            Duration = TimeSpan.FromHours(3),
            SessionId  = _planning1.Id,
        };
        OverheadEntry overheadSecondSprint = new() 
        {
            Description = "Planned a bit",
            UserId = _johnSmith.Id,
            SprintId = _secondSprint.Id,
            Duration = TimeSpan.FromHours(7),
            SessionId  = _planning2.Id,
        };
        

        UserTaskAssociation taskDoneAssign = new()
        {
            UserId = _pairUser.Id, 
            Role = TaskRole.Assigned,
            TaskId = taskReviewedDone.Id
        };

        UserTaskAssociation taskDeferredAssign = new()
        {
            UserId = _pairUser.Id,
            Role = TaskRole.Assigned, 
            TaskId = taskReviewedDeferred.Id
        };

        UserTaskAssociation taskDoneAssignSecondSprint = new()
        {
            UserId = _pairAndWorkUser.Id,
            Role = TaskRole.Assigned, 
            TaskId = taskReviewedDoneSecondSprint.Id
        };

        UserTaskAssociation johnReviewingDone = new()
        {
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDone.Id, 
            Role = TaskRole.Reviewer
        };

        UserTaskAssociation johnReviewingDeferred = new()
        {
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDeferred.Id,
            Role = TaskRole.Reviewer
        };

        UserTaskAssociation johnReviewingDoneSecondSprint = new()
        {
            UserId = _johnSmith.Id, 
            TaskId = taskReviewedDoneSecondSprint.Id, 
            Role = TaskRole.Reviewer
        };

        _tags = new[]
        {
            _chore,
            _document,
            _test,
            _feature,
            _fix,
            _review
        };
        _overheadSessions = new[]
        {
            _planning1,
            _planning2,
        };
        _worklog = new[]
        {
            worklogJohnChore, 
            worklogJohnTestChore, 
            worklogJohnTaskTwoFeatureTest, 
            worklogNotJohn,
            worklogJohnChoreSecondSprint, 
            worklogJohnTestChoreSecondSprint, 
            worklogJohnTaskTwoFeatureTestSecondSprint,
            worklogNotJohnSecondSprint,
            worklogJohnFeatureSecondProjectFirstSprint,
            worklogJohnFeatureSecondProjectFirstSprintWithPair,
            worklogJohnDocumentSecondSprint,
            worklogJohnReviewSecondSprint,
            worklogJohnReviewDocumentSecondSprint,
            worklogJohnReviewFirstSprintToDo,
            worklogJohnReviewFirstSprintInProgress,
            worklogJohnReviewFirstSprintDone,
            worklogJohnReviewFirstSprintDeferred
        };
        _overheadEntries = new[]
        {
            overheadFirstSprint1,
            overheadFirstSprint2,
            overheadSecondSprint,
            overheadNotJohn,
        };
        _tasks = new[]
        {
            taskWorked, 
            taskWorkedTwo,
            taskReviewedToDo,
            taskReviewedInProgress,
            taskReviewedDeferred,
            taskReviewedDone,
            taskWorkedSecondSprint, 
            taskWorkedTwoSecondSprint,
            taskReviewedDeferredSecondSprint,
            taskReviewedDoneSecondSprint,
            secondProjectTaskWorked,
        };
        _stories = new[]
        {
            firstSprintStoryOne,
            firstSprintStoryTwo,
            firstSprintStoryThree,
            secondSprintStoryOne,
            secondSprintStoryTwo,
            secondSprintStoryThree,
            secondProjectFirstSprintStoryOne,
        };
        _users = new[]
        {
            _johnSmith, 
            _pairUser,
            _pairAndWorkUser,
            _guest, 
            _reviewer
        };
        _associations = new[]
        {
            johnReviewingDeferred,
            johnReviewingDone,
            taskDeferredAssign,
            taskDoneAssignSecondSprint,
            taskDoneAssign,
            johnReviewingDoneSecondSprint
        };

        _memberships = new ProjectUserMembership[]
        {
            new() { ProjectId = _theProject.Id, UserId = _johnSmith.Id, Role = ProjectRole.Developer },
            new() { ProjectId = _theProject.Id, UserId = _pairUser.Id, Role = ProjectRole.Developer  },  
            new() { ProjectId = _theProject.Id, UserId = _pairAndWorkUser.Id, Role = ProjectRole.Developer },
            new() { ProjectId = _theProject.Id, UserId = _reviewer.Id, Role = ProjectRole.Reviewer },
            new() { ProjectId = _theProject.Id, UserId = _guest.Id, Role = ProjectRole.Guest },

            new() { ProjectId = _theSecondProject.Id, UserId = _johnSmith.Id, Role = ProjectRole.Developer },
            new() { ProjectId = _theSecondProject.Id, UserId = _pairUser.Id, Role = ProjectRole.Developer },
        };

        _userStatsService = ServiceProvider.GetRequiredService<IUserStatsService>();
        GetDbContextFactory();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        dbContext.OverheadSessions.AddRange(_overheadSessions);
        dbContext.WorklogTags.AddRange(_tags);
        dbContext.Users.AddRange(_users);
        dbContext.Projects.Add(_theProject);  
        dbContext.Projects.Add(_theSecondProject);      
        dbContext.Sprints.AddRange(_firstSprint, _secondSprint, _secondProjectFirstSprint);
        dbContext.Sprints.Add(_secondSprint);
        dbContext.OverheadEntries.AddRange(_overheadEntries);
        dbContext.Backlogs.Add(_backlog);
        dbContext.Backlogs.Add(_backlogSecondProject);
        dbContext.UserStories.AddRange(_stories);
        dbContext.UserStoryTasks.AddRange(_tasks);
        dbContext.UserTaskAssociations.AddRange(_associations);
        dbContext.ProjectUserMemberships.AddRange(_memberships);
        await dbContext.SaveChangesAsync();

        var worklogEntryService = ServiceProvider.GetRequiredService<IWorklogEntryService>();
        foreach (var worklogEntry in _worklog)
        {
            await worklogEntryService.CreateWorklogEntryAsync(
                new WorklogEntryForm(worklogEntry, DateTime.Now), 
                worklogEntry.UserId,
                worklogEntry.TaskId,
                pairId: worklogEntry.PairUserId,
                taggedWorkInstanceForms: worklogEntry.TaggedWorkInstances.Select(x => 
                    new TaggedWorkInstanceForm{ Duration = x.Duration, WorklogTagId = x.WorklogTagId})
                );
        }
        
        await dbContext.SaveChangesAsync();
    }

    [Theory]
    [InlineData("FirstProject", 23, 41)]
    [InlineData("SecondProject", 5.5, 5.5)]
    [InlineData("FirstSprint", 14, 24)]
    [InlineData("SecondSprint", 9, 17)]
    public async Task GetTimeWorkedForProject_UserHasTimeLoggedInMultipleProjects_ReturnsCorrectTimeLogged(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        Project aProject = _theProject;
        if (sprint == "SecondProject") {
            aProject = _theSecondProject;
        }
        var result = await _userStatsService.TimeWorked(
            _johnSmith, 
            aProject,
            aSprint
        );
        var expected = new TimeWorked(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 5, 6)]
    [InlineData("FirstSprint", 2, 3)]
    [InlineData("SecondSprint", 3, 3)]
    public async Task GetStoriesWorked_UserLoggedToStories_ReturnsCorrectStoryCount(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.StoriesWorked(
            _johnSmith,
            _theProject,
            aSprint
        );
        var expected = new StoriesWorked(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 9, 10)]
    [InlineData("FirstSprint", 6, 6)]
    [InlineData("SecondSprint", 3, 4)]
    public async Task GetTasksWorked_UserLoggedToTasks_ReturnsCorrectTaskCount(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.TasksWorked(
            _johnSmith,
            _theProject,
            aSprint
        );
        var expected = new TasksWorked(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 5, 5)]
    [InlineData("FirstSprint", 4, 4)]
    [InlineData("SecondSprint", 1, 1)]
    public async Task GetTasksReviewed_UserHasWorklogWithReviewTagForTask_ReturnsCorrectTaskCount(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.TasksReviewed(_johnSmith, _theProject, aSprint);
        var expected = new TasksReviewed(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 3, 3)]
    [InlineData("FirstSprint", 2, 2)]
    [InlineData("SecondSprint", 1, 1)]
    public async Task GetStoriesReviewed_UserHasWorklogWithReviewTagForTask_ReturnsCorrectStoryCount(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.StoriesReviewed(_johnSmith, _theProject, aSprint);
        var expected = new StoriesReviewed(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }
    
    [Theory]
    [InlineData("Project", 23, 34)]
    [InlineData("FirstSprint", 14, 18)]
    [InlineData("SecondSprint", 9, 16)]
    public async Task GetWorkEfficiency_UserAssignedToDoneOrDeferredTasks_ReturnsCorrectEfficiency(string sprint, double expectedValue, double expectedPopulation)
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.WorkEfficiency(
            _johnSmith,
            _theProject,
            aSprint
        );
        var expected = new WorkEfficiency(expectedValue, expectedPopulation, aSprint != null);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 3, 7, 2.5, 0, 5, 5.5, 23)]
    [InlineData("FirstSprint", 0, 4.5, 1.5, 0, 4, 4, 14)]
    [InlineData("SecondSprint", 3, 2.5, 1, 0, 1, 1.5, 9)]
    public async Task GetTagsWorked_UserLoggedToStoriesWithTags_ReturnsExpectedResult(
        string sprint, 
        double expectedDocumentTagHours, 
        double expectedChoreTagHours,
        double expectedFeatureTagHours,
        double expectedFixTagHours,
        double expectedTestTagHours,
        double expectedReviewTagHours,
        double expectedTotal
    )
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.TagsWorked(
            _johnSmith,
            _theProject,
            aSprint
        );
        var expected = new StatsBar(
            new List<ProgressBarChartSegment<double>>
            {
                new(_document.Id, _document.Name, expectedDocumentTagHours), 
                new(_chore.Id, _chore.Name, expectedChoreTagHours),
                new(_feature.Id, _feature.Name, expectedFeatureTagHours), 
                new(_fix.Id, _fix.Name, expectedFixTagHours), 
                new(_test.Id, _test.Name, expectedTestTagHours),
                new(_review.Id, _review.Name, expectedReviewTagHours),
            }.Where(x => x.Data != 0).ToList(), 
            expectedTotal
        );
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("FirstProject", 8, 11, 3, 11)]
    [InlineData("SecondProject", 2.5, 2.5, 0, 0)]
    [InlineData("FirstSprint", 5, 7, 2, 7)]
    [InlineData("SecondSprint", 3, 4, 1, 4)]
    public async Task GetPairRankings_UserLoggedWithPairsAcrossMultipleProjects_ReturnsExpectedRanking(
        string sprint, 
        double expectedValuePairUser, 
        double expectedPopulationPairUser,
        double expectedValuePairAndWorkUser, 
        double expectedPopulationPairAndWorkUser
    )
    {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        Project aProject = _theProject;
        if (sprint == "SecondProject") {
            aProject = _theSecondProject;
        }
        var result = await _userStatsService.PairRankings(
            _johnSmith.Id,
            aProject.Id,
            aSprint?.Id
        );
        var expected = new List<TimePairedForUser>
        {
            new(_pairUser, expectedValuePairUser, expectedPopulationPairUser)
        };
        if (sprint != "SecondProject") { // PairAndWorkUser is not part of second project
            expected.Add(new(_pairAndWorkUser, expectedValuePairAndWorkUser, expectedPopulationPairAndWorkUser));
        }
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Project", 2, 5, 0, 5)]
    [InlineData("FirstSprint", 2, 4, 0, 4)]
    [InlineData("SecondSprint", 0, 1, 0, 1)]
    public async Task GetReviewedRankings_ReviewsForTeamMates_ReturnsExpectedRanking(
        string sprint, 
        double expectedValuePairUser, 
        double expectedPopulationPairUser,
        double expectedValuePairAndWorkUser, 
        double expectedPopulationPairAndWorkUser
    ) {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.ReviewCountOfUserForTeamMates(
            _johnSmith.Id,
            _theProject.Id,
            aSprint?.Id
        );
        var expected = new List<TasksReviewedForUser>
        {
            new(_pairUser, expectedValuePairUser, expectedPopulationPairUser),
            new(_pairAndWorkUser, expectedValuePairAndWorkUser, expectedPopulationPairAndWorkUser)
        };
        result.Should().BeEquivalentTo(expected);
    }
    
    [Theory]
    [InlineData("Project", 2, 2, 0, 2)]
    [InlineData("FirstSprint", 2, 2, 0, 2)]
    [InlineData("SecondSprint", 0, 0, 0, 0)]
    public async Task GetReviewedRankings_ReviewsFromTeamMates_ReturnsExpectedRanking(
        string sprint, 
        double expectedValueJohn, 
        double expectedPopulationJohn,
        double expectedValuePairAndWorkUser, 
        double expectedPopulationPairAndWorkUser
    ) {
        Sprint aSprint = null;
        if (sprint == "FirstSprint") {
            aSprint = _firstSprint;
        } else if (sprint == "SecondSprint") {
            aSprint = _secondSprint;
        }
        var result = await _userStatsService.ReviewCountOfUserFromTeamMates(
            _pairUser.Id,
            _theProject.Id,
            aSprint?.Id
        );
        var expected = new List<TasksReviewedForUser>
        {
            new(_johnSmith, expectedValueJohn, expectedPopulationJohn),
            new(_pairAndWorkUser, expectedValuePairAndWorkUser, expectedPopulationPairAndWorkUser)
        };
        result.Should().BeEquivalentTo(expected);
    }
}