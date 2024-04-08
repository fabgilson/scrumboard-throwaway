using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using ScrumBoard.Repositories;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Messages;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Services
{
    public interface IBurndownService 
    {
        Task<IEnumerable<BurndownPoint<TimeSpan>>> GetTaskTimeDeltas(UserStoryTask task);
        Task<List<BurndownPoint<double>>> GetData(Sprint sprint, bool burnup);
        Task<Dictionary<Stage, List<BurndownPoint<double>>>> GetFlowData(Sprint sprint);
        Task<Dictionary<Stage, List<BurndownPoint<double>>>> GetFlowData(Project project);
        Task<IMessage> GenerateMessage(BurndownPoint<double> point);
    }
    public class BurndownService : IBurndownService
    {
        private readonly ILogger<BurndownService> _logger;
        private readonly IUserStoryTaskRepository _taskRepository;
        private readonly IUserStoryTaskChangelogRepository _taskChangelogRepository;
        private readonly IWorklogEntryService _worklogEntryService;

        public BurndownService(
            ILogger<BurndownService> logger,
            IUserStoryTaskRepository taskService,
            IUserStoryTaskChangelogRepository taskChangelogRepository,
            IWorklogEntryService worklogEntryService
        ) {
            _logger = logger;
            _taskRepository = taskService;
            _taskChangelogRepository = taskChangelogRepository;
            _worklogEntryService = worklogEntryService;
        }

        /// <summary> 
        /// Generate list of time deltas on the remaining time. 
        /// From the events: task creation, task estimate changes and time logged 
        /// The result is clamped, so that the running total is never below 0
        /// Intervals when the task is deferred are also taken into account
        /// </summary> 
        /// <param name="task"> Task to find the deltas for </param>
        /// <returns> List of remaining time deltas </returns>
        public async Task<IEnumerable<BurndownPoint<TimeSpan>>> GetTaskTimeDeltas(UserStoryTask task)
        {
            var result = await GetScopeChangesAndWorkForTask(task);
            result = AccumulateTime(result);
            result = await ZeroFinishedIntervals(task, result);
            result = TimeDifferences(result);
            return result;
        }

        private async Task<IEnumerable<BurndownPoint<TimeSpan>>> GetScopeChangesAndWorkForTask(UserStoryTask task)
        {
            var result = new List<BurndownPoint<TimeSpan>>();

            result.Add(BurndownPoint<TimeSpan>.NewTask(task.Created, task.OriginalEstimate, task));
            result.AddRange((await _taskChangelogRepository.GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Estimate)))
                .Select(change => BurndownPoint<TimeSpan>.ScopeChange(change.Created, (TimeSpan)change.ToValueObject - (TimeSpan)change.FromValueObject, change)));
            result.AddRange((await _worklogEntryService.GetWorklogEntriesForTaskAsync(task.Id))
                .Select(entry => BurndownPoint<TimeSpan>.Worklog(entry.Occurred, -entry.GetTotalTimeSpent(), entry)));

            result.Sort((x, y) => x.Moment.CompareTo(y.Moment));
            return result;
        }

        /// <summary>
        /// Accumulates a burndown point Values into a new enumerable of points where point.Value is the running total of all Values previous and the current
        /// The running total is clamped, so that it is always non-negative
        /// </summary>
        /// <param name="deltas">Enumerable of changes</param>
        /// <returns>Enumerable of running totals</returns>
        private IEnumerable<BurndownPoint<TimeSpan>> AccumulateTime(IEnumerable<BurndownPoint<TimeSpan>> deltas)
        {
            // List of running totals to this task's time clamped, so that the running total is never below 0
            var accumulatedTime = TimeSpan.Zero;
            foreach (var entry in deltas) {
                accumulatedTime += entry.Value;
                if (accumulatedTime < TimeSpan.Zero) accumulatedTime = TimeSpan.Zero;

                yield return entry.WithValue(accumulatedTime);
            }
        }
        
        /// <summary>
        /// Accumulates changes before a datetime into a single change, then changes made afterwards are accumulated as normal
        /// </summary>
        /// <param name="deltas">List of deltas to accumulate</param>
        /// <param name="cutoff">Moment to stop accumulating</param>
        /// <returns>Enumerable of running total points</returns>
        private IEnumerable<BurndownPoint<TimeSpan>> AccumulateAndMergeChanges(
            IEnumerable<BurndownPoint<TimeSpan>> deltas,
            DateTime cutoff)
        {
            var isBeforeStart = true;
            var accumulatedTime = TimeSpan.Zero;
            foreach (var change in deltas) {
                if (isBeforeStart)
                {
                    if (change.Moment > cutoff)
                    {
                        yield return BurndownPoint<TimeSpan>.Initial(cutoff, accumulatedTime);
                        isBeforeStart = false;
                    }
                }
                accumulatedTime += change.Value;

                if (!isBeforeStart)
                {
                    yield return change.WithValue(accumulatedTime);
                }
            }

            if (isBeforeStart)
            {
                yield return BurndownPoint<TimeSpan>.Initial(cutoff, accumulatedTime);
            }
        }
        
        /// <summary>
        /// Computes differences between subsequent burndown points
        /// </summary>
        /// <param name="accumulated">Enumerable of running total burndown points</param>
        /// <returns>Enumerable of change burndown points</returns>
        private IEnumerable<BurndownPoint<TimeSpan>> TimeDifferences(IEnumerable<BurndownPoint<TimeSpan>> accumulated)
        {
            var previous = TimeSpan.Zero;
            foreach (var entry in accumulated) {
                yield return entry.WithValue(entry.Value - previous);
                previous = entry.Value;
            }
        }

        /// <summary>
        /// Finds all the done or deferred intervals for a task then zeros out all the points within said intervals
        /// </summary>
        /// <param name="task">Task to apply finished intervals on</param>
        /// <param name="prevResult">Result to merge with finished intervals</param>
        /// <returns>Result with finished intervals zeroed</returns>
        private async Task<List<BurndownPoint<TimeSpan>>> ZeroFinishedIntervals(UserStoryTask task, IEnumerable<BurndownPoint<TimeSpan>> prevResult)
        {
            var zeroedStages = new List<Stage> {Stage.Done, Stage.Deferred};
            
            var deferralChanges = (await _taskChangelogRepository.GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Stage)))
                .Where(change => zeroedStages.Contains((Stage)change.FromValueObject) || zeroedStages.Contains((Stage)change.ToValueObject))
                .ToList();

            var mergedPoints = deferralChanges
                .Select(change => (BurndownPoint<TimeSpan>.StageChange(change.Created, TimeSpan.Zero, change),
                    zeroedStages.Contains((Stage) change.ToValueObject)))
                .Concat(prevResult.Select(point => (point, false)))
                .OrderBy(pair => pair.Item1.Moment);

            var withinDeferredInterval = false;
            var previousValue = TimeSpan.Zero;
            var result = new List<BurndownPoint<TimeSpan>>();
            foreach (var (point, isDeferred) in mergedPoints)
            {
                var newPoint = point;
                if (point.Type == BurndownPointType.StageChange)
                {
                    withinDeferredInterval = isDeferred;
                    newPoint = point.WithValue(previousValue);
                }

                previousValue = newPoint.Value;
                if (withinDeferredInterval) newPoint = newPoint.WithValue(TimeSpan.Zero);
                result.Add(newPoint);
            }

            return result;
        }

        /// <summary>
        /// Generates the burndown (or burnup) data for the provided sprint.
        /// </summary>
        /// <param name="sprint"> Sprint to compute burndown for, must have already started </param>
        /// <param name="burnup"> Bool determining whether to return burndown or burnup data. True for burnup. </param>
        /// <returns> Sprint burndown or burnup data as a list ordered by occurance of number of hours remaining </returns>
        public async Task<List<BurndownPoint<double>>> GetData(Sprint sprint, bool burnup) {
            var sprintStart = sprint.TimeStarted.Value; // Sprint must have started

            // List of changes to the total work done over the sprint
            var changes = new List<BurndownPoint<TimeSpan>>();
            foreach (var task in await _taskRepository.GetByStoryGroup(sprint)) {
                if (burnup) {
                    changes.AddRange(await GetBurnupWorkForTask(task));
                } else {    
                    changes.AddRange(await GetTaskTimeDeltas(task));                          
                }                
            }

            changes.Sort((x, y) => x.Moment.CompareTo(y.Moment));

            // Current value of the burndown
            return AccumulateAndMergeChanges(changes, sprintStart)
                .Select(point => point.WithValue(point.Value.TotalHours))
                .ToList();
        }

        /// <summary>
        /// Generates the burnup points for the given task
        /// </summary>
        /// <param name="task"> Task to get burnup points for </param>
        /// <returns> An enumerbale of burndown points for the burnup line </returns>
        private async Task<IEnumerable<BurndownPoint<TimeSpan>>> GetBurnupWorkForTask(UserStoryTask task)
        {
            var result = new List<BurndownPoint<TimeSpan>>();
            
            result.AddRange((await _worklogEntryService.GetWorklogEntriesForTaskAsync(task.Id))
                .Select(entry => BurndownPoint<TimeSpan>.Worklog(entry.Occurred, entry.GetTotalTimeSpent(), entry)));

            result.Sort((x, y) => x.Moment.CompareTo(y.Moment));
            return result;
        }

        /// <summary>
        /// Generates a tooltip message from a burndown point
        /// </summary>
        public async Task<IMessage> GenerateMessage(BurndownPoint<double> point)
        {
            switch (point.Type) {
                case BurndownPointType.NewTask:
                    var task = await _taskRepository.GetByIdAsync(point.Id);
                    return new BurndownPointMessage(point.Moment, task);
                case BurndownPointType.ScopeChange:
                case BurndownPointType.StageChange:
                    var change = await _taskChangelogRepository.GetByIdAsync(point.Id,
                        UserStoryTaskChangelogIncludes.TaskChanged
                    );
                    return new BurndownPointMessage(point.Moment, change);
                case BurndownPointType.Worklog:
                    var worklog = await _worklogEntryService.GetWorklogEntryByIdAsync(point.Id, true);
                    return new BurndownPointMessage(point.Moment, worklog);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Computes the cumulative flow diagram data for the provided sprint
        /// </summary>
        /// <param name="sprint">Sprint to compute for</param>
        /// <returns>Cumulative flow line for every stage</returns>
        public async Task<Dictionary<Stage, List<BurndownPoint<double>>>> GetFlowData(Sprint sprint)
        {
            return await GetFlowData(await _taskRepository.GetByStoryGroup(sprint), sprint.TimeStarted.Value);
        }
        
        /// <summary>
        /// Computes the cumulative flow diagram data for the provided project
        /// </summary>
        /// <param name="project">Project to compute for</param>
        /// <returns>Cumulative flow line for every stage</returns>
        public async Task<Dictionary<Stage, List<BurndownPoint<double>>>> GetFlowData(Project project)
        {
            return await GetFlowData(await _taskRepository.GetByProject(project, query => query.Where(task => task.UserStory.StoryGroup is Sprint)), project.StartDate.ToDateTime(TimeOnly.MinValue));
        }

        /// <summary>
        /// Generates data for the cumulative flow diagram
        /// </summary>
        /// <param name="tasks">List of tasks to include in flow diagram chart data</param>
        /// <param name="chartStart">Moment to start the chart at, will merge changes before this point</param>
        /// <returns>Dictionary mapping from every Stage to a line of points where the value is the total at that point in time within the stage</returns>
        private async Task<Dictionary<Stage, List<BurndownPoint<double>>>> GetFlowData(IEnumerable<UserStoryTask> tasks, DateTime chartStart)
        {
            var invalidStage = (Stage) (-1);
            
            // List of changes in duration across all stages
            // The change for a specific stage is stored in that enums int value e.g. Value for Stage.Done is point.Value[3]
            var changesForAllStages = new List<BurndownPoint<TimeSpan[]>>();
            foreach (var task in tasks)
            {
                var taskStagesChanges = await GetTaskEstimateAndStageData(task);
                
                var previousEstimate = TimeSpan.Zero;
                var previousStage = invalidStage;
                foreach (var (point, stage) in taskStagesChanges)
                {
                    var newValue = Enumerable.Repeat(TimeSpan.Zero, Enum.GetValues<Stage>().Length).ToArray();
                    
                    if (previousStage != stage)
                    {
                        // If we don't have a previous stage, then don't subtract off the previous estimate
                        if (previousStage != invalidStage)
                        {
                            newValue[(int) previousStage] = -previousEstimate;
                        }
                        
                        // Add the new estimate to the new stage
                        newValue[(int)stage] = point.Value;
                    }
                    else
                    {
                        // Stage has not changed, set the value for this stage as the difference in estimates
                        newValue[(int) stage] = point.Value - previousEstimate;
                    }

                    previousEstimate = point.Value;
                    previousStage = stage;
                    
                    changesForAllStages.Add(point.WithValue(newValue));
                }
            }
            changesForAllStages.Sort((a, b) => a.Moment.CompareTo(b.Moment));

            return Enum.GetValues<Stage>().ToDictionary(stage => stage, stage =>
            {
                // Split off just the changes for the given stage
                var stageChanges = changesForAllStages
                    .Select(point => point.WithValue(point.Value[(int) stage]));
                
                // Accumulate changes and prunes changes before the start
                stageChanges = AccumulateAndMergeChanges(stageChanges, chartStart);

                // Converts timespan to hours
                return stageChanges
                    .Select(point => point.WithValue(point.Value.TotalHours))
                    .ToList();
            });
        }

        /// <summary>
        /// Computes the estimate and stage of a task over time
        /// </summary>
        /// <param name="task">Task to compute for</param>
        /// <returns>List of pairs of estimate points and the current stage</returns>
        private async Task<List<(BurndownPoint<TimeSpan>, Stage)>> GetTaskEstimateAndStageData(UserStoryTask task)
        {
            var invalidStage = (Stage) (-1);

            var stageChanges = await _taskChangelogRepository
                .GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Stage));
            

            var points = new List<(BurndownPoint<TimeSpan>, Stage)>();
            points.Add((BurndownPoint<TimeSpan>.NewTask(task.Created, task.OriginalEstimate, task), invalidStage));
            points.AddRange(stageChanges
                .Select(change => 
                    (BurndownPoint<TimeSpan>.StageChange(change.Created, TimeSpan.Zero, change), (Stage)change.ToValueObject)));
            points.AddRange((await _taskChangelogRepository
                    .GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Estimate)))
                .Select(change => 
                    (BurndownPoint<TimeSpan>.ScopeChange(change.Created, (TimeSpan)change.ToValueObject, change), invalidStage)));
            points.Sort((a, b) => a.Item1.Moment.CompareTo(b.Item1.Moment));

            
            
            var currentStage = task.Stage;
            if (stageChanges.Any())
            {
                currentStage = (Stage)stageChanges.Last().FromValueObject;
            }
            var currentEstimate = task.OriginalEstimate;

            // Propagate the current stage through the scope changes and new task points
            // and the estimate through the stage changes
            var stagePropagated = new List<(BurndownPoint<TimeSpan>, Stage)>();
            foreach (var (point, stage) in points)
            {
                if (stage != invalidStage)
                {
                    currentStage = stage;
                }
                else
                {
                    currentEstimate = point.Value;
                }
                stagePropagated.Add((point.WithValue(currentEstimate), currentStage));
            }

            return stagePropagated;
        }
    }
}