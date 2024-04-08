using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Services
{
    public interface IUserStoryTaskService
    {
        /// <summary>
        /// Updates the stages of every task within the sprint using the provided stage mapping function
        /// </summary>
        /// <param name="actingUser">User to make changes as</param>
        /// <param name="sprint">Sprint to select tasks from, must include tasks</param>
        /// <param name="stageMapping">Mapping from current stage to new stage</param>
        Task UpdateStages(User actingUser, Sprint sprint, Func<Stage, Stage> stageMapping);
        
        /// <summary>
        /// Updates the stages for tasks within an enumerable using the provided stage mapping function
        /// </summary>
        /// <param name="actingUser">User to make changes as</param>
        /// <param name="tasks">Tasks to update stages</param>
        /// <param name="stageMapping">Mapping from current stage to new stage</param>
        Task UpdateStages(User actingUser, IEnumerable<UserStoryTask> tasks, Func<Stage, Stage> stageMapping);
        
        /// <summary>
        /// Gets all the tasks a user will reflect on in preparation for some stand-up. Tasks meeting all of the
        /// following criteria will be returned:
        /// * The user is assigned to the task
        /// * The task is either: not 'done'; or, is 'done' and some work was logged since the last stand-up
        /// </summary>
        /// <param name="standUpMeeting">Stand-up meeting to get tasks to reflect on for</param>
        /// <param name="user">User for whom we are getting assigned tasks</param>
        /// <returns>Tasks that user should be prepared to talk about during stand-up</returns>
        Task<StandUpMeetingPreparationReport> GetTasksForUserInPreparationForStandUpAsync(StandUpMeeting standUpMeeting, User user);

        /// <summary>
        /// Returns the time a user was assigned to some task. If the user has been assigned more than once, we return
        /// the time of the most recent assignment. If we can not find a record of the user being assigned, null is returned.
        /// </summary>
        /// <param name="task">Task for which to get the time of user's assignment</param>
        /// <param name="user">User for whom to get the time they were (most recently) assigned to the task</param>
        /// <returns>DateTime of most recent assignment, or null if none is found</returns>
        Task<DateTime?> GetTimeUserWasAssignedToTask(UserStoryTask task, User user);
        
        /// <summary>
        /// Defers all incomplete tasks within a given story. If task is in done or deferred it stays in the same stage,
        /// otherwise its stage is changed to deferred.
        /// </summary>
        /// <param name="actingUser">User to make changes as</param>
        /// <param name="story">Story to select tasks from, must include tasks</param>
        Task DeferAllIncompleteTasksInStory(User actingUser, UserStory story);
    }
    
    public class UserStoryTaskService : IUserStoryTaskService
    {
        private readonly IUserStoryTaskRepository _userStoryTaskRepository;
        private readonly IUserStoryTaskChangelogRepository _userStoryTaskChangelogRepository;
        private readonly IStandUpMeetingRepository _standUpMeetingRepository;

        public UserStoryTaskService(IUserStoryTaskRepository userStoryTaskRepository, 
            IUserStoryTaskChangelogRepository userStoryTaskChangelogRepository,
            IStandUpMeetingRepository standUpMeetingRepository)
        {
            _userStoryTaskRepository = userStoryTaskRepository;
            _userStoryTaskChangelogRepository = userStoryTaskChangelogRepository;
            _standUpMeetingRepository = standUpMeetingRepository;
        }

        /// <inheritdoc/>
        public async Task UpdateStages(User actingUser, Sprint sprint, Func<Stage, Stage> stageMapping)
        {
            await UpdateStages(actingUser, sprint.Stories.SelectMany(story => story.Tasks), stageMapping);
        }

        /// <inheritdoc/>
        public async Task UpdateStages(User actingUser, IEnumerable<UserStoryTask> tasks, Func<Stage, Stage> stageMapping)
        {
            var updatedTasks = new List<UserStoryTask>();
            var taskChanges = new List<UserStoryTaskChangelogEntry>();
            foreach (var task in tasks)
            {
                var oldStage = task.Stage;
                var newStage = stageMapping(oldStage);
                if (newStage == oldStage) continue;
                task.Stage = newStage;
                updatedTasks.Add(task);
                taskChanges.Add(new(actingUser, task, nameof(UserStoryTask.Stage), Change<object>.Update(oldStage, newStage)));
            }

            var clonedTasks = updatedTasks.Select(task => task.CloneForPersisting()).ToList();
            
            await _userStoryTaskRepository.UpdateAllAsync(clonedTasks);
            await _userStoryTaskChangelogRepository.AddAllAsync(taskChanges);
            
            foreach (var (task, clone) in updatedTasks.Zip(clonedTasks)) task.RowVersion = clone.RowVersion;
        }

        /// <inheritdoc/>
        public async Task<StandUpMeetingPreparationReport> GetTasksForUserInPreparationForStandUpAsync(StandUpMeeting standUpMeeting, User user)
        {
            var assignedAndUnfinishedTasks = await _userStoryTaskRepository.GetTransformedAsync(
                UserStoryTaskFilters.WithAssignedUser(user),
                UserStoryTaskFilters.NotDoneOrDeferred,
                UserStoryTaskFilters.InBacklogOfSprint(standUpMeeting.Sprint),
                UserStoryTaskIncludes.Worklogs,
                UserStoryTaskIncludes.Story
            );

            var lastStandUp = await _standUpMeetingRepository.GetStandUpMeetingPriorTo(standUpMeeting);
            var workedOnSinceLastStandUp = await _userStoryTaskRepository.GetTransformedAsync(
                UserStoryTaskFilters.WasWorkedOnSinceTimeByUser(lastStandUp?.ScheduledStart ?? DateTime.MinValue, user),
                UserStoryTaskFilters.InBacklogOfSprint(standUpMeeting.Sprint),
                UserStoryTaskFilters.IdIsNotIn(assignedAndUnfinishedTasks.Select(x => x.Id)),
                UserStoryTaskIncludes.Worklogs,
                UserStoryTaskIncludes.Story
            );

            return new StandUpMeetingPreparationReport()
            {
                TasksWorkedOn = assignedAndUnfinishedTasks.Concat(workedOnSinceLastStandUp).OrderBy(x => x.Name)
                    .ToList(),
                PriorStandUpMeeting = lastStandUp
            };
        }

        /// <inheritdoc/>
        public async Task<DateTime?> GetTimeUserWasAssignedToTask(UserStoryTask task, User user)
        {
            // The field name is currently hardcoded in UserStoryTaskForm.ApplyAssigneeChanges
            // De-hardcoding this will be a lot of effort, so I've added a GitLab issue for this (#79)
            var assigneeChanges = await _userStoryTaskChangelogRepository
                .GetByUserStoryTaskAndFieldAsync(task, "Assignee", UserStoryTaskChangelogIncludes.UserChanged);
            return assigneeChanges
                .Where(x => ((UserTaskAssociationChangelogEntry)x)!.UserChangedId == user.Id)
                .Where(x => x.ToValue == "Assigned")
                .MaxBy(x => x.Created)?.Created;
        }

        /// <inheritdoc/>
        public async Task DeferAllIncompleteTasksInStory(User actingUser, UserStory story)
        {
            Stage StageMapping(Stage stage) => stage == Stage.Done ? stage : Stage.Deferred;
            await UpdateStages(actingUser, story.Tasks, StageMapping);
        }
    }
}
