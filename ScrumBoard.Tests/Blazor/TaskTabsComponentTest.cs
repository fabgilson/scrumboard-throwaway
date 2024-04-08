using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using System;
using System.Collections.Generic;
using Xunit;
using ScrumBoard.Shared;
using FluentAssertions;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Tests.Blazor.Modals;
using ScrumBoard.Tests.Util;

namespace ScrumBoard.Tests.Blazor;

public class TaskTabsComponentTest : BaseProjectScopedComponentTestContext<TaskTabs>
{
    private readonly UserStoryTaskTag _magic = new() {Id = 1, Name = "Magic"};

    private readonly UserStoryTask _task;

    private readonly Sprint _currentSprint;

    private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);

    private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new(MockBehavior.Strict);
        
    private readonly Mock<IWorklogEntryService> _mockWorklogEntryRepository = new(MockBehavior.Strict);

    private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
        
    private readonly Mock<Action> _onUpdate = new();

    public TaskTabsComponentTest()
    {
        var backlogId = 999;
        var taskCreator = new User { Id = 100, FirstName = "Thomas", LastName = "Creator" };
        _task = new UserStoryTask
        {
            Id = 42,
            UserStory = new UserStory
            {
                Project = CurrentProject,
                Stage = Stage.Todo,
                StoryGroupId = backlogId,
            },
            Stage = Stage.InProgress,
            Creator = taskCreator,
            Created = DateTime.Now,
            Estimate = TimeSpan.FromHours(1),
            Tags = new List<UserStoryTaskTag> { _magic },
        };

        _currentSprint = new Sprint
        {
            Id = 13, 
            Project = CurrentProject, 
            Stage = SprintStage.Created
        };

        Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
        Services.AddScoped(_ => _mockWorklogEntryRepository.Object);
        Services.AddScoped(_ => _mockSprintRepository.Object);

        _mockSprintRepository.Setup(mock => mock.GetByIdAsync(backlogId))
            .ReturnsAsync((Sprint)null);
        _mockSprintRepository.Setup(mock => mock.GetByIdAsync(_currentSprint.Id))
            .ReturnsAsync(_currentSprint);

        _mockUserStoryTaskChangelogRepository
            .Setup(mock => mock.GetByUserStoryTaskAsync(_task, UserStoryTaskChangelogIncludes.Display))
            .ReturnsAsync(new List<UserStoryTaskChangelogEntry>());
        _mockWorklogEntryRepository
            .Setup(mock => mock.GetWorklogEntriesForTaskAsync(_task.Id))
            .ReturnsAsync(new List<WorklogEntry>());

        // Add dummy ModalTrigger
        ComponentFactories.Add(new ModalTriggerComponentFactory());

        ComponentFactories.AddDummyFactoryFor<EditWorklogEntry>();

        Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);
    }

    private void SetupComponent(bool hasSprint=false, bool initiallyAddingWorklog=false)
    {
        if (hasSprint) {
            _task.UserStory.StoryGroup = _currentSprint;
            _task.UserStory.StoryGroupId = _currentSprint.Id;
        }

        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .AddCascadingValue("AddingWorklog", initiallyAddingWorklog)
            .Add(cut => cut.Task, _task)
            .Add(cut => cut.OnUpdate, _onUpdate.Object)
        );
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InitialRender_NotAddingWorklog_NoEditWorklogEntryShown(bool hasSprint, bool sprintStarted)
    {
        if (sprintStarted) _currentSprint.Stage = SprintStage.Started;
        SetupComponent(hasSprint, initiallyAddingWorklog: false);

        ComponentUnderTest.FindComponents<EditWorklogEntry>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void InitialRender_AddingWorklogAndCannotEditWorklog_EditWorklogComponentNotShown(bool hasSprint, bool sprintStarted)
    {
        if (sprintStarted) _currentSprint.Stage = SprintStage.Started;
        SetupComponent(hasSprint, initiallyAddingWorklog: true);

        ComponentUnderTest.FindComponents<EditWorklogEntry>().Should().BeEmpty();
    }

    [Fact]
    public void InitialRender_AddingWorklogWithStartedSprint_EditWorklogComponentShown()
    {
        _currentSprint.Stage = SprintStage.Started;
        SetupComponent(hasSprint: true, initiallyAddingWorklog: true);

        var editWorklogs = ComponentUnderTest.FindComponents<Dummy<EditWorklogEntry>>();
        var editWorklog = editWorklogs.Should().ContainSingle().Which;
        editWorklog.Instance.GetParam(x => x.Entry).Task.Should().Be(_task);
    }
}