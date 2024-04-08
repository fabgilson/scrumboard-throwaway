using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface IWeeklyReflectionCheckInService
{
    /// <summary>
    /// Retrieve the weekly check-in for a user for the specified week and year if it exists.
    /// If no such check-in exists, then null is returned. 
    /// </summary>
    /// <param name="userId">ID of user for whom to fetch check-in.</param>
    /// <param name="projectId">ID of project from which to fetch user's check-in.</param>
    /// <param name="isoWeekNumber">ISO week number for which to fetch check-in.</param>
    /// <param name="year">Year number for which to fetch check-in.</param>
    /// <returns>Check-in for user for specified week and year if it exists, null otherwise.</returns>
    Task<WeeklyReflectionCheckIn> GetCheckInForUserForIsoWeekAndYear(long userId, long projectId, int isoWeekNumber, int year);

    /// <summary>
    /// Retrieves a collection of tasks that a user has worked on during a specified ISO week and year, within a specific project.
    /// Optionally includes active tasks currently assigned to the user that haven't had work logged yet.
    /// </summary>
    /// <param name="userId">The user's ID for whom tasks are being retrieved.</param>
    /// <param name="projectId">The project's ID from which tasks are being retrieved.</param>
    /// <param name="isoWeekNumber">The ISO week number for which tasks are being retrieved.</param>
    /// <param name="year">The year for which tasks are being retrieved.</param>
    /// <param name="includeCurrentlyAssignedTasks">Whether to include tasks that are currently assigned to the user.</param>
    /// <returns>UserStoryTasks worked on (or assigned to and still active), for user in given week.</returns>
    Task<ICollection<UserStoryTask>> GetTasksWorkedOrAssignedToUserForIsoWeekAndYear(long userId, long projectId, int isoWeekNumber, int year, bool includeCurrentlyAssignedTasks);

    /// <summary>
    /// Saves a weekly reflection check-in for a user within a specific project. Creates the check-in if it does not already exist,
    /// otherwise updates the existing check-in value.
    /// </summary>
    /// <param name="checkInValue">The check-in data to be saved.</param>
    /// <param name="userId">The ID of the user for whom the check-in is being saved.</param>
    /// <param name="projectId">The ID of the project for which the check-in is being saved.</param>
    /// <param name="editingSessionGuid">Optional, if given will ensure that only a single set of changelogs is generated for this GUID.</param>
    Task SaveCheckInForUserAsync(WeeklyReflectionCheckIn checkInValue, long userId, long projectId, Guid? editingSessionGuid);

    /// <summary>
    /// Saves a task check-in associated with a weekly reflection check-in.
    /// </summary>
    /// <param name="taskCheckInValue">The task check-in data to be saved.</param>
    /// <param name="weeklyCheckInId">The ID of the weekly reflection check-in the task check-in is associated with.</param>
    Task SaveTaskCheckInAsync(TaskCheckIn taskCheckInValue, long weeklyCheckInId);

    /// <summary>
    /// Retrieves a task check-in for a specified weekly reflection check-in and task.
    /// </summary>
    /// <param name="weeklyCheckInId">The ID of the weekly reflection check-in the task check-in is associated with.</param>
    /// <param name="taskId">The ID of the task for which the check-in is being retrieved.</param>
    /// <returns>Given check-in if it exists, otherwise null.</returns>
    Task<TaskCheckIn> GetTaskCheckInAsync(long weeklyCheckInId, long taskId);

    /// <summary>
    /// Returns all of a users task check-ins within a project, optionally filtering by sprint.
    /// If given sprint is null, all check-ins within the project are returned.
    /// </summary>
    /// <param name="projectId">Project in which to get check-ins</param>
    /// <param name="sprintId">Sprint to filter by, if null, returns all check-ins</param>
    /// <param name="userId">User for whom to get check-ins</param>
    /// <returns>All check-ins for the user in the project, optionally filtered by sprint</returns>
    Task<IEnumerable<WeeklyReflectionCheckIn>> GetAllCheckInsForUserForProjectAsync(long projectId, long? sprintId, long userId);

    /// <summary>
    /// Calculates the time spent by a user on tasks within specific check-ins.
    ///
    /// Note: The provided task check-ins must already have their WeeklyCheckIn navigation loaded.
    /// </summary>
    /// <param name="checkIns">A collection of task check-ins to calculate time spent for.</param>
    /// <param name="userId">The user's ID for whom the time spent is being calculated.</param>
    /// <returns>
    /// The task result contains a dictionary where each key is a <see cref="TaskCheckIn"/> and its value
    /// is the associated <see cref="TimeSpentOnTask"/>.
    /// </returns>
    Task<IDictionary<TaskCheckIn, TimeSpentOnTask>> GetTimeSpentForTasksInCheckInsAsync(ICollection<TaskCheckIn> checkIns, long userId);

    /// <summary>
    /// Returns all changelog entries belonging to some reflection check-in, ordered with the oldest changes last.
    /// </summary>
    /// <param name="checkInId">ID of weekly reflection check-in for which to retrieve changelogs</param>
    /// <returns>List of changelogs for given check-in</returns>
    Task<ICollection<WeeklyReflectionCheckInChangelogEntry>> GetChangelogsForCheckInAsync(long checkInId);
}

public class WeeklyReflectionCheckInService : IWeeklyReflectionCheckInService
{
    private readonly IClock _clock;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IUserStatsService _userStatsService;
    private readonly IChangelogService _changelogService;

    public WeeklyReflectionCheckInService(
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IClock clock, 
        IUserStatsService userStatsService, 
        IChangelogService changelogService
    ) {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
        _userStatsService = userStatsService;
        _changelogService = changelogService;
    }

    public async Task<WeeklyReflectionCheckIn> GetCheckInForUserForIsoWeekAndYear(long userId, long projectId, int isoWeekNumber, int year)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WeeklyReflectionCheckIns
            .Where(x => x.UserId == userId)
            .Where(x => x.ProjectId == projectId)
            .Where(x => x.Year == year)
            .FirstOrDefaultAsync(x => x.IsoWeekNumber == isoWeekNumber);
    }

    public async Task<ICollection<UserStoryTask>> GetTasksWorkedOrAssignedToUserForIsoWeekAndYear(long userId, long projectId, int isoWeekNumber, int year, bool includeCurrentlyAssignedTasks)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var assignedAndUnfinishedTasks = includeCurrentlyAssignedTasks
            ? await context.UserStoryTasks
                .Where(x => x.UserStory.ProjectId == projectId)
                .Where(x => x.UserAssociations.Any(a => a.UserId == userId && a.Role == TaskRole.Assigned))
                .Where(x => x.Stage != Stage.Done && x.Stage != Stage.Deferred)
                .Include(x => x.UserStory)
                .ToListAsync()
            : [];

        var startOfWeek = ISOWeek.ToDateTime(year, isoWeekNumber, DayOfWeek.Monday);
        var endOfWeek = startOfWeek.AddDays(7).AddMilliseconds(-1);
        var workedOnThisWeek = await context.TaggedWorkInstances
            .Where(twi => twi.WorklogEntry.UserId == userId)
            .Where(twi => twi.WorklogEntry.Occurred >= startOfWeek)
            .Where(twi => twi.WorklogEntry.Occurred <= endOfWeek)
            .Where(twi => twi.WorklogTag.Name != "Review")
            .Select(twi => twi.WorklogEntry.Task)
            .Distinct()
            .Include(x => x.UserStory)
            .ToListAsync();

        var combinedTasks = assignedAndUnfinishedTasks.UnionBy(workedOnThisWeek, x => x.Id);
        return combinedTasks.OrderBy(x => x.Name).ToList();
    }

    public async Task SaveCheckInForUserAsync(WeeklyReflectionCheckIn checkInValue, long userId, long projectId, Guid? editingSessionGuid)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var checkIn = checkInValue.Id == default
            ? null
            : await context.WeeklyReflectionCheckIns.FirstOrDefaultAsync(x => x.Id == checkInValue.Id);

        var changelogs = checkIn is null ? null : CalculateChangelogsForExistingCheckIn(checkIn, checkInValue, userId, editingSessionGuid);
        
        if (checkIn is not null) checkIn.LastUpdated = _clock.Now;
        
        checkIn ??= new WeeklyReflectionCheckIn
        {
            ProjectId = projectId,
            UserId = userId,
            IsoWeekNumber = checkInValue.IsoWeekNumber,
            Year = checkInValue.Year,
            Created = _clock.Now
        };
        
        checkIn.CompletionStatus = checkInValue.CompletionStatus;
        checkIn.WhatIDidWell = checkInValue.WhatIDidWell;
        checkIn.WhatIDidNotDoWell = checkInValue.WhatIDidNotDoWell;
        checkIn.WhatIWillDoDifferently = checkInValue.WhatIWillDoDifferently;
        checkIn.AnythingElse = checkInValue.AnythingElse;
        
        checkIn.TaskCheckIns = null;
        
        context.WeeklyReflectionCheckIns.Update(checkIn);
        await context.SaveChangesAsync();
        
        // If the changelogs so far is null, it means the check-in is new, and the ID is only now known
        changelogs ??= [new WeeklyReflectionCheckInChangelogEntry(checkIn, userId, null, Change<object>.Create(null), editingSessionGuid)];
        
        await _changelogService.SaveChangelogsAsync(changelogs, inDb => inDb.Where(x => x.WeeklyReflectionCheckInId == checkIn.Id));
    }

    public async Task SaveTaskCheckInAsync(TaskCheckIn taskCheckInValue, long weeklyCheckInId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var taskCheckIn = taskCheckInValue.Id == default
            ? null
            : await context.TaskCheckIns.FirstOrDefaultAsync(x => x.Id == taskCheckInValue.Id);

        if (taskCheckIn is not null) taskCheckIn.LastUpdated = _clock.Now;

        taskCheckIn ??= new TaskCheckIn
        {
            TaskId = taskCheckInValue.TaskId,
            Created = _clock.Now,
            WeeklyReflectionCheckInId = weeklyCheckInId
        };

        taskCheckIn.CheckInTaskStatus = taskCheckInValue.CheckInTaskStatus;
        taskCheckIn.CheckInTaskDifficulty = taskCheckInValue.CheckInTaskDifficulty;

        context.TaskCheckIns.Update(taskCheckIn);
        await context.SaveChangesAsync();
    }

    public async Task<TaskCheckIn> GetTaskCheckInAsync(long weeklyCheckInId, long taskId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.TaskCheckIns
            .Where(x => x.WeeklyReflectionCheckInId == weeklyCheckInId)
            .FirstOrDefaultAsync(x => x.TaskId == taskId);
    }

    public async Task<IEnumerable<WeeklyReflectionCheckIn>> GetAllCheckInsForUserForProjectAsync(long projectId, long? sprintId, long userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var sprint = sprintId is null ? null : await context.Sprints.FirstOrDefaultAsync(x => x.Id == sprintId);
        var allReflectionsForUserInProject = context.WeeklyReflectionCheckIns
            .Where(x => x.ProjectId == projectId)
            .Where(x => x.UserId == userId)
            .Include(x => x.TaskCheckIns)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.IsoWeekNumber);
        
        if (sprint is null)
        {
            return await allReflectionsForUserInProject.ToListAsync();
        }

        var isoWeekStart = ISOWeek.GetWeekOfYear(sprint.StartDate.ToDateTime(TimeOnly.MinValue));
        var isoWeekEnd = ISOWeek.GetWeekOfYear(sprint.EndDate.ToDateTime(TimeOnly.MaxValue));

        return await allReflectionsForUserInProject
            .Where(x => x.IsoWeekNumber >= isoWeekStart && x.Year >= sprint.StartDate.Year)
            .Where(x => x.IsoWeekNumber <= isoWeekEnd && x.Year <= sprint.EndDate.Year)
            .ToListAsync();
    }

    public async Task<IDictionary<TaskCheckIn, TimeSpentOnTask>> GetTimeSpentForTasksInCheckInsAsync(ICollection<TaskCheckIn> checkIns, long userId)
    {
        var taskTimeDict = new Dictionary<TaskCheckIn, TimeSpentOnTask>();
        if (!checkIns.Any()) return taskTimeDict;
        
        foreach (var checkIn in checkIns)
        {
            var start = ISOWeek.ToDateTime(checkIn.WeeklyReflectionCheckIn.Year, checkIn.WeeklyReflectionCheckIn.IsoWeekNumber, DayOfWeek.Monday);
            var end = ISOWeek.ToDateTime(checkIn.WeeklyReflectionCheckIn.Year, checkIn.WeeklyReflectionCheckIn.IsoWeekNumber, DayOfWeek.Sunday);
            
            var timeSpent = await _userStatsService.TagsWorkOnTaskByUser(checkIn.TaskId, userId, start, end);
            taskTimeDict.Add(checkIn, timeSpent);
        }
        
        return taskTimeDict;
    }

    public async Task<ICollection<WeeklyReflectionCheckInChangelogEntry>> GetChangelogsForCheckInAsync(long checkInId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WeeklyReflectionCheckInChangelogEntries
            .Where(x => x.WeeklyReflectionCheckInId == checkInId)
            .Include(x => x.Creator)
            .OrderByDescending(x => x.Created)
            .ToListAsync();
    }

    private static IEnumerable<WeeklyReflectionCheckInChangelogEntry> CalculateChangelogsForExistingCheckIn(
        WeeklyReflectionCheckIn existingCheckIn, 
        WeeklyReflectionCheckIn newValue, 
        long actingUserId, 
        Guid? editingSessionGuid
    )
    {
        if (existingCheckIn is null) throw new ArgumentException();
        
        var changes = ChangelogGenerator.GenerateChangesBetweenObjects(
            existingCheckIn,
            newValue,
            (nameof(existingCheckIn.CompletionStatus), existing => existing.CompletionStatus, incoming => incoming.CompletionStatus),
            (nameof(existingCheckIn.WhatIDidWell), existing => existing.WhatIDidWell, incoming => incoming.WhatIDidWell),
            (nameof(existingCheckIn.WhatIDidNotDoWell), existing => existing.WhatIDidNotDoWell, incoming => incoming.WhatIDidNotDoWell),
            (nameof(existingCheckIn.WhatIWillDoDifferently), existing => existing.WhatIWillDoDifferently, incoming => incoming.WhatIWillDoDifferently),
            (nameof(existingCheckIn.AnythingElse), existing => existing.AnythingElse, incoming => incoming.AnythingElse)
        ).ToList();
        return changes.Select(x => new WeeklyReflectionCheckInChangelogEntry(existingCheckIn, actingUserId, x.FieldName, x.Change, editingSessionGuid));
    }
}