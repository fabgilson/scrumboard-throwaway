using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Integration.LiveUpdating;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class AcceptanceCriteriaServiceTest : BaseIntegrationTestFixture
{
    protected readonly IAcceptanceCriteriaService AcceptanceCriteriaService;

    protected User ActingUser;

    protected AcceptanceCriteria NotYetReviewedAc;
    private AcceptanceCriteria _alreadyReviewedAc;

    public AcceptanceCriteriaServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper, bool startLiveUpdates=false) 
        : base(factory, outputHelper, startLiveUpdates)
    {
        AcceptanceCriteriaService = ServiceProvider.GetRequiredService<IAcceptanceCriteriaService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        ActingUser = FakeDataGenerator.CreateFakeUser();
        await dbContext.Users.AddAsync(ActingUser);

        var project = FakeDataGenerator.CreateFakeProject(projectId: LiveUpdateConnectionProjectId, developers: [DefaultUser]);
        var sprint = FakeDataGenerator.CreateFakeSprint(project);
        var story = FakeDataGenerator.CreateFakeUserStory(sprint);
        await dbContext.AddAsync(story);
        
        NotYetReviewedAc = FakeDataGenerator.CreateAcceptanceCriteria(
            story: story, 
            storyIsAlreadyInDatabase: true,
            reviewComments: "",
            status: null
        );
        _alreadyReviewedAc = FakeDataGenerator.CreateAcceptanceCriteria(
            story: story, 
            storyIsAlreadyInDatabase: true, 
            status: AcceptanceCriteriaStatus.Pass, 
            reviewComments: "All good"
        );
        await dbContext.AddRangeAsync(NotYetReviewedAc, _alreadyReviewedAc);
        
        await dbContext.SaveChangesAsync();
    }

    private async Task<AcceptanceCriteria> GetAcFromDbAsync(IId acceptanceCriteria)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.AcceptanceCriterias.FirstAsync(x => x.Id == acceptanceCriteria.Id);
    }

    private async Task<AcceptanceCriteriaChangelogEntry> GetSingleReviewCommentsChangelog(IId acceptanceCriteria)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.AcceptanceCriteriaChangelogEntries
            .Where(x => x.AcceptanceCriteriaChangedId == acceptanceCriteria.Id)
            .SingleOrDefaultAsync(x => x.FieldChanged == nameof(AcceptanceCriteria.ReviewComments));
    }
    
    private async Task<AcceptanceCriteriaChangelogEntry> GetSingleStatusChangelog(IId acceptanceCriteria)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.AcceptanceCriteriaChangelogEntries
            .Where(x => x.AcceptanceCriteriaChangedId == acceptanceCriteria.Id)
            .SingleOrDefaultAsync(x => x.FieldChanged == nameof(AcceptanceCriteria.Status));
    }
    
    private async Task<ICollection<AcceptanceCriteriaChangelogEntry>> GetAllReviewCommentsChangelogs(IId acceptanceCriteria)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.AcceptanceCriteriaChangelogEntries
            .Where(x => x.AcceptanceCriteriaChangedId == acceptanceCriteria.Id)
            .Where(x => x.FieldChanged == nameof(AcceptanceCriteria.ReviewComments))
            .ToListAsync();
    }
    
    private async Task<ICollection<AcceptanceCriteriaChangelogEntry>> GetAllStatusChangelogs(IId acceptanceCriteria)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.AcceptanceCriteriaChangelogEntries
            .Where(x => x.AcceptanceCriteriaChangedId == acceptanceCriteria.Id)
            .Where(x => x.FieldChanged == nameof(AcceptanceCriteria.Status))
            .ToListAsync();
    }

    [Fact]
    private async Task SetReviewFields_NotExistingAcceptanceCriteria_ArgumentExceptionThrown()
    {
        var action = async () => await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            12345,
            ActingUser.Id,
            AcceptanceCriteriaStatus.Pass,
            "New Review comments"
        );
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    private async Task SetReviewFields_AcceptanceCriteriaDoesExist_FieldsUpdated()
    {
        var initialAcValue = await GetAcFromDbAsync(NotYetReviewedAc);
        initialAcValue.Status.Should().BeNull();
        initialAcValue.ReviewComments.Should().BeNullOrEmpty();
        
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            NotYetReviewedAc.Id,
            ActingUser.Id,
            AcceptanceCriteriaStatus.Pass,
            "New Review comments"
        );

        var updatedAcValue = await GetAcFromDbAsync(NotYetReviewedAc);
        updatedAcValue.Status.Should().Be(AcceptanceCriteriaStatus.Pass);
        updatedAcValue.ReviewComments.Should().Be("New Review comments");
    }

    [Theory]
    [InlineData(AcceptanceCriteriaStatus.Pass, "")]
    [InlineData(AcceptanceCriteriaStatus.Pass, "Passed!")]
    [InlineData(AcceptanceCriteriaStatus.Fail, "Failed!")]
    private async Task SetReviewFields_NewValuesWhenNoneExisting_ChangelogsGeneratedCorrectly(AcceptanceCriteriaStatus status, string reviewComments)
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(NotYetReviewedAc.Id, ActingUser.Id, status, reviewComments);

        var reviewCommentChange = await GetSingleReviewCommentsChangelog(NotYetReviewedAc);
        if (!string.IsNullOrEmpty(reviewComments))
        {
            reviewCommentChange.FromValue.Should().BeNullOrEmpty();
            reviewCommentChange.ToValue.Should().Be(reviewComments);
            reviewCommentChange.Type.Should().Be(ChangeType.Update);
        }
        else
        {
            reviewCommentChange.Should().BeNull();
        }

        var statusChange = await GetSingleStatusChangelog(NotYetReviewedAc);
        statusChange.FromValueObject.Should().Be(null);
        statusChange.ToValueObject.Should().Be(status);
        statusChange.Type.Should().Be(ChangeType.Create);
    }

    [Fact]
    private async Task SetReviewFields_NewStatusAndReviewComments_BothChangelogsGenerated()
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            _alreadyReviewedAc.Id,
            ActingUser.Id, 
            AcceptanceCriteriaStatus.Fail, 
            "Failed :("
        );

        var statusChange = await GetSingleStatusChangelog(_alreadyReviewedAc);
        statusChange.FromValueObject.Should().Be(AcceptanceCriteriaStatus.Pass);
        statusChange.ToValueObject.Should().Be(AcceptanceCriteriaStatus.Fail);
        statusChange.Type.Should().Be(ChangeType.Update);
        
        var reviewCommentsChange = await GetSingleReviewCommentsChangelog(_alreadyReviewedAc);
        reviewCommentsChange.FromValue.Should().Be("All good");
        reviewCommentsChange.ToValueObject.Should().Be("Failed :(");
        reviewCommentsChange.Type.Should().Be(ChangeType.Update);
    }
    
    [Fact]
    private async Task SetReviewFields_NewStatusSameReviewComments_OnlyStatusChangelogGenerated()
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            _alreadyReviewedAc.Id,
            ActingUser.Id, 
            AcceptanceCriteriaStatus.Fail, 
            _alreadyReviewedAc.ReviewComments
        );

        var statusChange = await GetSingleStatusChangelog(_alreadyReviewedAc);
        statusChange.FromValueObject.Should().Be(AcceptanceCriteriaStatus.Pass);
        statusChange.ToValueObject.Should().Be(AcceptanceCriteriaStatus.Fail);
        statusChange.Type.Should().Be(ChangeType.Update);
        
        var reviewCommentsChange = await GetSingleReviewCommentsChangelog(_alreadyReviewedAc);
        reviewCommentsChange.Should().BeNull();
    }
    
    [Fact]
    private async Task SetReviewFields_SameStatusNewReviewComments_OnlyReviewCommentsChangelogGenerated()
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            _alreadyReviewedAc.Id,
            ActingUser.Id, 
            _alreadyReviewedAc.Status!.Value, 
            "Still all good"
        );

        var statusChange = await GetSingleStatusChangelog(_alreadyReviewedAc);
        statusChange.Should().BeNull();
        
        var reviewCommentsChange = await GetSingleReviewCommentsChangelog(_alreadyReviewedAc);
        reviewCommentsChange.FromValue.Should().Be("All good");
        reviewCommentsChange.ToValueObject.Should().Be("Still all good");
        reviewCommentsChange.Type.Should().Be(ChangeType.Update);
    }
    
    [Fact]
    private async Task SetReviewFields_SameStatusAndReviewComments_NoChangelogsGenerated()
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            _alreadyReviewedAc.Id,
            ActingUser.Id, 
            _alreadyReviewedAc.Status!.Value, 
            _alreadyReviewedAc.ReviewComments
        );

        var statusChange = await GetSingleStatusChangelog(_alreadyReviewedAc);
        statusChange.Should().BeNull();
        
        var reviewCommentsChange = await GetSingleReviewCommentsChangelog(_alreadyReviewedAc);
        reviewCommentsChange.Should().BeNull();
    }
    
    [Fact]
    private async Task SetStatusAndReviewCommentsMultipleTimes_SameEditingSessionGuid_OnlyOneChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var initialStatus = _alreadyReviewedAc.Status;
        var initialReviewComments = _alreadyReviewedAc.ReviewComments;
        var lastStatus = AcceptanceCriteriaStatus.Pass;
        var lastReviewComments = _alreadyReviewedAc.ReviewComments;

        // Use an odd number of iterations so we end up toggling to a different status
        for (var i = 0; i < 5; i++)
        {
            lastStatus = lastStatus is AcceptanceCriteriaStatus.Fail ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail;
            lastReviewComments = $"{lastReviewComments}-{i}";
            await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
                _alreadyReviewedAc.Id,
                ActingUser.Id,
                lastStatus,
                lastReviewComments,
                editingSessionGuid
            );
        }

        var reviewCommentsChanges = await GetAllReviewCommentsChangelogs(_alreadyReviewedAc);
        var reviewCommentChange = reviewCommentsChanges.Should().ContainSingle().Which;
        reviewCommentChange.FromValue.Should().Be(initialReviewComments);
        reviewCommentChange.ToValue.Should().Be(lastReviewComments);
        
        var statusChanges = await GetAllStatusChangelogs(_alreadyReviewedAc);
        var statusChange = statusChanges.Should().ContainSingle().Which;
        statusChange.FromValueObject.Should().Be(initialStatus);
        statusChange.ToValueObject.Should().Be(lastStatus);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private async Task SetStatusAndReviewCommentsMultipleTimes_DifferentEditingSessionGuidsOrNoGuids_MultipleChangelogsCreated(bool guidIsNull)
    {
        var reviewCommentsFromAndToPairs = new List<(string, string)>();
        var statusFromAndToPairs = new List<(AcceptanceCriteriaStatus?, AcceptanceCriteriaStatus?)>();

        var currentStatus = _alreadyReviewedAc.Status;
        var currentReviewComments = _alreadyReviewedAc.ReviewComments;
        for (var i = 0; i < 5; i++)
        {
            var newStatus = currentStatus is AcceptanceCriteriaStatus.Fail ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail;
            var newReviewComments = $"{currentReviewComments}-{i}";

            reviewCommentsFromAndToPairs.Add((currentReviewComments, newReviewComments));
            statusFromAndToPairs.Add((currentStatus, newStatus));

            currentStatus = newStatus;
            currentReviewComments = newReviewComments;

            await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
                _alreadyReviewedAc.Id,
                ActingUser.Id,
                newStatus,
                newReviewComments,
                guidIsNull ? null : Guid.NewGuid()
            );
        }

        var reviewCommentsChanges = await GetAllReviewCommentsChangelogs(_alreadyReviewedAc);
        reviewCommentsChanges.Should().HaveCount(5);
        foreach (var (fromComments, toComments) in reviewCommentsFromAndToPairs)
        {
            reviewCommentsChanges.Should().Contain(x => x.FromValue == fromComments && x.ToValue == toComments);
        }

        var statusChanges = await GetAllStatusChangelogs(_alreadyReviewedAc);
        statusChanges.Should().HaveCount(5);
        foreach (var (fromStatus, toStatus) in statusFromAndToPairs)
        {
            statusChanges.Should().Contain(x => 
                (AcceptanceCriteriaStatus?)x.FromValueObject == fromStatus 
                && (AcceptanceCriteriaStatus?)x.ToValueObject == toStatus
            );
        }
    }
    
    [Fact]
    private async Task SetStatusAndReviewCommentsMultipleTimes_SameEditingSessionGuidAndFinishesOnOriginalValues_NoChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var initialStatus = _alreadyReviewedAc.Status;
        var initialReviewComments = _alreadyReviewedAc.ReviewComments;

        var currentStatus = initialStatus;
        var currentReviewComments = initialReviewComments;

        for (var i = 0; i < 5; i++)
        {
            // On the last iteration, set back to original values
            if (i == 4)
            {
                currentStatus = initialStatus;
                currentReviewComments = initialReviewComments;
            }
            else
            {
                currentStatus = currentStatus is AcceptanceCriteriaStatus.Fail ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail;
                currentReviewComments = $"{currentReviewComments}-{i}";
            }
            await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
                _alreadyReviewedAc.Id,
                ActingUser.Id,
                currentStatus!.Value,
                currentReviewComments,
                editingSessionGuid
            );
        }

        var reviewCommentsChanges = await GetAllReviewCommentsChangelogs(_alreadyReviewedAc);
        reviewCommentsChanges.Should().BeEmpty();
    
        var statusChanges = await GetAllStatusChangelogs(_alreadyReviewedAc);
        statusChanges.Should().BeEmpty();
    }
}

[Collection(LiveUpdateIsolationCollection.CollectionName)]
public class AcceptanceCriteriaServiceLiveUpdatesTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : AcceptanceCriteriaServiceTest(factory, outputHelper, true)
{
    [Fact]
    public async Task AcceptanceCriteriaUpdated_NewValues_LiveUpdateBroadcastSent()
    {
        await AcceptanceCriteriaService.SetReviewFieldsByIdAsync(
            NotYetReviewedAc.Id,
            ActingUser.Id,
            AcceptanceCriteriaStatus.Pass,
            "Should send a broadcast"
        );

        await WaitForLiveUpdateInvocationsToNotBeEmpty(eventType: LiveUpdateEventType.EntityUpdated);
        var liveUpdateInvocation = LiveUpdateEventInvocations.Single(x => x.EventType == LiveUpdateEventType.EntityUpdated);

        liveUpdateInvocation.EntityType.Should().Be(typeof(AcceptanceCriteria));
        liveUpdateInvocation.EntityId.Should().Be(NotYetReviewedAc.Id);
        liveUpdateInvocation.EditingUserId.Should().Be(ActingUser.Id);

        var acValue = liveUpdateInvocation.GetDeserializedEntityValue<AcceptanceCriteria>();
        acValue.Id.Should().Be(NotYetReviewedAc.Id);
        acValue.ReviewComments.Should().Be("Should send a broadcast");
        acValue.Status.Should().Be(AcceptanceCriteriaStatus.Pass);
    }
}