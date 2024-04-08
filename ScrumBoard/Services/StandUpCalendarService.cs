using System;
using System.Linq;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

/// <summary>
/// Interface defining operations for managing stand-up calendar links for users.
/// </summary>
public interface IStandUpCalendarService
{
    /// <summary>
    /// Creates a new stand-up calendar link for a user within a specific project.
    /// </summary>
    /// <param name="userId">The user's identifier for whom the link is being created.</param>
    /// <param name="projectId">The project's identifier for which the link is being created.</param>
    /// <returns>The created UserStandUpCalendarLink.</returns>
    Task<UserStandUpCalendarLink> CreateStandUpCalendarLinkAsync(long userId, long projectId);

    /// <summary>
    /// Resets the token for an existing stand-up calendar link for a user within a specific project.
    /// </summary>
    /// <param name="userId">The user's identifier for whom the token is being reset.</param>
    /// <param name="projectId">The project's identifier for which the token is being reset.</param>
    /// <returns>The updated UserStandUpCalendarLink with a new token.</returns>
    Task<UserStandUpCalendarLink> ResetTokenForStandUpCalendarLink(long userId, long projectId);

    /// <summary>
    /// Deletes an existing stand-up calendar link for a user within a specific project.
    /// </summary>
    /// <param name="userId">The user's identifier for whom the link is being deleted.</param>
    /// <param name="projectId">The project's identifier for which the link is being deleted.</param>
    /// <returns>True if deletion was successful, false if there was no calendar to delete.</returns>
    Task DeleteStandUpCalendarLinkAsync(long userId, long projectId);
    
    /// <summary>
    /// Retrieves a stand-up calendar link for a specific user and project.
    /// </summary>
    /// <param name="userId">The user's identifier for whom the calendar link is being retrieved.</param>
    /// <param name="projectId">The project's identifier for which the calendar link is being retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the UserStandUpCalendarLink if found, or null if no link exists.
    /// </returns>
    Task<UserStandUpCalendarLink> GetStandUpCalendarLinkAsync(long userId, long projectId);
    
    /// <summary>
    /// Retrieves a stand-up calendar link by its unique token.
    /// </summary>
    /// <param name="token">The token of the stand-up calendar link to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the UserStandUpCalendarLink associated with the provided token, or null if not found.</returns>
    Task<UserStandUpCalendarLink> GetStandUpCalendarLinkByTokenAsync(string token);

    Task<Calendar> GetCalendarForProjectAsync(long projectId);
}


public class StandUpCalendarService : IStandUpCalendarService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IClock _clock;

    public StandUpCalendarService(IDbContextFactory<DatabaseContext> dbContextFactory, IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<UserStandUpCalendarLink> CreateStandUpCalendarLinkAsync(long userId, long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var existingLink = await GetStandUpCalendarLinkWithExistingContextAsync(userId, projectId, context);
        if (existingLink != null) throw new InvalidOperationException("A link already exists between this user and project");
        
        var newLink = new UserStandUpCalendarLink
        {
            UserId = userId,
            ProjectId = projectId,
            Token = Guid.NewGuid().ToString()
        };
        
        await context.UserStandUpCalendarLinks.AddAsync(newLink);
        await context.SaveChangesAsync();
        return newLink;
    }

    public async Task<UserStandUpCalendarLink> ResetTokenForStandUpCalendarLink(long userId, long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var link = await GetStandUpCalendarLinkWithExistingContextAsync(userId, projectId, context);
        if (link == null) throw new InvalidOperationException("No link found to reset token for.");
        
        link.Token = Guid.NewGuid().ToString();
        context.UserStandUpCalendarLinks.Update(link);
        await context.SaveChangesAsync();
        return link;
    }

    public async Task DeleteStandUpCalendarLinkAsync(long userId, long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var link = await GetStandUpCalendarLinkWithExistingContextAsync(userId, projectId, context);
        if (link == null) return;
        
        context.UserStandUpCalendarLinks.Remove(link);
        await context.SaveChangesAsync();
    }
    
    public async Task<UserStandUpCalendarLink> GetStandUpCalendarLinkAsync(long userId, long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await GetStandUpCalendarLinkWithExistingContextAsync(userId, projectId, context);
    }
    
    public async Task<UserStandUpCalendarLink> GetStandUpCalendarLinkByTokenAsync(string token)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var link = await context.UserStandUpCalendarLinks.FirstOrDefaultAsync(x => x.Token == token);
        return link;
    }
    
    private static async Task<UserStandUpCalendarLink> GetStandUpCalendarLinkWithExistingContextAsync(long userId, long projectId, DatabaseContext context)
    {
        return await context.UserStandUpCalendarLinks.FindAsync(userId, projectId);
    }
    
    public async Task<Calendar> GetCalendarForProjectAsync(long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var standUps = context.StandUpMeetings
            .Where(x => x.Sprint.Project.Id == projectId)
            .Where(x => x.ScheduledStart > _clock.Now);

        var calendar = new Calendar();
        var standUpCalendarEvents = standUps.Select(standUp => new CalendarEvent
        {
            Summary = standUp.Name,
            Start = new CalDateTime(standUp.ScheduledStart),
            End = new CalDateTime(standUp.ScheduledStart.Add(standUp.Duration)),
            Location = standUp.Location
        });
        calendar.Events.AddRange(standUpCalendarEvents);
        return calendar;
    }
}