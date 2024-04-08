using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Repositories;

namespace ScrumBoard.Services;

public interface IUserStatsService
{
    Task<TimeWorked> TimeWorked(User user, Project project, Sprint sprint); 
    Task<StoriesWorked> StoriesWorked(User user, Project project, Sprint sprint);
    Task<TasksWorked> TasksWorked(User user, Project project, Sprint sprint);
    Task<TasksReviewed> TasksReviewed(User user, Project project, Sprint sprint);
    Task<StoriesReviewed> StoriesReviewed(User user, Project project, Sprint sprint);
    Task<WorkEfficiency> WorkEfficiency(User user, Project project, Sprint sprint);
    Task<StatsBar> TagsWorked(User user, Project project, Sprint sprint);
    
    /// <summary>
    /// For some task, get all instances of time (tagged) a user logged against that task where the worklog occurred
    /// between some start and end time.
    /// </summary>
    /// <param name="userId">ID Of user for whom to fetch time spent</param>
    /// <param name="taskId">ID of the task against which to find time logged</param>
    /// <param name="start">Optional, start datetime before which to ignore results. If null, will consider worklogs since the beginning of time.</param>
    /// <param name="end">Optional, end datetime after which to ignore results. If null, will consider worklogs until the end of time.</param>
    Task<TimeSpentOnTask> TagsWorkOnTaskByUser(long taskId, long userId, DateTime? start=null, DateTime? end=null);

    Task<IEnumerable<TimePairedForUser>> PairRankings(long userId, long projectId, long? sprintId = null);

    /// <summary>
    /// For some user, gets a count of how many tasks they are assigned to have been reviewed by each of their teammates.
    /// </summary>
    /// <param name="userId">User for whom to get how many of their tasks have been reviewed by each team mate</param>
    /// <param name="projectId">Project in which to check for reviewed tasks</param>
    /// <param name="sprintId">Optional, sprint which results should be restricted to</param>
    /// <returns>Counts of how many OF THE USER'S tasks have been reviewed BY each team member</returns>
    Task<IEnumerable<TasksReviewedForUser>> ReviewCountOfUserFromTeamMates(long userId, long projectId, long? sprintId = null);
    
    /// <summary>
    /// For some user, gets a count of how many of their teammates tasks they have reviewed.
    /// </summary>
    /// <param name="userId">User for whom to get how many of their teammates tasks they have reviewed</param>
    /// <param name="projectId">Project in which to check for reviewed tasks</param>
    /// <param name="sprintId">Optional, sprint which results should be restricted to</param>
    /// <returns>A count FOR EACH TEAM MEMBER of how many of their tasks have been reviewed BY THE USER</returns>
    Task<IEnumerable<TasksReviewedForUser>> ReviewCountOfUserForTeamMates(long userId, long projectId, long? sprintId=null);
    
    Task<IEnumerable<IStatistic>> GetStatCardData(User user, Project project, Sprint sprint=null);
}

public struct TimeSpentOnTask
{
    public TimeSpan TotalTime { get; set; }
    
    public ICollection<TimeSpentOnWorklogTag> TimeSpentOnWorklogTags { get; set; }
}

public struct TimeSpentOnWorklogTag
{
    public WorklogTag Tag { get; set; }
    public TimeSpan TimeSpent { get; set; }
}

public class UserStatsService : IUserStatsService
{
    private readonly IUserStoryTaskRepository _userStoryTaskRepository;
    private readonly IOverheadEntryRepository _overheadEntryRepository;
    
    private readonly IProjectStatsService _projectStatsService;
    private readonly IWorklogEntryService _worklogEntryService;

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    
    public UserStatsService(
        IUserStoryTaskRepository userStoryTaskRepository, 
        IOverheadEntryRepository overheadEntryRepository, 
        IProjectStatsService projectStatsService,
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IWorklogEntryService worklogEntryService
    ) {
        _userStoryTaskRepository = userStoryTaskRepository;
        _overheadEntryRepository = overheadEntryRepository;
        _projectStatsService = projectStatsService;
        _dbContextFactory = dbContextFactory;
        _worklogEntryService = worklogEntryService;
    }

    public async Task<IEnumerable<IStatistic>> GetStatCardData(User user, Project project, Sprint sprint=null)
    {
        return new List<IStatistic>()
        {
            await TimeWorked(user, project, sprint),
            await StoriesWorked(user, project, sprint),
            await TasksWorked(user, project, sprint),
            await StoriesReviewed(user, project, sprint),
            await TasksReviewed(user, project, sprint),
            await WorkEfficiency(user, project, sprint),
        };
    }

    /// <summary>
    /// Gets both the sum of the time logged by the given user and the total time logged by all users in the scope of
    /// the given project or sprint if the sprint is present.
    /// </summary>
    /// <param name="user">The user to retrieve the sum of time logged for</param>
    /// <param name="project">The project to retrieve the worklogs from to calculate time logged for users</param>
    /// <param name="sprint">The optional sprint to retrieve the worklogs from to calculate time logged for users. Project will be ignored if sprint is not null.</param>
    /// <returns>
    /// Instance of TimeWorked, which contains the time worked by the given user and all users in the project
    /// or sprint if the sprint is present.
    /// </returns>
    public async Task<TimeWorked> TimeWorked(User user, Project project, Sprint sprint=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var allTaggedWorkInstances = context.TaggedWorkInstances
            .Where(x => x.WorklogEntry.Task.UserStory.ProjectId == project.Id)
            .Where(x => sprint == null || x.WorklogEntry.Task.UserStory.StoryGroupId == sprint.Id);

        var taggedWorkInstancesForUser = allTaggedWorkInstances
            .Where(x => x.WorklogEntry.UserId == user.Id);

        var timeWorkedByAllMembers = allTaggedWorkInstances.Sum(x => x.Duration);
        var timeWorkedByUser = taggedWorkInstancesForUser.Sum(x => x.Duration);
        
        return new TimeWorked(timeWorkedByUser.TotalHours, timeWorkedByAllMembers.TotalHours, sprint != null);
    }

    /// <summary>
    /// Gets both the total number of stories worked by the given user and the total number of stories worked by all 
    /// users in the given project or sprint if the sprint is present.
    ///
    /// Work-logs that have 'Review' or 'Document' as their only tags will be ignored.
    /// 
    /// </summary>
    /// <param name="user">The user to retrieve the total number of stories worked on for</param>
    /// <param name="project">The project to retrieve the total number of stories worked on from</param>
    /// <param name="sprint">The optional sprint to retrieve the total number of stories worked on from. Project will be ignored if sprint is not null.</param>
    /// <returns>
    /// Instance of StoriesWorked which contains the number of stories worked on by the given user and all
    /// users in the given project or sprint if the sprint is present.
    /// </returns>
    public async Task<StoriesWorked> StoriesWorked(User user, Project project, Sprint sprint=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var totalStories = context.UserStories
            .Where(x => x.ProjectId == project.Id)
            .Where(x => sprint == null || x.StoryGroupId == sprint.Id)
            .Where(x => x.StoryGroup is Sprint);
            
        var storiesWorkedByUser = totalStories
            .Where(story => story.Tasks.Any(task => task.Worklog.Any(entry => entry.UserId == user.Id
                && entry.TaggedWorkInstances.Any(x => x.WorklogTag.Name != "Review" && x.WorklogTag.Name != "Document")))
            );

        return new StoriesWorked(await storiesWorkedByUser.CountAsync(), await totalStories.CountAsync(), sprint != null);
    }

    /// <summary>
    /// Gets both the total number of tasks worked by the given user and the total number of tasks worked by all 
    /// users in the given project
    /// </summary>
    /// <param name="user">The user to retrieve the total number of tasks worked on for</param>
    /// <param name="project">The project to retrieve the total number of tasks worked on from</param>
    /// <param name="sprint">The optional sprint to retrieve the total number of tasks worked on from. Project will be ignored if sprint is not null.</param>
    /// <returns>
    /// Instance of TasksWorked which contains the number of tasks worked on by the given user and all
    /// users in the given project or sprint if the sprint is present.
    /// </returns>
    public async Task<TasksWorked> TasksWorked(User user, Project project, Sprint sprint=null)
    {
        double tasksWorked;
        double population; 
        bool isSprint = sprint != null;
        if (!isSprint) {
            tasksWorked = await _userStoryTaskRepository.GetCountByProject(project, StatisticFilters.TasksWorkedByUser(user));
            population = await _userStoryTaskRepository.GetCountByProject(project, StatisticFilters.TasksCommitted(project));
        } else {
            tasksWorked = await _userStoryTaskRepository.GetCountByStoryGroup(sprint, StatisticFilters.TasksWorkedByUser(user));
            population = await _userStoryTaskRepository.GetCountByStoryGroup(sprint);
        }

        return new TasksWorked(tasksWorked, population, isSprint);
    }
    
    /// <summary>
    /// Gets both the total number of tasks reviewed by the given user and the total number of tasks reviewed by all 
    /// users in the given project or sprint if the sprint is present.
    /// A reviewed task is a task with the user assigned as a reviewer on, and must have a stage of
    /// either Done, or Deferred.
    /// </summary>
    /// <param name="user">The user to retrieve the total number of tasks reviewed for</param>
    /// <param name="project">The project to retrieve the total number of tasks reviewed from</param>
    /// <param name="sprint">The optional sprint to retrieve the total number of tasks reviewed from. Project will be ignored if sprint is not null.</param>
    /// <returns>
    /// Instance of TasksReviewed which contains the number of tasks reviewed by the given user and all
    /// users in the given project or sprint if the sprint is present.
    /// </returns>
    public async Task<TasksReviewed> TasksReviewed(User user, Project project, Sprint sprint=null)
    {
        var tasksReviewedByUser = await _projectStatsService.GetTasksReviewedInProject(project.Id, user.Id, sprint?.Id);
        var tasksReviewedTotal = await _projectStatsService.GetTasksReviewedInProject(project.Id, null, sprint?.Id);

        return new TasksReviewed(tasksReviewedByUser.Count, tasksReviewedTotal.Count, sprint is not null);
    }

    /// <summary>
    /// Gets both the total number of stories that contain a reviewed task by the given user and the total number of
    /// stories that contain at least one reviewed task in the given project or sprint if the sprint is present.
    /// A reviewed task is a task with the user assigned as a reviewer on, and must be a stage of Done or Deferred.
    /// </summary>
    /// <param name="user">The user to retrieve the total number of stories that contain reviewed task for</param>
    /// <param name="project">The project to retrieve the total number of stories in</param>
    /// <param name="sprint">The optional sprint to retrieve the total number of stories in. Project will be ignored if sprint is not null.</param>
    /// The project to retrieve the total number of stories that contain a reviewed task from
    /// </param>
    /// <returns>
    /// Instance of StoriesReviewed which contains the number of stories that contain tasks reviewed by the given user
    /// in the given projector sprint if the sprint is present.
    /// </returns>
    public async Task<StoriesReviewed> StoriesReviewed(User user, Project project, Sprint sprint=null)
    {
        var storiesReviewedByUser = await _projectStatsService.GetUserStoriesReviewedInProject(project.Id, user.Id, sprint?.Id);
        var storiesReviewedTotal = await _projectStatsService.GetUserStoriesReviewedInProject(project.Id, null, sprint?.Id);
        
        return new StoriesReviewed(storiesReviewedByUser.Count, storiesReviewedTotal.Count, sprint is not null);
    }
    
    public async Task<WorkEfficiency> WorkEfficiency(User user, Project project, Sprint sprint = null)
    {
        var overheadTime = await _overheadEntryRepository.GetTotalTimeLogged(
            OverheadEntryTransforms.FilterByUser(user),
            sprint == null
                ? OverheadEntryTransforms.FilterByProject(project)
                : OverheadEntryTransforms.FilterBySprint(sprint)
            );

        var worklogs = await _worklogEntryService.GetWorklogEntriesForProjectAsync(
            project.Id, 
            sprintId: sprint?.Id,
            userId: user.Id
        );

        var workingTime = worklogs.Sum(x => x.GetTotalTimeSpent());

        return new WorkEfficiency(workingTime.TotalHours, (workingTime + overheadTime).TotalHours, sprint != null);
    }

    /// <summary>
    /// Gets the proportion of worklogs of each type of worklog tag, for the given user in the given project, out of
    /// the total number of worklogs in the given project or sprint if the sprint is present.
    /// </summary>
    /// <param name="user">The user to get the proportion of worklog tags worked on for.</param>
    /// <param name="project">The project that the proportion of worklog tags worked on for is retrieved from.</param>
    /// <param name="sprint">The optional sprint that the proportion of worklog tags worked on for is retrieved from. Project will be ignored if sprint is not null.</param>
    /// <returns>
    /// An instance of ProgressBarChart which contains datasets for the number of worklogs worked by the given
    /// user for each worklog tag in the given project or sprint if the sprint is present.
    /// </returns>
    public async Task<StatsBar> TagsWorked(User user, Project project, Sprint sprint)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var workInstancesPerWorklogTag = await context.TaggedWorkInstances
            .Where(x => x.WorklogEntry.UserId == user.Id)
            .Where(x => x.WorklogEntry.Task.UserStory.ProjectId == project.Id)
            .Where(x => sprint == null || x.WorklogEntry.Task.UserStory.StoryGroupId == sprint.Id)
            .Include(taggedWorkInstance => taggedWorkInstance.WorklogTag)
            .ToListAsync();

        var datasets = workInstancesPerWorklogTag
            .GroupBy(x => x.WorklogTag)
            .Select(group => 
                new ProgressBarChartSegment<double>(group.Key.Id, group.Key.Name, group.Sum(x => x.Duration).TotalHours)
            ).ToList();

        var totalHours = datasets.Sum(x => x.Data);
        return new StatsBar(datasets.OrderBy(d => d.Data).ToList(), totalHours);
    }
    
    public async Task<TimeSpentOnTask> TagsWorkOnTaskByUser(long taskId, long userId, DateTime? start=null, DateTime? end=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var taggedWorkByUser = await context.TaggedWorkInstances
            .Where(x => x.WorklogEntry.TaskId == taskId)
            .Where(x => x.WorklogEntry.UserId == userId)
            .Where(x => start == null || x.WorklogEntry.Occurred > start)
            .Where(x => end == null || x.WorklogEntry.Occurred < end)
            .Include(taggedWorkInstance => taggedWorkInstance.WorklogTag)
            .ToListAsync();

        var workPerTag = taggedWorkByUser
            .GroupBy(x => x.WorklogTag)
            .Select(x => new TimeSpentOnWorklogTag
            {
                Tag = x.Key,
                TimeSpent = x.Sum(twi => twi.Duration)
            }).ToList();

        return new TimeSpentOnTask
        {
            TimeSpentOnWorklogTags = workPerTag,
            TotalTime = workPerTag.Sum(x => x.TimeSpent)
        };
    }
    
    /// <summary>
    /// For some user, gets the amount of time spent working with each team member based on their own logs.
    /// </summary>
    /// <param name="userId">The user to get the ranking of work logged with each user as a pair for.</param>
    /// <param name="projectId">The project to get the ranking of work logged with pairs for the given user.</param>
    /// <param name="sprintId">The optional sprint to get the ranking of work logged with pairs for the given user.</param>
    /// <returns>
    /// An list of TimePairedForUser objects with each paired user and the proportion of hours 
    /// logged with the given user in the given project or sprint if the sprint is present.
    /// </returns>
    public async Task<IEnumerable<TimePairedForUser>> PairRankings(long userId, long projectId, long? sprintId=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var project = await context.Projects
            .Where(x => x.Id == projectId)
            .Include(x => x.MemberAssociations).ThenInclude(x => x.User)
            .FirstAsync();
        var teamMembers = project.GetWorkingMembers().Where(x => x.Id != userId).ToArray();

        var pairedLogs = await context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == project.Id)
            .Where(x => x.PairUserId != null && teamMembers.Select(tm => tm.Id).Contains(x.PairUserId.Value))
            .Where(x => x.UserId == userId)
            .Where(x => sprintId == null || sprintId == x.Task.UserStory.StoryGroupId)
            .Include(worklogEntry => worklogEntry.TaggedWorkInstances)
            .Include(worklogEntry => worklogEntry.User).Include(worklogEntry => worklogEntry.PairUser)
            .ToListAsync();

        var totalTimeUserHasPaired = pairedLogs.SelectMany(x => x.TaggedWorkInstances).Sum(x => x.Duration);

        var timesPairedPerUser = pairedLogs
            .GroupBy(x => x.PairUser)
            .Select(x => new TimePairedForUser(
                x.Key,
                x.SelectMany(worklog => worklog.TaggedWorkInstances).Sum(workInstance => workInstance.Duration).TotalHours,
                totalTimeUserHasPaired.TotalHours)
            );

        return timesPairedPerUser.OrderByDescending(x => x.Value);
    }
    
    public async Task<IEnumerable<TasksReviewedForUser>> ReviewCountOfUserFromTeamMates(long userId, long projectId, long? sprintId=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var project = await context.Projects
            .Where(x => x.Id == projectId)
            .Include(x => x.MemberAssociations).ThenInclude(x => x.User)
            .FirstAsync();
        var teamMembers = project.GetWorkingMembers().Where(x => x.Id != userId).ToArray();

        var totalReviewedWorklogs = context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => sprintId == null || x.Task.UserStory.StoryGroupId == sprintId)
            .Where(x => teamMembers.Select(tm => tm.Id).Contains(x.UserId))
            .Where(x => x.Task.UserAssociations.Any(ta => ta.UserId == userId && ta.Role == TaskRole.Assigned))
            .Where(x => x.TaggedWorkInstances.Any(twi => twi.WorklogTag.Name == "Review"));
        
        var totalTasksReviewedForUser = await totalReviewedWorklogs.Select(x => x.Task).Distinct().CountAsync();

        var tasksReviewedPerUser = new List<TasksReviewedForUser>();
        foreach (var teamMember in teamMembers)
        {
            var count = await totalReviewedWorklogs
                .Where(x => x.UserId == teamMember.Id)
                .Select(x => x.Task).Distinct()
                .CountAsync();
            tasksReviewedPerUser.Add(new TasksReviewedForUser(teamMember, count, totalTasksReviewedForUser));
        }

        return tasksReviewedPerUser.OrderByDescending(x => x.Value);
    }
    

    public async Task<IEnumerable<TasksReviewedForUser>> ReviewCountOfUserForTeamMates(long userId, long projectId, long? sprintId=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var project = await context.Projects
            .Where(x => x.Id == projectId)
            .Include(x => x.MemberAssociations).ThenInclude(x => x.User)
            .FirstAsync();
        var teamMembers = project.GetWorkingMembers().Where(x => x.Id != userId).ToArray();

        var totalReviewedTasks = context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == project.Id)
            .Where(x => sprintId == null || x.Task.UserStory.StoryGroupId == sprintId)
            .Where(x => x.UserId == userId)
            .Where(x => x.TaggedWorkInstances.Any(twi => twi.WorklogTag.Name == "Review"));

        var totalTasksReviewedByUser = await totalReviewedTasks.Select(x => x.Task).Distinct().CountAsync();

        var tasksReviewedPerUser = new List<TasksReviewedForUser>();
        foreach (var teamMember in teamMembers)
        {
            var count = await totalReviewedTasks
                .Where(x => x.Task.UserAssociations.Any(ta => ta.UserId == teamMember.Id && ta.Role == TaskRole.Assigned))            
                .Select(x => x.Task).Distinct()
                .CountAsync();
            tasksReviewedPerUser.Add(new TasksReviewedForUser(teamMember, count, totalTasksReviewedByUser));
        }

        return tasksReviewedPerUser.OrderByDescending(x => x.Value);
    }
}