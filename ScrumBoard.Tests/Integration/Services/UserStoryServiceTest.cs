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
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class UserStoryServiceTest : BaseIntegrationTestFixture
{
    private readonly IUserStoryService _userStoryService;
    
    private User _actingUser;
    private Sprint _sprint;
    private UserStory _notYetReviewedStory, _alreadyReviewedStory, _deferredStory;
    
    public UserStoryServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _userStoryService = ServiceProvider.GetRequiredService<IUserStoryService>();
    }
    
    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _actingUser = FakeDataGenerator.CreateFakeUser();
        await dbContext.Users.AddAsync(_actingUser);

        var project = FakeDataGenerator.CreateFakeProject();
        _sprint = FakeDataGenerator.CreateFakeSprint(project);
        _notYetReviewedStory = FakeDataGenerator.CreateFakeUserStory(_sprint);
        _alreadyReviewedStory = FakeDataGenerator.CreateFakeUserStory(_sprint, generateReviewComments: true);
        _deferredStory = FakeDataGenerator.CreateFakeUserStory(_sprint, stage: Stage.Deferred);
        await dbContext.AddRangeAsync(_notYetReviewedStory, _alreadyReviewedStory, _deferredStory);
        
        await dbContext.SaveChangesAsync();
    }

    private async Task<UserStory> GetStoryFromDb(UserStory story)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.UserStories.SingleOrDefaultAsync(x => x.Id == story.Id);
    }

    private async Task<UserStoryChangelogEntry> GetSingleReviewCommentsChangelog(UserStory story)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.UserStoryChangelogEntries
            .SingleOrDefaultAsync(x => x.UserStoryChangedId == story.Id);
    }
    
    private async Task<ICollection<UserStoryChangelogEntry>> GetAllReviewCommentsChangelogs(UserStory story)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        return await context.UserStoryChangelogEntries
            .Where(x => x.UserStoryChangedId == story.Id)
            .ToListAsync();
    }

    [Fact]
    public async Task SetReviewComments_NotExistingStory_ArgumentExceptionThrown()
    {
        var action = async () => await _userStoryService.SetReviewCommentsForIdAsync(
            12345, 
            _actingUser.Id, 
            "New Review comments"
        );
        await action.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task SetReviewComments_StoryDoesExist_FieldUpdated()
    {
        var initialStoryValue = await GetStoryFromDb(_notYetReviewedStory);
        initialStoryValue.ReviewComments.Should().BeNullOrEmpty();
        
        await _userStoryService.SetReviewCommentsForIdAsync(
            _notYetReviewedStory.Id, 
            _actingUser.Id, 
            "New Review comments"
        );
        
        var newStoryValue = await GetStoryFromDb(_notYetReviewedStory);
        newStoryValue.ReviewComments.Should().Be("New Review comments");
    }
    
    [Fact]
    public async Task SetReviewComments_DifferentValue_ChangelogCreated()
    {
        await _userStoryService.SetReviewCommentsForIdAsync(
            _notYetReviewedStory.Id, 
            _actingUser.Id, 
            "New Review comments"
        );
        
        var change = await GetSingleReviewCommentsChangelog(_notYetReviewedStory);
        change.Type.Should().Be(ChangeType.Update);
        change.FromValue.Should().BeNullOrEmpty();
        change.ToValueObject.Should().Be("New Review comments");
    }
    
    [Fact]
    public async Task SetReviewComments_SameValue_NoChangelogCreated()
    {
        await _userStoryService.SetReviewCommentsForIdAsync(
            _alreadyReviewedStory.Id, 
            _actingUser.Id, 
            _alreadyReviewedStory.ReviewComments
        );
        
        var change = await GetSingleReviewCommentsChangelog(_notYetReviewedStory);
        change.Should().BeNull();
    }
    
    [Fact]
    public async Task SetReviewCommentsMultipleTimes_SameEditingSessionGuid_OnlyOneChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var initialValue = _alreadyReviewedStory.ReviewComments;
        var lastValue = "";
        
        var reviewComments = _alreadyReviewedStory.ReviewComments;
        for (var i = 0; i < 5; i++)
        {
            reviewComments = $"{reviewComments}-{i}";
            await _userStoryService.SetReviewCommentsForIdAsync(
                _alreadyReviewedStory.Id, 
                _actingUser.Id, 
                reviewComments,
                editingSessionGuid: editingSessionGuid
            );
            lastValue = reviewComments;
        }
        
        var changes = await GetAllReviewCommentsChangelogs(_alreadyReviewedStory);
        var change = changes.Should().ContainSingle().Which;
        change.FromValue.Should().Be(initialValue);
        change.ToValue.Should().Be(lastValue);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetReviewCommentsMultipleTimes_DifferentEditingSessionGuidsOrNoGuids_MultipleChangelogsCreated(bool guidIsNull)
    {
        var fromAndToPairs = new List<(string, string)>();
        
        var reviewComments = _alreadyReviewedStory.ReviewComments;
        for (var i = 0; i < 5; i++)
        {
            var newReviewComments = $"{reviewComments}-{i}";
            fromAndToPairs.Add((reviewComments, newReviewComments));
            reviewComments = newReviewComments;
            
            await _userStoryService.SetReviewCommentsForIdAsync(
                _alreadyReviewedStory.Id, 
                _actingUser.Id, 
                reviewComments,
                editingSessionGuid: guidIsNull ? null : Guid.NewGuid()
            );
        }
        
        var changes = await GetAllReviewCommentsChangelogs(_alreadyReviewedStory);
        changes.Should().HaveCount(5);
        foreach (var fromAndToPair in fromAndToPairs)
        {
            changes.Should().Contain(x => x.FromValue == fromAndToPair.Item1 && x.ToValue == fromAndToPair.Item2);
        }
    }
    
    [Fact]
    public async Task SetReviewCommentsMultipleTimes_SameEditingSessionGuidAndFinishesOnOriginalValue_NoChangelogCreated()
    {
        var editingSessionGuid = Guid.NewGuid();
        var initialValue = _alreadyReviewedStory.ReviewComments;

        var reviewComments = _alreadyReviewedStory.ReviewComments;
        for (var i = 0; i < 5; i++)
        {
            // On the last iteration, set back to original value
            reviewComments = i == 4 ? initialValue : $"{reviewComments}-{i}";
            await _userStoryService.SetReviewCommentsForIdAsync(
                _alreadyReviewedStory.Id, 
                _actingUser.Id, 
                reviewComments,
                editingSessionGuid: editingSessionGuid
            );
        }
        
        var changes = await GetAllReviewCommentsChangelogs(_alreadyReviewedStory);
        changes.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetStoryById_StoryDoesNotExist_NullReturned()
    {
        var actualStoryReturned = await _userStoryService.GetByIdAsync(12345);
        actualStoryReturned.Should().BeNull();
    }
    
    [Fact]
    public async Task GetStoryById_StoryExists_CorrectStoryReturned()
    {
        var actualStoryReturned = await _userStoryService.GetByIdAsync(_notYetReviewedStory.Id);
        actualStoryReturned.Should().BeEquivalentTo(_notYetReviewedStory, options => options
            .Excluding(x => x.Project)
            .Excluding(x => x.AcceptanceCriterias)
            .Excluding(x => x.StoryGroup)
            .Excluding(x => x.Creator)
        );
    }

    [Fact]
    public async Task GetStoriesBySprintId_StoriesExistInGivenSprint_CorrectStoriesReturned()
    {
        var expected = new [] { _notYetReviewedStory, _alreadyReviewedStory, _deferredStory};
        var actualStoriesReturned = await _userStoryService.GetBySprintIdAsync(_sprint.Id);
        
        actualStoriesReturned.Should().BeEquivalentTo(expected, options => options
            .Excluding(x => x.Project)
            .Excluding(x => x.AcceptanceCriterias)
            .Excluding(x => x.StoryGroup)
            .Excluding(x => x.Creator)
            .WithStrictOrdering());
    }
    
    [Fact]
    public async Task GetStoriesBySprintReviewAsync_MixtureOfDeferredAndNonDeferredStoriesInSprint_OnlyNonDeferredStoriesReturned()
    {
        var expected = new [] { _notYetReviewedStory, _alreadyReviewedStory};
        var actualStoriesReturned = await _userStoryService.GetStoriesForSprintReviewAsync(_sprint.Id);
        
        actualStoriesReturned.Should().BeEquivalentTo(expected, options => options
            .Excluding(x => x.Project)
            .Excluding(x => x.AcceptanceCriterias)
            .Excluding(x => x.StoryGroup)
            .Excluding(x => x.Creator)
            .WithStrictOrdering());
    }
}