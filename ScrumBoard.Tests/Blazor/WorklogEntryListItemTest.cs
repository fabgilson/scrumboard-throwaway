using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Utils;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Shared.Widgets.Messages;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class WorklogEntryListItemTest : TestContext
{
    private readonly User _actingUser = new() {Id = 33, FirstName = "Jeff", LastName = "Jefferson"};

    private readonly User _pairUser = new() { Id = 101, FirstName = "Pair", LastName = "User" };

    private static readonly User _taskCreator = new() {Id = 34, FirstName = "Thomas", LastName = "Creator"};

    private static readonly Project _currentProject = new() {Id = 1};

    private readonly UserStoryTask _currentTask = new()
    {
        UserStory = new UserStory
        {
            Project = _currentProject,
            Stage = Stage.Todo
        },
        Stage = Stage.Todo,
        Creator = _taskCreator,
        Created = DateTime.Now
    };

    private WorklogEntryChangelogEntry _worklogEntryChangelogEntry;
    private WorklogEntry _currentWorklogEntry;
    private IRenderedComponent<WorklogEntryListItem> _component;
    
    private DateTime _changelogCreated;
    private IRenderedComponent<MessageListItem> _changelogDisplay;

    private readonly WorklogTag _testTag = FakeDataGenerator.CreateWorklogTag(name: "Test");
    private readonly WorklogTag _featureTag = FakeDataGenerator.CreateWorklogTag(name: "Feature");

    private readonly Mock<IWorklogEntryChangelogRepository> _mockWorklogEntryChangelogRepository = new(MockBehavior.Strict);
    private readonly Mock<IWorklogTagRepository> _mockWorklogTagRepository = new(MockBehavior.Strict);
    private readonly Mock<IJsInteropService> _mockJsInteropService = new();

    private Mock<Action> _editWorklog = new();

    private bool _isEditing = false;

    public WorklogEntryListItemTest()
    {
        TypeDescriptor.AddAttributes(typeof(TaggedWorkInstance), new TypeConverterAttribute(typeof(TaggedWorkInstanceTypeConverter)));
        
        _mockWorklogTagRepository
            .Setup(mock => mock.GetAllAsync())
            .ReturnsAsync(new List<WorklogTag>());

        Services.AddScoped(_ => _mockWorklogTagRepository.Object);
        Services.AddScoped(_ => _mockWorklogEntryChangelogRepository.Object);
        Services.AddScoped(_ => _mockJsInteropService.Object);
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
    }

    private void SetupComponent(bool hasChanges = false, bool hasPair = false, bool isCreator = true, bool isReadOnly = false, ProjectRole projectRole = ProjectRole.Leader)
    {
        _currentWorklogEntry = new WorklogEntry
        {
            Id = 24,
            User = isCreator ? _actingUser : _pairUser, 
            Task = _currentTask, 
            Created = DateTime.Now, 
            Description = "Initial Description", 
            TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(1)) }, 
            Occurred = DateTime.Now, 
        };
        _currentWorklogEntry.UserId = _currentWorklogEntry.User.Id;

        if (hasPair) _currentWorklogEntry.PairUser = _pairUser;

        _worklogEntryChangelogEntry = new(_actingUser, _currentWorklogEntry, nameof(WorklogEntry.Description), Change<object>.Update("A description", "Initial Description"));
        _worklogEntryChangelogEntry.Creator = _actingUser; // Only CreatorId is set by constructor
            
        if (hasChanges) {
            _mockWorklogEntryChangelogRepository
                .Setup(mock => mock.GetByWorklogEntryAsync(_currentWorklogEntry, WorklogEntryChangelogIncludes.Display))
                .ReturnsAsync(new List<WorklogEntryChangelogEntry> {_worklogEntryChangelogEntry});
        } else {
            _mockWorklogEntryChangelogRepository
                .Setup(mock => mock.GetByWorklogEntryAsync(_currentWorklogEntry, WorklogEntryChangelogIncludes.Display))
                .ReturnsAsync(new List<WorklogEntryChangelogEntry>());
        }
            
        _component = RenderComponent<WorklogEntryListItem>(parameters => parameters
            .AddCascadingValue("Self", _actingUser)
            .AddCascadingValue("ProjectState", new ProjectState {ProjectRole = projectRole, IsReadOnly = isReadOnly})
            .Add(cut => cut.Entry, _currentWorklogEntry)
            .Add(cut => cut.IsEditing, _isEditing)
            .Add(cut => cut.EditWorklog, _editWorklog.Object)
        );
    }

    [Fact]
    public void ExpandWorklog_HasOneUser_FullNameShown() {
        SetupComponent();
        _component.Find("#worklog-list-item").Click();
            
        var expectedName = $"{_currentWorklogEntry.User.FirstName} {_currentWorklogEntry.User.LastName}"; 
        var contributors = _component.Find("#contributors");
        contributors.TextContent.Should().Contain(expectedName);
    }

    [Fact]
    public void ExpandWorklog_HasTwoUsers_BothFullNamesShown() {
        SetupComponent(hasPair: true);

        var expectedContributorName = $"{_currentWorklogEntry.User.FirstName} {_currentWorklogEntry.User.LastName}"; 
        var expectedPairName = $"{_currentWorklogEntry.PairUser.FirstName} {_currentWorklogEntry.PairUser.LastName}";
        
        _component.Find("#worklog-list-item").Click();
        var contributors = _component.Find("#contributors");
        contributors.TextContent.Should().Contain(expectedContributorName);
        contributors.TextContent.Should().Contain(expectedPairName);
    }

    [Fact]
    public void EditWorklogButton_Pressed_EditWorklogEventTrigger()
    {
        SetupComponent();
            
        _component.Find("#edit-worklog").Click();
        _editWorklog.Verify(mock => mock(), Times.Once);
    }
        
    [Fact]
    public void EditWorklogButton_IsReadOnly_EditButtonDoesNotExist()
    {
        SetupComponent(isReadOnly: true);
        _component.FindAll("#edit-worklog").Should().BeEmpty();
    }
        
    [Fact]
    public void EditWorklogButton_IsNotLeaderButIsWorklogCreator_EditButtonExists()
    {
        SetupComponent(isCreator: true, projectRole: ProjectRole.Developer);
        _component.FindAll("#edit-worklog").Should().ContainSingle();
    }
        
    [Fact]
    public void EditWorklogButton_IsNotLeaderAndIsNotWorklogCreator_EditButtonDoesNotExist()
    {
        SetupComponent(isCreator: false, projectRole: ProjectRole.Developer);
        _component.FindAll("#edit-worklog").Should().BeEmpty();
    }
    
    
    [Fact]
    public void DefaultState_ChangelogNotVisible()
    {
        SetupComponent();

        _component.FindAll("#worklog-changelog-container").Should().BeEmpty();
    }
        
    [Fact]
    public void OpenChangelog_NoChanges_PlaceholderShown()
    {
        SetupComponent();
            
        _component.Find("#changelog-toggle-button").Click();
            
        _component.FindAll("#worklog-changelog-container").Should().HaveCount(1);
        _component.FindAll("#no-changelog-items").Should().HaveCount(1);
    }
        
    [Fact]
    public void OpenChangelog_HasChanges_ChangesShown()
    {
        SetupComponent(true);
        _component.Find("#changelog-toggle-button").Click();
            
        _component.FindAll("#worklog-changelog-container").Should().HaveCount(1);
        _component.FindAll("#no-changelog-items").Should().BeEmpty();
        _component.FindAll("#worklog-entry-changelog-entry").Should().HaveCount(1);

    }

    private void SetupChangelogDisplayTest(ChangeType changeType, TimeSpan? fromDuration = null, TimeSpan? toDuration = null)
    {
        SetupComponent(true);
        _changelogCreated = DateTime.Now;

        var changelogEntry = new TaggedWorkInstanceChangelogEntry
        {
            Type = changeType,
            Created = _changelogCreated,
            Creator = _actingUser,
            WorklogTag = _testTag,
            FromValueObject = fromDuration.HasValue ? new TaggedWorkInstance { WorklogTag = _testTag, Duration = fromDuration.Value } : null,
            ToValueObject = toDuration.HasValue ? new TaggedWorkInstance { WorklogTag = _testTag, Duration = toDuration.Value } : null
        };

        _mockWorklogEntryChangelogRepository.Setup(x =>
            x.GetByWorklogEntryAsync(It.IsAny<WorklogEntry>(), WorklogEntryChangelogIncludes.Display)
        ).ReturnsAsync(new List<WorklogEntryChangelogEntry> { changelogEntry });

        _component.Find("#changelog-toggle-button").Click();
        _changelogDisplay = _component.FindComponents<MessageListItem>()[0];
    }

    private void VerifyChangelogDisplay(string expectedTextStart, string expectedCreatedAt)
    {
        _changelogDisplay.Find("#message-list-item-created").TextContent.Should().Be(expectedCreatedAt);
        _changelogDisplay.Find("#text-token-text-content").TextContent.Should().StartWith(expectedTextStart);
    }

    [Fact]
    public void OpenChangelog_CreateTaggedWorkInstanceChange_ChangeShownCorrectly()
    {
        SetupChangelogDisplayTest(ChangeType.Create, toDuration: TimeSpan.FromMinutes(90));
        VerifyChangelogDisplay($"{_actingUser.GetFullName()} added work instance", _changelogCreated.ToString(CultureInfo.CurrentCulture));
        _changelogDisplay.Find("#value-token-text-content").TextContent.Should().Be("Test (1h 30m)");
    }
    
    [Fact]
    public void OpenChangelog_UpdateTaggedWorkInstanceChange_ChangeShownCorrectly()
    {
        SetupChangelogDisplayTest(ChangeType.Update, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(90));
        VerifyChangelogDisplay($"{_actingUser.GetFullName()} updated work instance", _changelogCreated.ToString(CultureInfo.CurrentCulture));
        var valueTokens = _changelogDisplay.FindAll("#value-token-text-content");
        valueTokens[0].TextContent.Should().Be("Test (30m)");
        _changelogDisplay.FindAll("#arrow-token").Should().ContainSingle();
        valueTokens[1].TextContent.Should().Be("Test (1h 30m)");
    }
    
    [Fact]
    public void OpenChangelog_RemoveTaggedWorkInstanceChange_ChangeShownCorrectly()
    {
        SetupChangelogDisplayTest(ChangeType.Delete, TimeSpan.FromMinutes(30));
        VerifyChangelogDisplay($"{_actingUser.GetFullName()} removed a work instance", _changelogCreated.ToString(CultureInfo.CurrentCulture));
        _changelogDisplay.Find("#value-token-text-content").TextContent.Should().Be("Test (30m)");
    }
}