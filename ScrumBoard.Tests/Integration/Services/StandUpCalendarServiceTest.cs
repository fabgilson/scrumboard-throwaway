using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class StandUpCalendarServiceTest : BaseIntegrationTestFixture
{
    private User _userInProject;
    private Project _project;
    private Sprint _sprint;
    
    private readonly IStandUpCalendarService _standUpCalendarService;
    
    public StandUpCalendarServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _standUpCalendarService = ServiceProvider.GetRequiredService<IStandUpCalendarService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _userInProject = FakeDataGenerator.CreateFakeUser();
        await dbContext.Users.AddRangeAsync(_userInProject);
        
        _project = FakeDataGenerator.CreateFakeProject(developers: new[] {_userInProject});
        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project);
        await dbContext.Projects.AddAsync(_project);
        await dbContext.Sprints.AddAsync(_sprint);
        
        await dbContext.SaveChangesAsync();
    }

    private async Task<Calendar> GetCalendarForProject(long projectId)
    {
        return await _standUpCalendarService.GetCalendarForProjectAsync(projectId);
    }
    
    [Fact]
    public async Task CreateStandUpCalendarLinkAsync_NoExistingLink_LinkCreated()
    {
        var result = await _standUpCalendarService.CreateStandUpCalendarLinkAsync(_userInProject.Id, _project.Id);

        result.Should().NotBeNull();
        result.UserId.Should().Be(_userInProject.Id);
        result.ProjectId.Should().Be(_project.Id);
        result.Token.Should().NotBeNullOrEmpty();

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var linkInDb = await context.UserStandUpCalendarLinks.FindAsync(_userInProject.Id, _project.Id);
        linkInDb.Should().NotBeNull();
        linkInDb!.Token.Should().Be(result.Token);
    }

    [Fact]
    public async Task CreateStandUpCalendarLinkAsync_ExistingLink_ThrowsInvalidOperationException()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var existingLink = new UserStandUpCalendarLink
        {
            UserId = _userInProject.Id,
            ProjectId = _project.Id,
            Token = Guid.NewGuid().ToString()
        };
        context.UserStandUpCalendarLinks.Add(existingLink);
        await context.SaveChangesAsync();

        var action = async () => { await _standUpCalendarService.CreateStandUpCalendarLinkAsync(_userInProject.Id, _project.Id); };

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A link already exists between this user and project");
    }
    
    [Fact]
    public async Task ResetTokenForStandUpCalendarLink_ExistingLink_TokenReset()
    {
        await using var arrangeContext = await GetDbContextFactory().CreateDbContextAsync();
        var existingLink = new UserStandUpCalendarLink
        {
            UserId = _userInProject.Id,
            ProjectId = _project.Id,
            Token = Guid.NewGuid().ToString()
        };
        arrangeContext.UserStandUpCalendarLinks.Add(existingLink);
        await arrangeContext.SaveChangesAsync();

        var result = await _standUpCalendarService.ResetTokenForStandUpCalendarLink(_userInProject.Id, _project.Id);
        result.Should().NotBeNull();
        result.Token.Should().NotBe(existingLink.Token);
        
        await using var assertContext = await GetDbContextFactory().CreateDbContextAsync();
        var linkInDb = await assertContext.UserStandUpCalendarLinks.FindAsync(_userInProject.Id, _project.Id);
        linkInDb.Should().NotBeNull();
        linkInDb!.Token.Should().Be(result.Token);
    }

    [Fact]
    public async Task ResetTokenForStandUpCalendarLink_NoLinkExists_ThrowsInvalidOperationException()
    {
        var action = async () => { await _standUpCalendarService.ResetTokenForStandUpCalendarLink(_userInProject.Id, _project.Id); };
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("No link found to reset token for.");
    }
    
    [Fact]
    public async Task DeleteStandUpCalendarLinkAsync_ExistingLink_LinkDeleted()
    {
        await using var arrangeContext = await GetDbContextFactory().CreateDbContextAsync();
        var existingLink = new UserStandUpCalendarLink
        {
            UserId = _userInProject.Id,
            ProjectId = _project.Id,
            Token = Guid.NewGuid().ToString()
        };
        arrangeContext.UserStandUpCalendarLinks.Add(existingLink);
        await arrangeContext.SaveChangesAsync();

        await _standUpCalendarService.DeleteStandUpCalendarLinkAsync(_userInProject.Id, _project.Id);

        await using var assertContext = await GetDbContextFactory().CreateDbContextAsync();
        var linkInDb = await assertContext.UserStandUpCalendarLinks.FindAsync(_userInProject.Id, _project.Id);
        linkInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteStandUpCalendarLinkAsync_NoLinkExists_NoExceptionThrown()
    {
        // No exception should be thrown and method should return quietly
        await _standUpCalendarService.Invoking(s => s.DeleteStandUpCalendarLinkAsync(_userInProject.Id, _project.Id))
            .Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task GetCalendarForProjectAsync_NoStandUps_ReturnsEmptyCalendar()
    {
        var calendar = await GetCalendarForProject(_project.Id);
        calendar.Should().NotBeNull();
        calendar.Events.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetCalendarForProjectAsync_WithFutureStandUps_ReturnsCalendarWithEvents()
    {
        var standUpMeeting = new StandUpMeeting
        {
            SprintId = _sprint.Id,
            Name = "Daily Scrum",
            ScheduledStart = DateTime.Now.AddDays(1), // Future date
            Duration = TimeSpan.FromMinutes(15),
            Location = "Meeting Room 1",
            CreatorId = DefaultUser.Id
        };

        await using var arrangeContext = await GetDbContextFactory().CreateDbContextAsync();
        arrangeContext.StandUpMeetings.Add(standUpMeeting);
        await arrangeContext.SaveChangesAsync();

        var calendar = await GetCalendarForProject(_project.Id);

        calendar.Should().NotBeNull();
        calendar.Events.Should().ContainSingle();
        var calendarEvent = calendar.Events.First();
        calendarEvent.Summary.Should().Be(standUpMeeting.Name);
        calendarEvent.Start.Should().Be(new CalDateTime(standUpMeeting.ScheduledStart));
        calendarEvent.End.Should().Be(new CalDateTime(standUpMeeting.ScheduledStart.Add(standUpMeeting.Duration)));
        calendarEvent.Location.Should().Be(standUpMeeting.Location);
    }

    [Fact]
    public async Task GetCalendarForProjectAsync_WithPastStandUps_ReturnsEmptyCalendar()
    {
        var pastStandUpMeeting = new StandUpMeeting
        {
            SprintId = _sprint.Id,
            Name = "Daily Scrum",
            ScheduledStart = DateTime.Now.AddDays(-1), // Past date
            Duration = TimeSpan.FromMinutes(15),
            Location = "Meeting Room 1",
            CreatorId = DefaultUser.Id
        };

        await using var arrangeContext = await GetDbContextFactory().CreateDbContextAsync();
        arrangeContext.StandUpMeetings.Add(pastStandUpMeeting);
        await arrangeContext.SaveChangesAsync();

        var calendar = await GetCalendarForProject(_project.Id);

        calendar.Should().NotBeNull();
        calendar.Events.Should().BeEmpty();
    }
}
