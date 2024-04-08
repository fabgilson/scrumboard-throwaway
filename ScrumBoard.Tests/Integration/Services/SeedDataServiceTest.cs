using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

/// <summary>
/// Tests to ensure that the generated seed data follows the data integrity rules that we need.
/// In future if a different system is used for generating seed data it should be very simple 
/// to use these tests on that.
/// </summary>
public class SeedDataServiceTest : BaseIntegrationTestFixture
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public SeedDataServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _dbContextFactory = GetDbContextFactory();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        var seedDataService = ServiceProvider.GetRequiredService<ISeedDataService>();
        await seedDataService.SeedInitialDataAsync();
    }

    [Fact]
    public void Projects_AllHaveCreators()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var project in context.Projects.Include(project => project.Creator))
        {
            context.Users.Find(project.CreatorId).Should().NotBeNull($"'{project.Name}' needs a creator");
        }
    }

    [Fact]
    public void UserStories_AllHaveCreators()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories.Include(story => story.Creator))
        {
            context.Users.Find(story.CreatorId).Should().NotBeNull($"'{story.Name}' needs a creator");
        }
    }
        
    [Fact]
    public void UserStoryTasks_AllHaveCreators()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var task in context.UserStoryTasks.Include(task => task.Creator))
        {
            context.Users.Find(task.CreatorId).Should().NotBeNull($"'{task.Name}' needs a creator");
        }
    }

    [Fact]
    public void WorklogEntries_AllHaveUsers()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var entry in context.WorklogEntries)
        {
            context.Users.Find(entry.UserId).Should().NotBeNull($"'{entry.Description}' needs a user");
        }
    }

    [Fact]
    public void WorklogEntries_AllHaveCreatedAndOccurredSet()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var entry in context.WorklogEntries)
        {
            entry.Created.Should().NotBe(default);
            entry.Occurred.Should().NotBe(default);
        }
    }

    [Fact]
    public void WorklogEntries_AllUsersAreDevelopersOrLeaders()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var entry in context.WorklogEntries.Include(e => e.Task).ThenInclude(e => e.UserStory))
        {
            long entryProjectId = entry.Task.UserStory.ProjectId;
            context.ProjectUserMemberships.Where(m => m.UserId == entry.UserId && m.ProjectId == entryProjectId && (m.Role == ProjectRole.Developer || m.Role == ProjectRole.Leader)).FirstOrDefault()
                .Should().NotBeNull($"Entry: '{entry.Description}' user must have developer or leader role");
            if (entry.PairUserId != default) {
                context.ProjectUserMemberships.Where(m => m.UserId == entry.PairUserId && m.ProjectId == entryProjectId && (m.Role == ProjectRole.Developer || m.Role == ProjectRole.Leader)).FirstOrDefault()
                    .Should().NotBeNull($"Entry: '{entry.Description}' pair user must have developer or leader role");
            }               
        }
    }

    [Fact]
    public void UserStories_HaveUniqueOrders()
    {
        using var context = _dbContextFactory.CreateDbContext();

        foreach (var project in context.Projects
                     .Include(project => project.Backlog)
                     .ThenInclude(backlog => backlog.Stories)
                     .Include(project => project.Sprints)
                     .ThenInclude(sprint => sprint.Stories)
                ) {
            var allStories = project.Backlog.Stories
                .Concat(project.Sprints.SelectMany(sprint => sprint.Stories));
            allStories.Select(story => story.Order).Should().OnlyHaveUniqueItems($"'{project.Name}' cannot have duplicate story orders");
        }
    }

    [Fact]
    public void UserStories_HaveNonNullProject()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories)
        {
            context.Projects.Find(story.ProjectId).Should().NotBeNull($"'{story.Name}' needs a reference to a valid project");
        }
    }

    [Fact]
    public void UserStories_HaveNonNullStoryGroup()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories)
        {
            context.Find<StoryGroup>(story.StoryGroupId).Should().NotBeNull($"'{story.Name}' needs a reference to a valid story group");
        }
    }

    [Fact]
    public void UserStories_HaveAtLeastOneAcceptanceCriteria()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories.Include(u => u.AcceptanceCriterias))
        {
            story.AcceptanceCriterias.Should().NotBeEmpty($"'{story.Name}' needs at least one acceptance criteria");
        }
    }

    [Fact]
    public void UserStories_AllAcceptanceCriteriaHaveUniqueInStoryIds()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories.Include(u => u.AcceptanceCriterias))
        {
            story.AcceptanceCriterias.Should().OnlyHaveUniqueItems(ac => ac.InStoryId);
        }
    }
        
    [Fact]
    public void AcceptanceCriteria_InStoryIdShouldNotBeDefault()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var acceptanceCriteria in context.AcceptanceCriterias)
        {
            acceptanceCriteria.InStoryId.Should().NotBe(default, $"{acceptanceCriteria.Content} should have an assigned InStoryId");
        }
    }

    [Fact]
    public void UserStories_StoryGroupProjectIsSameAsProject()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories
                     .Include(story => story.StoryGroup)
                )
        {
            var storyGroupProjectId = story.StoryGroup switch
            {
                Sprint sprint => sprint.SprintProjectId,
                Backlog backlog => backlog.BacklogProjectId,
                _ => throw new NotSupportedException($"Unknown story group type: {story.StoryGroup}")
            };
            storyGroupProjectId.Should().Be(story.ProjectId);
        }
    }

    [Fact]
    public void UserStories_IfInBacklogThenStageIsTodo()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var story in context.UserStories
                     .Include(story => story.StoryGroup))
        {
            if (story.StoryGroup is Backlog)
            {
                story.Stage.Should().Be(Stage.Todo, $"'{story.Name}' is in backlog, but does not have stage {nameof(Stage.Todo)}");
            }
        }
    }

    [Fact]
    public void StoryGroups_StoriesWithinGroupHaveUniqueOrderField()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var storyGroup in context.Set<StoryGroup>()
                     .Include(group => group.Stories))
        {
            storyGroup.Stories
                .Select(story => story.Order)
                .Should()
                .OnlyHaveUniqueItems($"{storyGroup} should have only unique items for Story.Order");
        }
    }

    [Fact]
    public void Sprints_StartOnOrAfterProjectStart()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var sprint in context.Sprints
                     .Include(group => group.Project))
        {
                
        }
    }
        
    [Fact]
    public void Sprints_TimeStartedShouldBeOnOrAfterStartDate()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var sprint in context.Sprints)
        {
            if (sprint.TimeStarted.HasValue)
            {
                DateOnly.FromDateTime(sprint.TimeStarted.Value).Should().BeOnOrAfter(sprint.StartDate,
                    $"'{sprint.Name}' should have been started on or after sprint start date");
            }
        }
    }

    [Fact]
    public void Sprints_EndOnOrBeforeProjectEnd()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var sprint in context.Sprints
                     .Include(group => group.Project))
        {
            sprint.EndDate.Should().BeOnOrBefore(sprint.Project.EndDate,
                $"'{sprint.Name}' should end on or before project end date");
        }
    }

    [Fact]
    public void Users_HaveLargeUserIds()
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var user in context.Users)
        {
            user.Id.Should().BeGreaterThanOrEqualTo(10000, "small user ids are likely to be clobbered by IdentityProvider");
        }
    }
}