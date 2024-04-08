using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Shapes;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Repositories;

public interface IStandUpMeetingRepository : IRepository<StandUpMeeting>
{
    Task<StandUpMeeting> GetByIdAsync(long id);
    Task<IEnumerable<StandUpMeeting>> GetByIdsAsync(IEnumerable<long> ids);
    Task<PaginatedList<StandUpMeeting>> GetPaginatedUpcomingStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize);
    Task<PaginatedList<StandUpMeeting>> GetPaginatedPastStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize);
    Task<int> GetStandUpMeetingCountInSprintAsync(Sprint sprint);
    Task<IEnumerable<StandUpMeeting>> GetOverlappingStandUpsAsync(IStandUpMeetingShape standUpMeetingShape, Sprint sprint, long? existingId);
    Task UpdateStandUpAndMembershipsAsync(StandUpMeeting standUpMeeting);
    Task ScheduleNewStandUpMeetingAsync(StandUpMeeting standUpMeeting);
    Task<StandUpMeeting> GetStandUpMeetingPriorTo(StandUpMeeting standUpMeeting);
    
    /// <summary>
    /// Attempts to find the stand-up meeting occurring (in the same sprint) directly after the given stand-up.
    /// </summary>
    /// <param name="standUpMeeting">Stand-up meeting for which to find successor</param>
    /// <returns>Stand-up meetings occurring in sprint directly after given stand-up, or null if given stand-up was last in sprint</returns>
    Task<StandUpMeeting> GetStandUpMeetingAfter(StandUpMeeting standUpMeeting);
    Task<StandUpMeeting> GetNextForUserInProjectBeforeAsync(User user, Project project, DateTime before);
    Task<PaginatedList<StandUpMeeting>> GetPaginatedAllUpcomingStandUpsForProjects(
        int pageNum, 
        int pageSize, 
        IEnumerable<Project> filteredProjects,
        bool limitToActiveSprints
    );
    
    /// <summary>
    /// Returns all stand-ups that a user is an attendee for within some given sprint
    /// </summary>
    /// <param name="sprint">The sprint in which to find the user's stand-ups</param>
    /// <param name="user">The user for whom to find attending stand-ups</param>
    /// <returns>All stand-ups within the given sprint for which the user is marked as an attendee</returns>
    Task<IEnumerable<StandUpMeeting>> GetAllStandUpsForUserForSprintAsync(Sprint sprint, User user);
}

public class StandUpMeetingRepository : Repository<StandUpMeeting>, IStandUpMeetingRepository
{
    public StandUpMeetingRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<StandUpMeetingRepository> logger) 
        : base(dbContextFactory, logger) { }

    /// <summary>
    /// Returns a stand-up meeting with given ID if it exists, null otherwise.
    /// </summary>
    /// <param name="id">ID of stand-up meeting entity to find</param>
    /// <returns>Stand-up meeting with given ID, or null if no such entity is found</returns>
    public async Task<StandUpMeeting> GetByIdAsync(long id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => x.Id == id)
            .Include(x => x.Sprint)
            .Include(x => x.ExpectedAttendances)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<StandUpMeeting>> GetByIdsAsync(IEnumerable<long> ids)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => ids.Contains(x.Id))
            .Include(x => x.Sprint)
            .Include(x => x.ExpectedAttendances)
            .ThenInclude(x => x.User)
            .ToListAsync();
    }

    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedUpcomingStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await PaginatedList<StandUpMeeting>.CreateAsync(
            context.StandUpMeetings
                .Where(x => x.SprintId == sprint.Id && x.ScheduledStart > DateTime.Now)
                .Include(x => x.Sprint)
                .OrderBy(x => x.ScheduledStart),
            pageNum,
            pageSize
        );
    }

    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedPastStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await PaginatedList<StandUpMeeting>.CreateAsync(
            context.StandUpMeetings
                .Where(x => x.SprintId == sprint.Id && x.ScheduledStart < DateTime.Now)
                .Include(x => x.Sprint)
                .OrderByDescending(x => x.ScheduledStart),
            pageNum,
            pageSize
        );
    }

    public async Task<int> GetStandUpMeetingCountInSprintAsync(Sprint sprint)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => x.SprintId == sprint.Id)
            .CountAsync();
    }

    public async Task<IEnumerable<StandUpMeeting>> GetOverlappingStandUpsAsync(IStandUpMeetingShape standUpMeetingShape, Sprint sprint, long? existingId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.StandUpMeetings.Where(x =>
            ((existingId.HasValue && x.Id != existingId) || !existingId.HasValue)
            && x.SprintId == sprint.Id 
            && standUpMeetingShape.ScheduledStart < x.ScheduledStart + x.Duration
            && x.ScheduledStart < standUpMeetingShape.ScheduledStart + standUpMeetingShape.Duration
        ).ToArrayAsync();
    }

    public async Task UpdateStandUpAndMembershipsAsync(StandUpMeeting standUpMeeting)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var existingStandUp = await GetByIdAsync(standUpMeeting.Id);
        if (existingStandUp is null) throw new InvalidOperationException("StandUp with given ID must exist in DB");
        
        context.Entry(existingStandUp).CurrentValues.SetValues(standUpMeeting);
        context.Entry(existingStandUp).State = EntityState.Modified;
        
        // Delete children
        foreach (var attendance in existingStandUp.ExpectedAttendances)
        {
            if (standUpMeeting.ExpectedAttendances.All(c => c.User.Id != attendance.User.Id))
                context.StandUpMeetingAttendance.Remove(attendance);
        }

        // Update and Insert children
        foreach (var attendance in standUpMeeting.ExpectedAttendances)
        {
            var existingAttendance = existingStandUp.ExpectedAttendances
                .SingleOrDefault(c => c.UserId == attendance.UserId && c.StandUpMeetingId == standUpMeeting.Id);

            var attendanceToSave = new StandUpMeetingAttendance
            {
                ArrivedAt = attendance.ArrivedAt,
                UserId = attendance.UserId,
                StandUpMeetingId = attendance.StandUpMeetingId
            };

            if (existingAttendance != null)
                context.Entry(existingAttendance).CurrentValues.SetValues(attendanceToSave);
            else
                existingStandUp.ExpectedAttendances.Add(attendanceToSave);
        }
        
        await context.SaveChangesAsync();
    }

    public async Task ScheduleNewStandUpMeetingAsync(StandUpMeeting standUpMeeting)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var attendances = standUpMeeting.ExpectedAttendances.ToList();
        var sprint = standUpMeeting.Sprint;
        standUpMeeting.ExpectedAttendances = null;
        standUpMeeting.Sprint = null;
        
        await context.AddAsync(standUpMeeting);
        attendances = attendances.Select(attendance => new StandUpMeetingAttendance
        {
            ArrivedAt = attendance.ArrivedAt,
            UserId = attendance.UserId,
            StandUpMeetingId = attendance.StandUpMeetingId
        }).ToList();
        await context.AddRangeAsync(attendances);

        standUpMeeting.ExpectedAttendances = attendances;
        await context.SaveChangesAsync();
        
        standUpMeeting.Sprint = sprint;
    }

    /// <summary>
    /// Get the stand-up meeting in a sprint that was scheduled prior to some other stand-up meeting.
    /// If the given stand-up meeting is the first scheduled stand-up in a sprint, returns null.
    /// </summary>
    /// <param name="standUpMeeting">Stand-up meeting for which to find the meeting prior</param>
    /// <returns>Stand-up meeting prior to given stand-up meeting, or null if given stand-up was first in sprint</returns>
    public async Task<StandUpMeeting> GetStandUpMeetingPriorTo(StandUpMeeting standUpMeeting)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => x.SprintId == standUpMeeting.SprintId)
            .Where(x => x.ScheduledStart < standUpMeeting.ScheduledStart)
            .OrderBy(x => x.ScheduledStart)
            .LastOrDefaultAsync();
    }

    public async Task<StandUpMeeting> GetStandUpMeetingAfter(StandUpMeeting standUpMeeting)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => x.SprintId == standUpMeeting.SprintId)
            .Where(x => x.ScheduledStart > standUpMeeting.ScheduledStart)
            .OrderBy(x => x.ScheduledStart)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get the next stand-up that a user is expected to attend in some project which starts before some given cutoff
    /// datetime. Note that stand-ups meetings currently in progress may be returned here.
    /// </summary>
    /// <param name="user">User for whom to find the next upcoming stand-up</param>
    /// <param name="project">Project in which to find the next upcoming stand-up</param>
    /// <param name="before">Only accept stand-ups that begin before this datetime</param>
    /// <returns>Next stand-up for user in project within upcoming period, or null if none found</returns>
    public async Task<StandUpMeeting> GetNextForUserInProjectBeforeAsync(User user, Project project, DateTime before)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        // Call .ToListAsync after getting stand-ups in a sprint to allow for dynamic datetime checking
        var standUpsAfterCutoff = await context.StandUpMeetings
            .Where(x => x.Sprint.SprintProjectId == project.Id)
            .Where(x => x.Sprint.Stage == SprintStage.Started || x.Sprint.Stage == SprintStage.Created)
            .Where(x => x.ScheduledStart < before)
            .Where(x => x.ExpectedAttendances.Select(a => a.UserId).Contains(user.Id))
            .OrderBy(x => x.ScheduledStart)
            .ToListAsync();

        return standUpsAfterCutoff.FirstOrDefault(x => x.ScheduledStart.Add(x.Duration) > DateTime.Now);
    }

    /// <summary>
    /// Returns a paginated list of all upcoming stand-ups from all projects. This list can be optionally filtered
    /// by specifying a list of projects from which to get stand-ups, or by specifying that only stand-ups
    /// from a currently active sprint should be returned.
    /// </summary>
    /// <param name="pageNum">Number of page to return, first page = 1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <param name="filteredProjects">Optional, if given will only return stand-ups that belong to these projects</param>
    /// <param name="limitToActiveSprints">If true, only returns stand-ups inside a sprint that is in-progress or ready for review</param>
    /// <returns>Paginated list of upcoming stand-ups for all projects, or the stand-ups matching filter criteria.</returns>
    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedAllUpcomingStandUpsForProjects(
        int pageNum, 
        int pageSize, 
        IEnumerable<Project> filteredProjects,
        bool limitToActiveSprints
    ) {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var filteredIdArray = filteredProjects.Select(x => x.Id).ToArray();
        var startingSource = filteredIdArray.IsNullOrEmpty()
            ? context.StandUpMeetings
            : context.StandUpMeetings.Where(x => filteredIdArray.Contains(x.Sprint.SprintProjectId));

        startingSource = limitToActiveSprints
            ? startingSource.Where(x => x.Sprint.Stage == SprintStage.Started || x.Sprint.Stage == SprintStage.ReadyToReview)
            : startingSource;
        
        return await PaginatedList<StandUpMeeting>.CreateAsync(
                startingSource
                .Where(x => x.ScheduledStart > DateTime.Now)
                .Include(x => x.Sprint).ThenInclude(s => s.Project)
                .OrderBy(x => x.ScheduledStart),
            pageNum,
            pageSize
        );
    }
    
    public async Task<IEnumerable<StandUpMeeting>> GetAllStandUpsForUserForSprintAsync(Sprint sprint, User user)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StandUpMeetings
            .Where(x => sprint == null || x.SprintId == sprint.Id)
            .Where(x => x.ExpectedAttendances.Any(a => a.UserId == user.Id))
            .Include(x => x.Sprint)
            .ToListAsync();
    }
}