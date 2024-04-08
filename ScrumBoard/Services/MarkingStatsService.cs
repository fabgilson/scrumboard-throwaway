using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Filters;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface IMarkingStatsService
{
    /// <summary>
    /// Calculates how much overhead a student logged in each week of a project/sprint. If no sprintId is provided, we
    /// return a list of WeeklyTimeSpans, which represents the amount of time logged in each active week of the project.
    /// If there is a sprintId, we return this list for only the weeks from the sprint start to the sprint end, or the
    /// last day the student logged time for that sprint, if it's after the sprint date.
    /// </summary>
    /// <param name="userId">The user to sum overhead logs for</param>
    /// <param name="projectId">The project we are calculating overhead for</param>
    /// <param name="sprintId">An optional Id for a sprint to calculate overhead for</param>
    /// <returns>A list of WeeklyTimeSpan objects</returns>
    Task<IEnumerable<WeeklyTimeSpan>> GetOverheadByWeek(long userId, long projectId, long? sprintId = null);
    
    /// <summary>
    /// Calculates the average duration of work logs in each week of a project/sprint.
    /// If there are no work logs in a week, the average duration is 0.
    /// </summary>
    /// <param name="userId">The user to sum overhead logs for</param>
    /// <param name="projectId">The project we are calculating overhead for</param>
    /// <param name="sprintId">An optional Id for a sprint to calculate overhead for</param>
    /// <returns></returns>
    Task<IEnumerable<WeeklyTimeSpan>> GetAvgWorkLogDurationByWeek(long userId, long projectId, long? sprintId);

    /// <summary>
    /// Same as GetOverheadByWeek, but for story hours.
    /// </summary>
    /// <param name="userId">The user to sum overhead logs for</param>
    /// <param name="projectId">The project we are calculating overhead for</param>
    /// <param name="sprintId">An optional Id for a sprint to calculate overhead for</param>
    /// <returns></returns>
    Task<IEnumerable<WeeklyTimeSpan>> GetStoryHoursByWeek(long userId, long projectId, long? sprintId = null);
    
    /// <summary>
    /// Same as GetOverheadByWeek, but for test hours. Includes work logs with #test and #testmanual tags.
    /// </summary>
    /// <param name="userId">The user to sum overhead logs for</param>
    /// <param name="projectId">The project we are calculating overhead for</param>
    /// <param name="sprintId">An optional Id for a sprint to calculate overhead for</param>
    /// <returns></returns>
    Task<IEnumerable<WeeklyTimeSpan>> GetTestHoursByWeek(long userId, long projectId, long? sprintId = null);
    
    /// <summary>
    /// Calculates the timespan of the shortest worklog that a student created in each week of a sprint. If no sprintId is provided, we
    /// return a list of WeeklyTimeSpans, which represents the amount of time logged in each active week of the project.
    /// If there is a sprintId, we return this list for only the weeks from the sprint start to the sprint end, or the
    /// last day the student logged time for that sprint, if it's after the sprint date.
    /// </summary>
    /// <param name="userId">The user to find the shortest worklogs for</param>
    /// <param name="projectId">The project that the worklogs should belong to</param>
    /// <param name="sprintId">Optional id of a sprint to find the shortest worklogs for</param>
    /// <returns></returns>
    Task<IEnumerable<WeeklyTimeSpan>> GetShortestWorklogDurationByWeek(long userId, long projectId, long? sprintId = null);

    /// <summary>
    /// Calculates the range of ISO weeks that may have overhead logged in them for either a whole project or a specific sprint.
    /// Each sprint's date range starts on its start date and ends on either the sprint end date, or the last date anyone logged overhead against the sprint.
    /// </summary>
    /// <param name="sprints">The sprints to calculate date ranges for</param>
    /// <param name="sprintId">The optional sprint to calculate date ranges for</param>
    /// <returns>A list of ISO week numbers representing all weeks in the project/sprint</returns>
    Task<IList<DateOnly>> CalculateDateRangesForSprintOrSprints(IEnumerable<Sprint> sprints, long? sprintId = null);

    /// <summary>
    /// Returns either the date the sprint ended, or the date of the last overhead log for the sprint, whichever is last.
    /// </summary>
    /// <param name="sprint">The sprint to compute for</param>
    /// <returns>The later of the two dates as a DateOnly</returns>
    Task<DateOnly> GetSprintEndOrLastLog(Sprint sprint);
}

public struct WeeklyTimeSpan
{
    public DateOnly WeekStart { get; init; }
    public long Ticks { get; init; }
    public long? SprintId { get; init; }
    public string SprintName { get; init; }
}

public class MarkingStatsService : IMarkingStatsService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IWorklogEntryService _worklogEntryService;
    private readonly IWorklogTagRepository _worklogTagRepository;

    public MarkingStatsService(IDbContextFactory<DatabaseContext> dbContextFactory, IWorklogEntryService worklogEntryService, IWorklogTagRepository worklogTagRepository)
    {
        _dbContextFactory = dbContextFactory;
        _worklogEntryService = worklogEntryService;
        _worklogTagRepository = worklogTagRepository;
    }

    public async Task<IEnumerable<WeeklyTimeSpan>> GetOverheadByWeek(long userId, long projectId, long? sprintId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sprints = context.Sprints
            .Where(x => x.SprintProjectId == projectId)
            .Where(x => sprintId == null || x.Id == sprintId);
          
        var overheadEntries = await sprints
            .SelectMany(x => x.OverheadEntries)
            .Where(entry => entry.UserId == userId)
            .Include(overheadEntry => overheadEntry.Sprint)
            .ToListAsync();
  
        // Get all Mondays from weeks in returned sprint range
        var weekStarts = await CalculateDateRangesForSprintOrSprints(sprints, sprintId);
  
        // For each week in the range, get all instances of time spent per sprint
        var overheadEntriesPerSprint = overheadEntries.GroupBy(entry => entry.Sprint).ToList();
        return weekStarts.SelectMany(weekStart =>
        {
            var timesSpentInWeek = overheadEntriesPerSprint.Select(overheadEntriesInSprintGroup =>
            {
                var timeSpent = overheadEntriesInSprintGroup
                    .Where(entry => DateOnly.FromDateTime(entry.Occurred.AddDays(DayOfWeek.Monday - entry.Occurred.DayOfWeek)) == weekStart)
                    .Sum(entry => entry.DurationTicks);

                return new WeeklyTimeSpan
                {
                    WeekStart = weekStart,
                    Ticks = timeSpent,
                    SprintId = overheadEntriesInSprintGroup.Key.Id,
                    SprintName = overheadEntriesInSprintGroup.Key.Name,
                };
            }).ToList();
            return timesSpentInWeek.Any() ? timesSpentInWeek : new List<WeeklyTimeSpan> { new() { WeekStart = weekStart } };
        });
    }

    public async Task<IEnumerable<WeeklyTimeSpan>> GetAvgWorkLogDurationByWeek(long userId, long projectId, long? sprintId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sprints = await GetSprints(projectId, sprintId, context).ToListAsync();
        
        var workLogs = await _worklogEntryService.GetWorklogEntriesForProjectAsync(projectId, sprintId, userId);
        
        // Get all Mondays from weeks in returned sprint range
        var weekStarts = await CalculateDateRangesForSprintOrSprints(sprints, sprintId);
  
        // For each week in the range, get all instances of time spent per sprint
        var workLogEntries = workLogs.ToList();
        var workLogsPerSprint= workLogEntries.GroupBy(x => x.Task.UserStory.StoryGroupId).ToList();
        return weekStarts.SelectMany(weekStart =>
        {
            var timesSpentInWeek = workLogsPerSprint.Select(workLogsInSprintGroup =>
            {
                var currentSprintId = workLogsInSprintGroup.Key;
                long averageLogDuration;
                var logsForWeek = workLogsInSprintGroup
                    .Where(entry => DateOnly.FromDateTime(entry.Occurred.AddDays(DayOfWeek.Monday - entry.Occurred.DayOfWeek)) == weekStart).ToList();

                if (sprintId != null && logsForWeek.Any())
                {
                    averageLogDuration = (long) logsForWeek
                        .Select(entry => entry.GetTotalTimeSpent().Ticks)
                        .Average();
                }
                else
                {
                    var totalTimeSpent = logsForWeek.Sum(entry => entry.GetTotalTimeSpent().Ticks);
                    // Divide either by the number of work logs this week from all sprints, or by 1 if there are none
                    var numberOfWorkLogsInWeekFromAllSprints = workLogEntries.Count(entry => DateOnly.FromDateTime(entry.Occurred.AddDays(DayOfWeek.Monday - entry.Occurred.DayOfWeek)) == weekStart);
                    var denominator = numberOfWorkLogsInWeekFromAllSprints > 0 ? numberOfWorkLogsInWeekFromAllSprints : 1;
                    averageLogDuration = totalTimeSpent / denominator;
                }
                
                return new WeeklyTimeSpan
                {
                    WeekStart = weekStart,
                    Ticks = averageLogDuration,
                    SprintId = currentSprintId,
                    SprintName = sprints.First(x => x.Id == currentSprintId).Name
                };
            }).ToList();
            return timesSpentInWeek.Any() ? timesSpentInWeek : new List<WeeklyTimeSpan> { new() { WeekStart = weekStart } };
        });
    }
    
    public async Task<IEnumerable<WeeklyTimeSpan>> GetStoryHoursByWeek(long userId, long projectId, long? sprintId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sprints = await context.Sprints
            .Where(x => x.SprintProjectId == projectId)
            .Where(x => sprintId == null || x.Id == sprintId)
            .ToListAsync();
        
        var workLogs = await _worklogEntryService.GetWorklogEntriesForProjectAsync(projectId, sprintId, userId);

        return await ExtractWeeklyTimeSpanFromWorklogsForSprint(
            sprintId, 
            sprints, 
            workLogs,
            w => new TimeSpan(w.Sum(entry => entry.GetTotalTimeSpent().Ticks))
        );
    }

    public async Task<IEnumerable<WeeklyTimeSpan>> GetTestHoursByWeek(long userId, long projectId, long? sprintId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sprints = await context.Sprints
            .Where(x => x.SprintProjectId == projectId)
            .Where(x => sprintId == null || x.Id == sprintId)
            .ToListAsync();
        
        // Filter work logs by tags, users, and sprints ranges
        var testTags = new List<WorklogTag>();
        testTags.AddRange( new List<WorklogTag>{ await _worklogTagRepository.GetByNameAsync("Test"), await _worklogTagRepository.GetByNameAsync("Testmanual") });
        var user = await context.Users.Where(x => x.Id == userId).ToListAsync();
        var startAndEndDate = await CalculateDateRangesForSprintOrSprints(sprints);
        var workLogEntryFilter = new WorklogEntryFilter
        {
            WorklogTagsFilter = testTags, 
            AssigneeFilter = user, 
            DateRangeStart = startAndEndDate.Min(), 
            DateRangeEnd = startAndEndDate.Max(), 
            DateRangeFilterEnabled = true
        };

        var workLogs = await _worklogEntryService.GetByProjectFilteredAsync(projectId, workLogEntryFilter, sprintId);

        return await ExtractWeeklyTimeSpanFromWorklogsForSprint(
            sprintId, 
            sprints, 
            workLogs, 
            w => new TimeSpan(w.Sum(entry => entry.GetTotalTimeSpent().Ticks))
        );
    }
    
    public async Task<IEnumerable<WeeklyTimeSpan>> GetShortestWorklogDurationByWeek(long userId, long projectId, long? sprintId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var worklogEntries = await context.WorklogEntries
            .Where(x => x.UserId == userId)
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => sprintId == null || x.Task.UserStory.StoryGroupId == sprintId)
            .Include(x => x.TaggedWorkInstances)
            .Include(x => x.Task).ThenInclude(x => x.UserStory)
            .ToListAsync();
        
        var sprints = await context.Sprints
            .Where(x => x.SprintProjectId == projectId)
            .Where(x => sprintId == null || x.Id == sprintId)
            .ToListAsync();

        return await ExtractWeeklyTimeSpanFromWorklogsForSprint(
            sprintId, 
            sprints, 
            worklogEntries,
            worklogs => worklogs.Any() ? worklogs.Min(x => x.GetTotalTimeSpent()) : new TimeSpan(0)
        );
    }
    
    private async Task<IEnumerable<WeeklyTimeSpan>> ExtractWeeklyTimeSpanFromWorklogsForSprint(
        long? sprintId, 
        IReadOnlyCollection<Sprint> sprints, 
        IEnumerable<WorklogEntry> workLogs,
        Func<ICollection<WorklogEntry>, TimeSpan> extractTimeSpanFunc
    ) {
        // Get all Mondays within returned sprint range
        var weekStarts = await CalculateDateRangesForSprintOrSprints(sprints, sprintId);

        // For each week in the range, get all instances of time spent per sprint
        var workLogsPerSprint = workLogs.GroupBy(x => x.Task.UserStory.StoryGroupId).ToList();

        return weekStarts.SelectMany(weekStart =>
        {
            var timesSpentInWeek = workLogsPerSprint.Select(workLogsInSprintGroup =>
            {
                var worklogs = workLogsInSprintGroup
                    .Where(entry => DateOnly.FromDateTime(entry.Occurred.AddDays(DayOfWeek.Monday - entry.Occurred.DayOfWeek)) == weekStart)
                    .ToList();
                var timeSpent = extractTimeSpanFunc(worklogs);

                var currentSprintId = workLogsInSprintGroup.Key;
                return new WeeklyTimeSpan
                {
                    WeekStart = weekStart,
                    Ticks = timeSpent.Ticks,
                    SprintId = currentSprintId,
                    SprintName = sprints.First(x => x.Id == currentSprintId).Name
                };
            }).ToList();
            return timesSpentInWeek.Any() ? timesSpentInWeek : new List<WeeklyTimeSpan> { new() { WeekStart = weekStart } };
        });
    }

    public async Task<IList<DateOnly>> CalculateDateRangesForSprintOrSprints(IEnumerable<Sprint> sprints, long? sprintId = null)
    {
        sprints = sprints.ToList();

        var start = sprints.Min(x => x.StartDate);
        var end = await GetSprintEndOrLastLog(sprints.MaxBy(x => x.EndDate));
        
        if (sprintId == null) return IsoWeekCalculator.GetWeekStartsBetweenDates(start, end);
        
        var sprint = sprints.First(x => x.Id == sprintId);
        start = sprint.StartDate;
        end =  await GetSprintEndOrLastLog(sprint);
        
        return IsoWeekCalculator.GetWeekStartsBetweenDates(start, end);
    }

    public async Task<DateOnly> GetSprintEndOrLastLog(Sprint sprint)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var overheadOccurrences = context.OverheadEntries
            .Where(x => x.SprintId == sprint.Id);

        if (!overheadOccurrences.Any()) return sprint.EndDate;

        var latestOverheadOccurence = DateOnly.FromDateTime(overheadOccurrences.Select(x => x.Occurred).Max());
        return latestOverheadOccurence > sprint.EndDate ? latestOverheadOccurence : sprint.EndDate;
    }

    private static IQueryable<Sprint> GetSprints(long projectId, long? sprintId, DatabaseContext context)
    {
        var sprints = context.Sprints
            .Where(x => x.SprintProjectId == projectId)
            .Where(x => sprintId == null || x.Id == sprintId);
        return sprints;
    }
}