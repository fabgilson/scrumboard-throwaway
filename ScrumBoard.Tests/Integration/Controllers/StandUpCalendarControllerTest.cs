using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Controllers;

public class StandUpCalendarControllerTest : BaseIntegrationTestFixture
{
    public StandUpCalendarControllerTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper) { }

    private User _userInProject;
    private User _userNotInProject;
    private Project _project;
    private UserStandUpCalendarLink _calendarLink;
    
    private async Task<HttpResponseMessage> GetCalendarWithToken(string token)
    {
        return await HttpClient.GetAsync($"api/StandUpCalendar/GetByToken/{token}");
    }

    private static async Task<Calendar> GetCalendarFromResponse(HttpResponseMessage responseMessage)
    {
        var calendarContent = await responseMessage.Content.ReadAsStringAsync();
        return Calendar.Load(calendarContent);
    }

    private static void CalendarEventShouldMatchStandUp(CalendarEvent calendarEvent, StandUpMeeting standUpMeeting)
    {
        calendarEvent.Summary.Should().Be(standUpMeeting.Name);
        calendarEvent.Start.Should().BeEquivalentTo(new CalDateTime(standUpMeeting.ScheduledStart));
        calendarEvent.End.Should().BeEquivalentTo(new CalDateTime(standUpMeeting.ScheduledStart.Add(standUpMeeting.Duration)));
        calendarEvent.Location.Should().Be(standUpMeeting.Location);
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _userInProject = FakeDataGenerator.CreateFakeUser();
        _userNotInProject = FakeDataGenerator.CreateFakeUser();
        await dbContext.Users.AddRangeAsync(_userInProject, _userNotInProject);
        
        _project = FakeDataGenerator.CreateFakeProject(developers: new[] {_userInProject});
        await dbContext.Projects.AddAsync(_project);
        
        // Create a link for a user that is a member of the project 
        _calendarLink = new UserStandUpCalendarLink
        {
            UserId = _userInProject.Id,
            ProjectId = _project.Id,
            Token = "user-is-in-project-link"
        };
        await dbContext.UserStandUpCalendarLinks.AddAsync(_calendarLink);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CalendarRequested_NonExistentIdGiven_NotFoundReturned()
    {
        var response = await GetCalendarWithToken("does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalendarRequested_UserNoLongerBelongsToProject_BadRequestReturned()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        // Create a link for a user that is not a member of the project 
        var newLink = new UserStandUpCalendarLink
        {
            UserId = _userNotInProject.Id,
            ProjectId = _project.Id,
            Token = "user-not-in-project-link"
        };
        await context.UserStandUpCalendarLinks.AddAsync(newLink);
        await context.SaveChangesAsync();
        
        var response = await GetCalendarWithToken("user-not-in-project-link");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidCalendarRequested_NoStandUps_EmptyCalendarReturned()
    {
        var response = await GetCalendarWithToken(_calendarLink.Token);
        
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

            var calendar = await GetCalendarFromResponse(response);
            calendar.Events.Should().BeEmpty();
        }
    }
    
    [Fact]
    public async Task ValidCalendarRequested_StandUpInFuture_CalendarWithOneEventReturned()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var upcomingStandUp = FakeDataGenerator.CreateFakeStandUp(FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project));
        upcomingStandUp.ScheduledStart = DateTime.Now.AddDays(1);
        await context.StandUpMeetings.AddAsync(upcomingStandUp);
        await context.SaveChangesAsync();
        
        var response = await GetCalendarWithToken(_calendarLink.Token);
        
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

            var calendar = await GetCalendarFromResponse(response);
            calendar.Events.Should().ContainSingle();
            CalendarEventShouldMatchStandUp(calendar.Events[0], upcomingStandUp);
        }
    }
    
    [Fact]
    public async Task ValidCalendarRequested_StandUpInPast_EmptyCalendarReturned()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var pastStandUp = FakeDataGenerator.CreateFakeStandUp(FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project));
        pastStandUp.ScheduledStart = DateTime.Now.AddDays(-1);
        await context.StandUpMeetings.AddAsync(pastStandUp);
        await context.SaveChangesAsync();
        
        var response = await GetCalendarWithToken(_calendarLink.Token);
        
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

            var calendar = await GetCalendarFromResponse(response);
            calendar.Events.Should().BeEmpty();
        }
    }
    
    [Fact]
    public async Task ValidCalendarRequested_StandUpInPastAndStandUpInFuture_CalendarWithSingleStandUpReturned()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var pastStandUp = FakeDataGenerator.CreateFakeStandUp(FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project));
        pastStandUp.ScheduledStart = DateTime.Now.AddDays(-1);
        var upcomingStandUp = FakeDataGenerator.CreateFakeStandUp(FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project));
        upcomingStandUp.ScheduledStart = DateTime.Now.AddDays(1);
        
        await context.StandUpMeetings.AddRangeAsync(pastStandUp, upcomingStandUp);
        await context.SaveChangesAsync();
        
        var response = await GetCalendarWithToken(_calendarLink.Token);
        
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

            var calendar = await GetCalendarFromResponse(response);
            calendar.Events.Should().ContainSingle();
            CalendarEventShouldMatchStandUp(calendar.Events[0], upcomingStandUp);
        }
    }
    
    [Fact]
    public async Task ValidCalendarRequested_MultipleFutureStandUps_CalendarWithMultipleStandUpsReturned()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();

        var upcomingStandUps = new List<StandUpMeeting>();
        for (var i = 0; i < 5; i++)
        {
            var upcomingStandUp = FakeDataGenerator.CreateFakeStandUp(FakeDataGenerator.CreateFakeSprintWithDatabaseProject(_project));
            upcomingStandUp.ScheduledStart = DateTime.Now.AddDays(i);
            upcomingStandUps.Add(upcomingStandUp);
        }
        await context.StandUpMeetings.AddRangeAsync(upcomingStandUps);
        await context.SaveChangesAsync();
        
        var response = await GetCalendarWithToken(_calendarLink.Token);
        
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

            var calendar = await GetCalendarFromResponse(response);
            calendar.Events.Should().HaveCount(5);
            for (var i = 0; i < 5; i++)
            {
                CalendarEventShouldMatchStandUp(calendar.Events[i], upcomingStandUps[i]);
            }
        }
    }
}