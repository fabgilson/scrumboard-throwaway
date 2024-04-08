using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Tests.Util;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Tests.Unit.Services;

public class SprintServiceTest
{
    private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
    private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SprintService>> _mockLogger = new();
    private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory = new();
    private readonly ISprintService _service;

    private readonly User _actingUser;

    private readonly DateOnly _initialEndDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);

    public SprintServiceTest()
    {
        _actingUser = new User()
        {
            Id = 5,
            FirstName = "Jimmy",
            LastName = "Neutron",
        };
        _service = new SprintService(
            _mockSprintChangelogRepository.Object, 
            _mockSprintRepository.Object, 
            _mockLogger.Object, 
            _mockDbContextFactory.Object,
            new Mock<IEntityLiveUpdateService>().Object
        );
    }

    [Fact]
    public async Task UpdateStage_CalledWithSprint_StageUpdated()
    {
        var id = 3;
        var name = "Test sprint";
        var newStage = SprintStage.InReview;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = SprintStage.Started,
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<SprintChangelogEntry>>()))
            .Returns(Task.CompletedTask);
            
            
        await _service.UpdateStage(_actingUser, sprint, newStage);
            
        var savedSprintArg = new ArgumentCaptor<Sprint>();
        _mockSprintRepository.Verify(mock => mock.UpdateAsync(savedSprintArg.Capture()), Times.Once);
        var savedSprint = savedSprintArg.Value;

        savedSprint.Stage.Should().Be(newStage);
        savedSprint.Id.Should().Be(id);
        savedSprint.Name.Should().Be(name);
            
        sprint.Stage.Should().Be(newStage);
        sprint.Id.Should().Be(id);
        sprint.Name.Should().Be(name);
    }

    [Fact]
    public async Task UpdateStage_CalledWithSprint_ChangelogEntryAdded()
    {
        var id = 3;
        var name = "Test sprint";
        var oldStage = SprintStage.Started;
        var newStage = SprintStage.InReview;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = oldStage,
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<SprintChangelogEntry>>()))
            .Returns(Task.CompletedTask);
            
            
        await _service.UpdateStage(_actingUser, sprint, newStage);
            
        var changeArg = new ArgumentCaptor<List<SprintChangelogEntry>>();
        _mockSprintChangelogRepository.Verify(mock => mock.AddAllAsync(changeArg.Capture()), Times.Once);
        changeArg.Value.Should().ContainSingle();
        var change = changeArg.Value.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.FieldChanged.Should().Be(nameof(Sprint.Stage));
        change.FromValueObject.Should().Be(oldStage);
        change.ToValueObject.Should().Be(newStage);
    }
        
    [Fact]
    public async Task UpdateStage_Successful_TrueReturned()
    {
        var id = 3;
        var name = "Test sprint";
        var newStage = SprintStage.InReview;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = SprintStage.Started,
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<SprintChangelogEntry>>()))
            .Returns(Task.CompletedTask);
            
            
        var success = await _service.UpdateStage(_actingUser, sprint, newStage);

        success.Should().BeTrue();
    }

    /// <summary>
    /// This test has been changed to check that the date is NOT updated, as this was causing a
    /// bug that prevented later sprints from being saved, as that sprint has a start date set to before
    /// when the prior sprint was closed.
    /// </summary>
    [Fact]
    public async Task UpdateStage_NewStageClosed_DateNotUpdated()
    {
        var id = 3;
        var name = "Test sprint";
        var newStage = SprintStage.Closed;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = SprintStage.Started,
            StartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
            EndDate = _initialEndDate
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<SprintChangelogEntry>>()))
            .Returns(Task.CompletedTask);
            
            
        var success = await _service.UpdateStage(_actingUser, sprint, newStage);

        var changeArg = new ArgumentCaptor<Sprint>();
        _mockSprintRepository.Verify(mock => mock.UpdateAsync(changeArg.Capture()), Times.Once);
        var change = changeArg.Value;

        change.EndDate.Should().Be(sprint.EndDate);

        var changelogArg = new ArgumentCaptor<List<SprintChangelogEntry>>();
        _mockSprintChangelogRepository.Verify(mock => mock.AddAllAsync(changelogArg.Capture()), Times.Once);
        var changelogs = changelogArg.Value;
        changelogs.Should().HaveCount(1);            
    }

    [Fact]
    public async Task UpdateStage_NewStageClosed_SameDate_NoChangelogAdded()
    {
        var id = 3;
        var name = "Test sprint";
        var newStage = SprintStage.Closed;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = SprintStage.Started,
            StartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
            EndDate = DateOnly.FromDateTime(DateTime.Now)
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Returns(Task.CompletedTask);
        _mockSprintChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<SprintChangelogEntry>>()))
            .Returns(Task.CompletedTask);
            
            
        var success = await _service.UpdateStage(_actingUser, sprint, newStage);

        var changeArg = new ArgumentCaptor<Sprint>();
        _mockSprintRepository.Verify(mock => mock.UpdateAsync(changeArg.Capture()), Times.Once);
        var change = changeArg.Value;

        change.EndDate.Should().Be(DateOnly.FromDateTime(DateTime.Now));

        var changelogArg = new ArgumentCaptor<List<SprintChangelogEntry>>();
        _mockSprintChangelogRepository.Verify(mock => mock.AddAllAsync(changelogArg.Capture()), Times.Once);
        var changelogs = changelogArg.Value;
        changelogs.Should().HaveCount(1);            
    }

    [Fact]
    public async Task UpdateStage_ConcurrencyExceptionOccurs_FalseReturned()
    {
        var id = 3;
        var name = "Test sprint";
        var oldStage = SprintStage.Started;
        var newStage = SprintStage.InReview;
        var sprint = new Sprint()
        {
            Id = id,
            Name = name,
            Stage = oldStage,
        };

        _mockSprintRepository
            .Setup(mock => mock.UpdateAsync(It.IsAny<Sprint>()))
            .Throws(new DbUpdateConcurrencyException());
            
        var success = await _service.UpdateStage(_actingUser, sprint, newStage);
        success.Should().BeFalse();
    }
}