using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Shared.SprintBoard;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;
using FakeNavigationManager = Bunit.TestDoubles.FakeNavigationManager;

namespace ScrumBoard.Tests.Blazor;

public class SprintBoardComponentTest : TestContext
{
    private readonly ITestOutputHelper _output;

    public SprintBoardComponentTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly User ActingUser = new()
    {
        Id = 33,
        FirstName = "Jeff",
        LastName = "Geoff"
    };

    private static readonly User AnotherUser = new()
    {
        Id = 34,
        FirstName = "Bob",
        LastName = "Bobert"
    };

    private static readonly UserStoryTask FirstTask = new()
    {
        Id = 101,
        Name = "Task 1",
        Description = "Do this thing",
        Tags = new List<UserStoryTaskTag>(),
        Created = DateTime.Now,
        Creator = new User(),
        UserAssociations = new List<UserTaskAssociation>(),
        Priority = Priority.Low
    };

    private readonly UserTaskAssociation _taskAssociation = new()
    {
        TaskId = 101,
        UserId = ActingUser.Id,
        Role = TaskRole.Assigned,
        User = ActingUser,
        Task = FirstTask
    };

    private static readonly UserStoryTask SecondTask = new()
    {
        Id = 102,
        Name = "Task 2",
        Description = "Do this other thing",
        Tags = new List<UserStoryTaskTag>(),
        Stage = Stage.InProgress,
        Created = DateTime.Now,
        Creator = new User(),
        UserAssociations = new List<UserTaskAssociation>(),
        Priority = Priority.Low
    };

    private readonly UserTaskAssociation _secondTaskAssociation = new()
    {
        TaskId = 102,
        UserId = AnotherUser.Id,
        Role = TaskRole.Reviewer,
        User = AnotherUser,
        Task = SecondTask
    };

    private readonly UserStory _storyWithTask = new()
    {
        Id = 101,
        Name = "Story With Task",
        Description = "Story with task description",
        Priority = Priority.High,
        Stage = Stage.InProgress,
        Estimate = 10,
        AcceptanceCriterias = new List<AcceptanceCriteria>()
        {
            new() { Id = 12, Content = "AC1-Hello" }
        },
        Created = DateTime.Now,
        Creator = new User() { Id = 1, FirstName = "Firstname", LastName = "Lastname" },
        Tasks = new List<UserStoryTask>
        {
            FirstTask,
            SecondTask,
            new()
            {
                Id = 103,
                Name = "Task 3",
                Description = "Do another thing",
                Tags = new List<UserStoryTaskTag>(),
                Stage = Stage.UnderReview,
                Created = DateTime.Now,
                Creator = new User(),
                UserAssociations = new List<UserTaskAssociation>(),
                Priority = Priority.Low
            },
            new()
            {
                Id = 104,
                Name = "Task 4",
                Description = "Do this thing",
                Tags = new List<UserStoryTaskTag>(),
                Created = DateTime.Now,
                Creator = new User(),
                UserAssociations = new List<UserTaskAssociation>(),
                Priority = Priority.Low
            }
        }
    };

    private readonly UserStory _storyWithoutTask = new()
    {
        Id = 102,
        Priority = Priority.Low,
        Name = "Story Without Task",
        Stage = Stage.Todo,
        Tasks = new List<UserStoryTask>(),
        Description = "story without task description",
        Estimate = 200,
        AcceptanceCriterias = new List<AcceptanceCriteria>()
        {
            new() { Id = 1, Content = "AC1" }
        },
        Created = DateTime.Now,
        Creator = new User { Id = 1, FirstName = "Firstname", LastName = "Lastname" }
    };

    private ICollection<UserStory> _stories;
    private List<UserStoryTask> _tasks;

    private IRenderedComponent<SprintBoard> _component;

    // Mocks
    private Mock<IProjectRepository> _mockProjectRepository;

    private Mock<IScrumBoardStateStorageService> _mockStateStorageService;

    private Mock<IUserStoryRepository> _mockUserStoryRepository;

    private Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository;

    private Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository;

    private Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository;

    private Mock<IWorklogEntryService> _mockWorklogEntryService;

    private readonly Mock<IJsInteropService> _mockJsInteropService = new();

    private IRenderedComponent<SprintBoard> CreateComponent()
    {
        FirstTask.UserAssociations.Add(_taskAssociation);
        SecondTask.UserAssociations.Add(_secondTaskAssociation);
        long fakeProjectId = 1;
        _stories = new List<UserStory> { _storyWithTask, _storyWithoutTask };
        Project fakeProject = new() { Id = fakeProjectId };

        Sprint fakeSprint = new() { Project = fakeProject, Stage = SprintStage.Started, TimeStarted = DateTime.Now };
        fakeProject.Sprints.Add(fakeSprint);

        _tasks = _stories.SelectMany(story => story.Tasks).ToList();

        foreach (var story in _stories)
        {
            story.StoryGroup = fakeSprint;
            story.Project = fakeProject;
            fakeSprint.AddStory(story);
        }

        foreach (var task in _storyWithTask.Tasks) task.UserStory = _storyWithTask;


        var projectUserMemberships = new List<ProjectUserMembership>
        {
            new()
            {
                ProjectId = fakeProjectId, UserId = ActingUser.Id, Project = fakeProject, User = ActingUser,
                Role = ProjectRole.Developer
            },
            new()
            {
                ProjectId = fakeProjectId, UserId = AnotherUser.Id, Project = fakeProject, User = AnotherUser,
                Role = ProjectRole.Developer
            }
        };
        ActingUser.ProjectAssociations = projectUserMemberships;
        AnotherUser.ProjectAssociations = projectUserMemberships;
        fakeProject.MemberAssociations = projectUserMemberships;

        _mockProjectRepository = new Mock<IProjectRepository>(MockBehavior.Strict);
        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(fakeProjectId, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(fakeProject);

        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddSingleton<NavigationManager, FakeNavigationManager>();
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        _mockStateStorageService = new Mock<IScrumBoardStateStorageService>(MockBehavior.Strict);
        Services.AddScoped(_ => _mockStateStorageService.Object);

        _mockUserStoryRepository = new Mock<IUserStoryRepository>(MockBehavior.Strict);
        _mockUserStoryRepository
            .Setup(mock => mock.GetByStoryGroupAsync(fakeSprint))
            .ReturnsAsync(fakeSprint.Stories);
        foreach (var story in _stories)
        {
            _mockUserStoryRepository
                .Setup(mock => mock.GetByIdAsync(story.Id))
                .ReturnsAsync(story);
            _mockUserStoryRepository
                .Setup(mock => mock.GetByIdAsync(story.Id, UserStoryIncludes.Display))
                .ReturnsAsync(story);
        }

        Services.AddScoped(_ => _mockUserStoryRepository.Object);

        _mockUserStoryTaskRepository = new Mock<IUserStoryTaskRepository>(MockBehavior.Strict);
        foreach (var task in _tasks)
            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetByIdAsync(task.Id, UserStoryTaskIncludes.Creator,
                    UserStoryTaskIncludes.StoryGroup, UserStoryTaskIncludes.Users))
                .ReturnsAsync(task);
        foreach (var story in _stories)
            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetByStory(story, UserStoryTaskIncludes.Users))
                .ReturnsAsync(() => story.Tasks.ToList());
        Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);

        _mockUserStoryChangelogRepository = new Mock<IUserStoryChangelogRepository>(MockBehavior.Strict);
        Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);

        var mockSortableService = new Mock<ISortableService<UserStoryTask>>();
        Services.AddScoped(_ => mockSortableService.Object);
        Services.AddScoped(_ => _mockJsInteropService.Object);

        _mockUserStoryTaskChangelogRepository = new Mock<IUserStoryTaskChangelogRepository>(MockBehavior.Strict);
        Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);

        _mockWorklogEntryService = new Mock<IWorklogEntryService>(MockBehavior.Strict);
        _mockWorklogEntryService.Setup(x => x.GetWorklogEntriesForTaskAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<WorklogEntry> { new()
                {
                    Description = "Test Worklog Entry",
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(1)) },
                }
            });
        Services.AddScoped(_ => _mockWorklogEntryService.Object);

        ComponentFactories.AddDummyFactoryFor<FullStory>();
        ComponentFactories.AddDummyFactoryFor<TaskEditForm>();
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();

        return RenderComponent<SprintBoard>(parameters => parameters
            .AddCascadingValue("Self", ActingUser)
            .AddCascadingValue("ProjectState",
                new ProjectState { ProjectId = fakeProjectId, ProjectRole = ProjectRole.Developer })
        );
    }

    [Fact]
    public void PageLoaded_ActingUserCanView_DisplaysAllStories()
    {
        _component = CreateComponent();
        var stories = _component.FindAll(".story");
        stories.Should().HaveCount(_stories.Count);
    }

    [Fact]
    public void PageLoaded_GetStoryWithCards_CardsAllRender()
    {
        _component = CreateComponent();
        var cards = _component.FindComponents<SprintBoardTask>();
        cards.Should().HaveCount(_storyWithTask.Tasks.Count);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(102)]
    public void FindDetails_GivenStory_RendersNames(long storyId)
    {
        _component = CreateComponent();

        var story = _component.Instance.Sprint.Stories.First(s => s.Id == storyId);
        story.Tasks.Should().NotBeNull();

        var name = _component
            .FindComponents<StoryListItem>().First(c => c.Instance.Story.Id == storyId)
            .Find("#story-name");
        name.TextContent.Trim().Should().Contain(story.Name);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(102)]
    public void FindDetails_GivenStory_RendersNumberOfTasks(long storyId)
    {
        _component = CreateComponent();

        var story = _component.Instance.Sprint.Stories.First(s => s.Id == storyId);
        story.Tasks.Should().NotBeNull();

        var taskCount = _component
            .FindComponents<StoryListItem>().First(c => c.Instance.Story.Id == storyId)
            .Find("#story-task-count");
        taskCount.TextContent.Trim().Should().Contain(story.Tasks.Count.ToString());
    }

    [Theory]
    [InlineData(101)]
    [InlineData(102)]
    public void FindDetails_GivenStory_RendersStoryPriority(long storyId)
    {
        _component = CreateComponent();

        var story = _component.Instance.Sprint.Stories.First(s => s.Id == storyId);

        var priority = _component
            .FindComponents<StoryListItem>().First(c => c.Instance.Story.Id == storyId)
            .FindComponent<PriorityIndicator>();
        priority.Instance.Priority.Should().Be(story.Priority);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(102)]
    public void FindDetails_GivenStory_RendersStoryStage(long storyId)
    {
        _component = CreateComponent();

        var story = _component.Instance.Sprint.Stories.First(s => s.Id == storyId);

        var stage = _component
            .FindComponents<StoryListItem>().First(c => c.Instance.Story.Id == storyId)
            .FindComponent<StageBadge>();
        stage.Instance.Stage.Should().Be(story.Stage);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(102)]
    public void ClickCloseStory_GivenStoryPanelOpen_HidesStoryDetails(long storyId)
    {
        _component = CreateComponent();
        _component.Find($"#view-story-details-{storyId}").Click();

        var details = _component.FindComponent<DisplayPanel>()
            .FindComponent<Dummy<FullStory>>().Instance;
        _component.InvokeAsync(() => details.GetParam(x => x.OnClose).InvokeAsync());

        _component.FindComponents<Dummy<FullStory>>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(101, 101)]
    [InlineData(101, 102)]
    [InlineData(101, 103)]
    [InlineData(101, 104)]
    public async Task ClickDetailTask_GivenStoryAndTask_RendersStoryTask(long storyId, long taskId)
    {
        _component = CreateComponent();
        var task = _tasks.Single(task => task.Id == taskId);

        _component.Find($"#view-story-details-{storyId}").Click();

        // Select task from story view
        var fullStory = _component.FindComponent<DisplayPanel>()
            .FindComponent<Dummy<FullStory>>().Instance;
        await _component.InvokeAsync(() => fullStory.GetParam(x => x.OnViewTaskDetails).InvokeAsync(task));

        // Task form exists
        _component.FindComponents<Dummy<TaskEditForm>>().Should().ContainSingle();
    }

    [Theory]
    [InlineData(101, 101)]
    [InlineData(101, 102)]
    [InlineData(101, 103)]
    [InlineData(101, 104)]
    public async Task ClickDetailTask_GivenStoryAndTask_HidesStoryTask(long storyId, long taskId)
    {
        _component = CreateComponent();
        var task = _tasks.Single(task => task.Id == taskId);

        _component.Find($"#view-story-details-{storyId}").Click();

        // Select task from story view
        var fullStory = _component.FindComponent<DisplayPanel>()
            .FindComponent<Dummy<FullStory>>().Instance;
        await _component.InvokeAsync(() => fullStory.GetParam(x => x.OnViewTaskDetails).InvokeAsync(task));

        // Close task form
        var taskEditForm = _component.FindComponent<Dummy<TaskEditForm>>().Instance;
        await _component.InvokeAsync(() => taskEditForm.GetParam(x => x.OnClose).InvokeAsync());

        // Task form no longer exists
        _component.FindComponents<Dummy<TaskEditForm>>().Should().BeEmpty();
    }

    [Fact]
    public void NotFilteringTasks_AllTasksDisplayed()
    {
        _component = CreateComponent();

        _component.FindAll(".task-card").Count.Should().Be(4);
    }

    [Fact]
    public async Task FilterTasks_ByAssignee_RelevantTasksDisplayed()
    {
        _component = CreateComponent();

        _component.FindAll(".task-card").Count.Should().Be(4);

        _component.Find($"#assignee-user-select-{ActingUser.Id}").Click();

        var sortableList = _component.FindComponents<SortableList<UserStoryTask>>();

        foreach (var list in sortableList) await _component.InvokeAsync(list.Instance.Synchronize);

        _component.FindAll(".task-card").Count.Should().Be(1);
    }

    [Fact]
    public async Task FilterTasks_ByReviewer_RelevantTasksDisplayed()
    {
        _component = CreateComponent();

        _component.FindAll(".task-card").Count.Should().Be(4);

        _component.Find($"#reviewer-user-select-{AnotherUser.Id}").Click();

        var sortableList = _component.FindComponents<SortableList<UserStoryTask>>();

        foreach (var list in sortableList) await _component.InvokeAsync(list.Instance.Synchronize);
        
        _component.FindAll(".task-card").Count.Should().Be(1);
    }
    
    [Fact]
    public async Task FilterTasks_ByAssigneeAndReviewer_RelevantTasksDisplayed()
    {
        _component = CreateComponent();

        _component.FindAll(".task-card").Count.Should().Be(4);

        _component.Find($"#reviewer-user-select-{AnotherUser.Id}").Click();
        
        _component.Find($"#assignee-user-select-{ActingUser.Id}").Click();

        var sortableList = _component.FindComponents<SortableList<UserStoryTask>>();

        foreach (var list in sortableList) await _component.InvokeAsync(list.Instance.Synchronize);
        
        _component.FindAll(".task-card").Count.Should().Be(2);
    }
}