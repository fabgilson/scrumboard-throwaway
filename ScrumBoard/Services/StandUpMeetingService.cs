using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services;

public interface IStandUpMeetingService
{
    /// <summary>
    /// Adds a new scheduled stand-up meeting for some sprint of some project.
    /// </summary>
    /// <param name="standUpMeeting">Stand-up meeting to add</param>
    /// <param name="sprint">Sprint to which stand-up meeting is being added</param>
    /// <param name="creator">User who is scheduling the sprint</param>
    /// <returns>Created stand-up meeting entity after it has been added to DB</returns>
    Task ScheduleNewStandUpMeetingForSprintAsync(StandUpMeeting standUpMeeting, Sprint sprint, User creator);

    /// <summary>
    /// Updates an already existing stand-up meeting in the database,
    /// </summary>
    /// <param name="standUpMeeting">Stand-up meeting entity to update.</param>
    /// <param name="editor">User who is performing the update</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to update a stand-up meeting that does not already exist in the database
    /// </exception>
    Task UpdateStandUpMeetingAsync(StandUpMeeting standUpMeeting, User editor);

    /// <summary>
    /// Gets all stand-ups in a sprint for which the scheduled start datetime is in the future - paginated.
    /// </summary>
    /// <param name="sprint">Sprint for which to gather upcoming stand-ups</param>
    /// <param name="pageNum">Page number of results to get - first page = 1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <returns>Paginated list of upcoming stand-ups within a sprint</returns>
    Task<PaginatedList<StandUpMeeting>> GetPaginatedUpcomingStandUpsForSprintAsync(Sprint sprint, int pageNum,
        int pageSize);

    /// <summary>
    /// Gets all stand-ups in a sprint for which the scheduled start datetime is in the past - paginated.
    /// </summary>
    /// <param name="sprint">Sprint for which to gather past stand-ups</param>
    /// <param name="pageNum">Page number of results to get - first page = 1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <returns>Paginated list of past stand-ups within a sprint</returns>
    Task<PaginatedList<StandUpMeeting>>
        GetPaginatedPastStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize);

    /// <summary>
    /// Gets all stand-ups for all projects, unless a list of projects is given to constrain search to.
    /// </summary>
    /// <param name="pageNum">Page number of results to get - first page = 1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <param name="filteredProjects">If not null or empty, only stand-ups belonging to these projects will be returned.</param>
    /// <param name="limitToActiveSprints">If true, only returns stand-ups inside a sprint that is in-progress or ready for review</param>
    /// <returns>Paginated list of all stand-ups for all projects (or just a sub-set of projects)</returns>
    Task<PaginatedList<StandUpMeeting>> GetPaginatedAllUpcomingStandUpsForProjects(
        int pageNum,
        int pageSize,
        IEnumerable<Project> filteredProjects,
        bool limitToActiveSprints
    );

    /// <summary>
    /// Gets the count of stand-ups meetings scheduled in a given sprint.
    /// </summary>
    /// <param name="sprint">Sprint for which to count stand-up meetings.</param>
    /// <returns>Count of stand-up meetings scheduled in given sprint.</returns>
    Task<int> GetStandUpMeetingCountInSprintAsync(Sprint sprint);

    /// <summary>
    /// Gets all stand-ups whose time periods (start + duration) overlap with some proposed stand-up meeting shape.
    /// </summary>
    /// <param name="standUpMeetingShape">Proposed stand-up to get overlaps for</param>
    /// <param name="sprint">Sprint in which to consider overlaps</param>
    /// <param name="existingId">Optional, ignores stand-up with given ID from overlap checks</param>
    /// <returns>Enumerable of stand-ups that overlap with proposed stand-up shape</returns>
    Task<IEnumerable<StandUpMeeting>> GetOverlappingStandUpsAsync(IStandUpMeetingShape standUpMeetingShape,
        Sprint sprint, long? existingId);

    /// <summary>
    /// Get a stand-up meeting by its unique ID
    /// </summary>
    /// <param name="id">Unique ID by which to get stand-up meeting</param>
    /// <returns>Stand-up meeting with given ID</returns>
    Task<StandUpMeeting> GetByIdAsync(long id);

    /// <summary>
    /// Using the value of <see cref="StandUpMeetingService.LookForwardPeriodForUpcomingStandUp"/>, looks into the future
    /// to find the next upcoming stand-up within that period. If no such stand-up is found, returns null.
    /// </summary>
    /// <param name="user">User for whom to find the next upcoming stand-up</param>
    /// <param name="project">Project in which to find the next upcoming stand-up</param>
    /// <returns>Next stand-up for user in project within upcoming period, or null if none found</returns>
    Task<StandUpMeeting> GetUpcomingStandUpIfPresentAsync(User user, Project project);
}

public class StandUpMeetingService(
    IStandUpMeetingRepository standUpMeetingRepository,
    IStandUpMeetingChangelogRepository standUpChangeLogRepository
) : IStandUpMeetingService
{
    public static readonly TimeSpan LookForwardPeriodForUpcomingStandUp = TimeSpan.FromDays(7);
    public static readonly TimeSpan AllowCheckInBeforeStandUpPeriod = TimeSpan.FromDays(3);

    /// <inheritdoc/>
    public async Task ScheduleNewStandUpMeetingForSprintAsync(StandUpMeeting standUpMeeting, Sprint sprint, User creator)
    {
        standUpMeeting.Created = DateTime.Now;
        standUpMeeting.SprintId = sprint.Id;
        standUpMeeting.CreatorId = creator.Id;
        await standUpMeetingRepository.ScheduleNewStandUpMeetingAsync(standUpMeeting);
    }

    /// <inheritdoc/>
    public async Task UpdateStandUpMeetingAsync(StandUpMeeting standUpMeeting, User editor)
    {
        var old = await standUpMeetingRepository.GetByIdAsync(standUpMeeting.Id);
        if (old is null) throw new InvalidOperationException("Stand-up meeting must already exist in database");
        await standUpMeetingRepository.UpdateStandUpAndMembershipsAsync(standUpMeeting);
        var changes = ApplyChanges(editor, old, standUpMeeting);
        if (changes is not null) await standUpChangeLogRepository.AddAllAsync(changes);
    }

    /// <inheritdoc/>
    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedUpcomingStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize)
    {
        return await standUpMeetingRepository.GetPaginatedUpcomingStandUpsForSprintAsync(sprint, pageNum, pageSize);
    }

    /// <inheritdoc/>
    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedPastStandUpsForSprintAsync(Sprint sprint, int pageNum, int pageSize)
    {
        return await standUpMeetingRepository.GetPaginatedPastStandUpsForSprintAsync(sprint, pageNum,  pageSize);
    }

    /// <inheritdoc/>
    public async Task<int> GetStandUpMeetingCountInSprintAsync(Sprint sprint)
    {
        return await standUpMeetingRepository.GetStandUpMeetingCountInSprintAsync(sprint);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StandUpMeeting>> GetOverlappingStandUpsAsync(IStandUpMeetingShape standUpMeetingShape, Sprint sprint, long? existingId=null)
    {
        return await standUpMeetingRepository.GetOverlappingStandUpsAsync(standUpMeetingShape, sprint, existingId);
    }

    /// <inheritdoc/>
    public async Task<StandUpMeeting> GetByIdAsync(long id)
    {
        return await standUpMeetingRepository.GetByIdAsync(id);
    }

    /// <inheritdoc/>
    public async Task<StandUpMeeting> GetUpcomingStandUpIfPresentAsync(User user, Project project)
    {
        return await standUpMeetingRepository.GetNextForUserInProjectBeforeAsync(user, project,
            DateTime.Now.Add(LookForwardPeriodForUpcomingStandUp));
    }

    /// <inheritdoc/>
    public async Task<PaginatedList<StandUpMeeting>> GetPaginatedAllUpcomingStandUpsForProjects(
        int pageNum, 
        int pageSize, 
        IEnumerable<Project> filteredProjects,
        bool limitToActiveSprints
    ) {
        return await standUpMeetingRepository.GetPaginatedAllUpcomingStandUpsForProjects(
            pageNum, 
            pageSize, 
            filteredProjects, 
            limitToActiveSprints
        );
    }
    
    /// <summary>
    /// Generates a set of changelog objects based on the differences between some old and new values of a stand-up meeting
    /// </summary>
    /// <param name="actingUser">The user to whom the changes should be recorded</param>
    /// <param name="oldValue">The old value of the stand-up meeting, before changes are applied</param>
    /// <param name="newValue">The new value of the stand-up meeting, after changes are applied</param>
    /// <returns></returns>
    private static IEnumerable<StandUpMeetingChangelogEntry> ApplyChanges(User actingUser, IStandUpMeetingShape oldValue, StandUpMeeting newValue)
    {
        var removals = oldValue.ExpectedAttendances
            .Select(membership => new
            {
                membership, 
                newMembership = newValue.ExpectedAttendances.FirstOrDefault(assoc => assoc.User.Id == membership.User.Id),
                user = membership.User
            })
            .Where(t => t.newMembership == null)
            .Select(t => new StandUpMeetingUserMembershipChangelogEntry(
                actingUser, newValue, t.user, Change<object>.Delete(t.membership.User))
            ).ToList();
        
        var additions = newValue.ExpectedAttendances
            .Where(membership => oldValue.ExpectedAttendances.All(assoc => assoc.User.Id != membership.User.Id))
            .Select(membership => new StandUpMeetingUserMembershipChangelogEntry(
                actingUser, newValue, membership.User, Change<object>.Create(membership.User))
            ).ToList();
        
        var changes = ShapeUtils.ApplyChanges(newValue, oldValue, nameof(StandUpMeeting.ExpectedAttendances))
            .Select(fieldAndChange => new StandUpMeetingChangelogEntry(
                actingUser, newValue, fieldAndChange.Item1, fieldAndChange.Item2)).ToList();

        changes.AddRange(removals);
        changes.AddRange(additions);
        
        return changes;
    }
}