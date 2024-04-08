using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Repositories;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using System.Threading.Tasks;
using ScrumBoard.Extensions;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services
{
    public class BurndownServiceTest 
    {
        private IBurndownService _burndownService;

        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);

        private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new(MockBehavior.Strict);

        private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new(MockBehavior.Strict);

        private readonly User _user = new();

        private readonly Sprint _sprint = new();
        
        private readonly Project _project = new();

        private DateTime _initialTime = DateTime.Now;

        /// <summary> Mock clock that keeps track of the total time passed after all the actions </summary>
        private DateTime _currentTime;

        /// <summary> The amount of time between each of the different types of actions (adding a task, updating task estimate, logging work) </summary>
        private readonly TimeSpan _actionTime = TimeSpan.FromHours(1);

        private Dictionary<UserStoryTask, TaskHistory> _histories = new();

        /// <summary> Class to keep track of the events that have occured to each task </summary>
        private class TaskHistory
        {
            public readonly List<WorklogEntry> Worklog = new();
            public readonly List<UserStoryTaskChangelogEntry> Changelog = new();
        }


        public BurndownServiceTest() 
        {
            var logger = new Mock<ILogger<BurndownService>>();
            _burndownService = new BurndownService(logger.Object, 
                _mockUserStoryTaskRepository.Object, 
                _mockUserStoryTaskChangelogRepository.Object, 
                _mockWorklogEntryService.Object
            );

            _sprint.TimeStarted = _initialTime - TimeSpan.FromHours(3);

            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetByStoryGroup(_sprint))
                .ReturnsAsync(() => _histories.Keys.ToList());

            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetByProject(_project, It.IsAny<Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>>>()))
                .ReturnsAsync(() => _histories.Keys.ToList());
            
            _currentTime = _initialTime;
        }

        /// <summary> Creates a new task at the current time and increments the time </summary>
        private TaskHistory AddTask(UserStoryTask task)
        {
            var history = new TaskHistory();
            _histories.Add(task, history);

            task.OriginalEstimate = task.Estimate;
            task.Created = _currentTime;

            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Estimate)))
                .ReturnsAsync(() => history.Changelog.Where(change => change.FieldChanged == nameof(UserStoryTask.Estimate)).ToList());
            
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Stage)))
                .ReturnsAsync(() => history.Changelog.Where(change => change.FieldChanged == nameof(UserStoryTask.Stage)).ToList());

            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _mockWorklogEntryService
                .Setup(mock => mock.GetWorklogEntriesForTaskAsync(task.Id))
                .ReturnsAsync(history.Worklog);

            _currentTime += _actionTime;
            return history;
        }

        /// <summary> Updates the estimate for a task and increments the current time </summary>
        private UserStoryTaskChangelogEntry UpdateEstimate(UserStoryTask task, TimeSpan newEstimate, long changelogEntryId)
        {
            var history = _histories[task];

            var changelogEntry = new UserStoryTaskChangelogEntry(_user, task, nameof(UserStoryTask.Estimate), Change<object>.Update(task.Estimate, newEstimate));
            changelogEntry.UserStoryTaskChanged = task;
            changelogEntry.Created = _currentTime;
            changelogEntry.Id = changelogEntryId;
            history.Changelog.Add(changelogEntry);
            task.Estimate = newEstimate;

            _currentTime += _actionTime;
            return changelogEntry;
        }
        
        /// <summary> Updates the estimate for a task and increments the current time </summary>
        private UserStoryTaskChangelogEntry UpdateStage(UserStoryTask task, Stage newStage, long changelogEntryId)
        {
            var history = _histories[task];

            var changelogEntry = new UserStoryTaskChangelogEntry(_user, task, nameof(UserStoryTask.Stage), Change<object>.Update(task.Stage, newStage));
            changelogEntry.UserStoryTaskChanged = task;
            changelogEntry.Created = _currentTime;
            changelogEntry.Id = changelogEntryId;
            history.Changelog.Add(changelogEntry);
            task.Stage = newStage;

            _currentTime += _actionTime;
            return changelogEntry;
        }

        /// <summary> Logs work on a task and updates the current time </summary>
        private void LogWork(WorklogEntry worklogEntry)
        {
            var history = _histories[worklogEntry.Task];
            worklogEntry.Occurred = _currentTime;
            worklogEntry.Created = _currentTime;
            history.Worklog.Add(worklogEntry);
            _currentTime += _actionTime;
        }

        [Fact]
        public async Task GetTaskTimeDeltas_NoChangesMade_OnlyInitialEstimatePoint() {
            var initialEstimate = TimeSpan.FromHours(1);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
                Id = 6,
            };
            AddTask(task);

            var deltas = await _burndownService.GetTaskTimeDeltas(task);

            deltas.Should().HaveCount(1);
            var entry = deltas.First();

            _mockUserStoryTaskChangelogRepository
                .Verify(mock => mock.GetByUserStoryTaskAndFieldAsync(task, nameof(UserStoryTask.Estimate)), Times.Once());
            _mockWorklogEntryService
                .Verify(mock => mock.GetWorklogEntriesForTaskAsync(task.Id), Times.Once());

            entry.Moment.Should().Be(_initialTime);
            entry.Value.Should().Be(initialEstimate);
            entry.Type.Should().Be(BurndownPointType.NewTask);
            entry.Id.Should().Be(task.Id);
        }

        [Fact]
        public async Task GetTaskTimeDeltas_OneWorklog_InitialEstimateAndWorklogPoints() {
            var initialEstimate = TimeSpan.FromHours(2);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
                Id = 5,
            };
            AddTask(task);
            var timeLogged = TimeSpan.FromHours(1);
            var worklogEntry = new WorklogEntry() {
                Task = task,
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(timeLogged) }, 
                Id = 40,
            };
            LogWork(worklogEntry);

            var deltas = (await _burndownService.GetTaskTimeDeltas(task)).ToList();

            deltas.Should().HaveCount(2);

            deltas[0].Moment.Should().Be(_initialTime);
            deltas[0].Value.Should().Be(initialEstimate);
            deltas[0].Type.Should().Be(BurndownPointType.NewTask);
            deltas[0].Id.Should().Be(task.Id);

            deltas[1].Moment.Should().Be(_initialTime + _actionTime);
            deltas[1].Value.Should().Be(-timeLogged);
            deltas[1].Type.Should().Be(BurndownPointType.Worklog);
            deltas[1].Id.Should().Be(worklogEntry.Id);
        }

        [Fact]
        public async Task GetTaskTimeDeltas_OneChange_InitialEstimateAndChangelogPoints() {
            var initialEstimate = TimeSpan.FromHours(1);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
                Id = 5,
            };
            AddTask(task);
            var newEstimate = TimeSpan.FromHours(3);
            var changelogEntryId = 13;
            UpdateEstimate(task, newEstimate, changelogEntryId);

            var deltas = (await _burndownService.GetTaskTimeDeltas(task)).ToList();

            deltas.Should().HaveCount(2);

            deltas[0].Moment.Should().Be(_initialTime);
            deltas[0].Value.Should().Be(initialEstimate);
            deltas[0].Type.Should().Be(BurndownPointType.NewTask);
            deltas[0].Id.Should().Be(task.Id);

            deltas[1].Moment.Should().Be(_initialTime + _actionTime);
            deltas[1].Value.Should().Be(newEstimate - initialEstimate);
            deltas[1].Type.Should().Be(BurndownPointType.ScopeChange);
            deltas[1].Id.Should().Be(changelogEntryId);
        }

        [Fact]
        public async Task GetTaskTimeDeltas_MoreWorkLoggedThanRemaining_ChangeIsClamped() {
            var initialEstimate = TimeSpan.FromHours(1);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
            };
            AddTask(task);
            var timeLogged = TimeSpan.FromHours(10);
            var worklogEntry = new WorklogEntry() {
                Task = task,
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(timeLogged) }, 
            };
            LogWork(worklogEntry);

            var deltas = (await _burndownService.GetTaskTimeDeltas(task)).ToList();

            deltas.Should().HaveCount(2);

            deltas[0].Moment.Should().Be(_initialTime);
            deltas[0].Value.Should().Be(initialEstimate);

            deltas[1].Moment.Should().Be(_initialTime + _actionTime);
            deltas[1].Value.Should().Be(-initialEstimate);
        }
        
        [Theory]
        [InlineData(Stage.Deferred)]
        [InlineData(Stage.Done)]
        public async Task GetTaskTimeDeltas_DoneOrDeferredForAPeriod_BurndownDropsToZeroAndReturns(Stage zeroValueTaskStage) {
            var initialEstimate = TimeSpan.FromHours(2);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
                Stage = Stage.InProgress,
            };
            AddTask(task);

            UpdateStage(task, zeroValueTaskStage, 71);
            var timeLogged = TimeSpan.FromHours(1);
            var worklogEntry = new WorklogEntry() {
                Task = task,
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(timeLogged) }, 
            };
            LogWork(worklogEntry);
            UpdateStage(task, Stage.InProgress, 72);

            var deltas = (await _burndownService.GetTaskTimeDeltas(task)).ToList();

            deltas.Should().HaveCount(4);

            deltas[0].Moment.Should().Be(_initialTime);
            deltas[0].Value.Should().Be(initialEstimate);
            deltas[0].Type.Should().Be(BurndownPointType.NewTask);

            deltas[1].Moment.Should().Be(_initialTime + _actionTime);
            deltas[1].Value.Should().Be(-initialEstimate);
            deltas[1].Type.Should().Be(BurndownPointType.StageChange);

            deltas[2].Moment.Should().Be(_initialTime + 2 * _actionTime);
            deltas[2].Value.Should().Be(TimeSpan.Zero);
            deltas[2].Type.Should().Be(BurndownPointType.Worklog);
            
            deltas[3].Moment.Should().Be(_initialTime + 3 * _actionTime);
            deltas[3].Value.Should().Be(initialEstimate - timeLogged);
            deltas[3].Type.Should().Be(BurndownPointType.StageChange);
        }

        [Fact]
        public async Task GetBurndownData_NoTasks_SinglePointReturned() {
            _sprint.TimeStarted = _initialTime;
            var deltas = await _burndownService.GetData(_sprint, false);

            deltas.Should().HaveCount(1);
            var entry = deltas.First();

            entry.Moment.Should().Be(_initialTime);
            entry.Value.Should().Be(0.0);
            entry.Type.Should().Be(BurndownPointType.None);
        }

        [Fact]
        public async Task GetBurndownData_AllPointsBeforeSprintStart_SinglePoint()
        {
            var sprintStart = _initialTime + _actionTime * 2;
            _sprint.TimeStarted = sprintStart;

            var estimate1 = TimeSpan.FromHours(1);
            AddTask(new UserStoryTask() { Estimate = estimate1 });
            var estimate2 = TimeSpan.FromHours(2);
            AddTask(new UserStoryTask() { Estimate = estimate2 });

            var deltas = await _burndownService.GetData(_sprint, false);

            deltas.Should().HaveCount(1);
            var entry = deltas.First();

            entry.Moment.Should().Be(sprintStart);
            entry.Value.Should().Be((estimate1 + estimate2).TotalHours);
            entry.Type.Should().Be(BurndownPointType.None);
        }

        [Fact]
        public async Task GetBurndownData_MultipleTasks_ManyPoints()
        {
            var sprintStart = _initialTime - _actionTime;
            _sprint.TimeStarted = sprintStart;

            var estimate1 = TimeSpan.FromHours(1);
            AddTask(new UserStoryTask() { Estimate = estimate1, Id = 4 });
            var estimate2 = TimeSpan.FromHours(2);
            AddTask(new UserStoryTask() { Estimate = estimate2, Id = 6 });

            var deltas = await _burndownService.GetData(_sprint, false);

            deltas.Should().HaveCount(3);

            deltas[0].Moment.Should().Be(sprintStart);
            deltas[0].Value.Should().Be(0.0);
            deltas[0].Type.Should().Be(BurndownPointType.None);

            deltas[1].Moment.Should().Be(_initialTime);
            deltas[1].Value.Should().Be(estimate1.TotalHours);
            deltas[1].Type.Should().Be(BurndownPointType.NewTask);
            deltas[1].Id.Should().Be(4);

            deltas[2].Moment.Should().Be(_initialTime + _actionTime);
            deltas[2].Value.Should().Be((estimate1 + estimate2).TotalHours);
            deltas[2].Type.Should().Be(BurndownPointType.NewTask);
            deltas[2].Id.Should().Be(6);
        }

        [Fact]
        public async Task GetBurnupData_NoTasks_SinglePointReturned() {
            _sprint.TimeStarted = _initialTime;
            var deltas = await _burndownService.GetData(_sprint, true);

            deltas.Should().HaveCount(1);
            var entry = deltas.First();

            entry.Moment.Should().Be(_initialTime);
            entry.Value.Should().Be(0.0);
            entry.Type.Should().Be(BurndownPointType.None);
        }

        [Fact]
        public async Task GetBurnupData_LogMoreThanEstimate_ResultNotClamped() {
            var sprintStart = _initialTime - _actionTime;
            _sprint.TimeStarted = sprintStart;

            var initialEstimate = TimeSpan.FromHours(2);
            var task = new UserStoryTask() {
                Estimate = initialEstimate,
                Id = 5,
            };
            AddTask(task);         
            var worklogDuration = TimeSpan.FromHours(1);
            var worklogEntry = new WorklogEntry() {
                Task = task,
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(worklogDuration) }, 
                Id = 40,
            };
            LogWork(worklogEntry);
            var anotherWorklogDuration = TimeSpan.FromHours(1);
            var anotherWorklogEntry = new WorklogEntry() {
                Task = task,
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(anotherWorklogDuration) }, 
                Id = 41,
            };
            LogWork(anotherWorklogEntry);

            var deltas = await _burndownService.GetData(_sprint, true);

            deltas.Should().HaveCount(3);

            deltas[0].Moment.Should().Be(sprintStart);
            deltas[0].Value.Should().Be(0.0);
            deltas[0].Type.Should().Be(BurndownPointType.None);

            deltas[1].Moment.Should().Be(_initialTime + worklogDuration);
            deltas[1].Value.Should().Be(worklogDuration.TotalHours);
            deltas[1].Type.Should().Be(BurndownPointType.Worklog);
            deltas[1].Id.Should().Be(40);

            deltas[2].Moment.Should().Be(_initialTime + _actionTime + worklogDuration);
            deltas[2].Value.Should().Be((worklogDuration + anotherWorklogDuration).TotalHours);
            deltas[2].Type.Should().Be(BurndownPointType.Worklog);
            deltas[2].Id.Should().Be(41);
        }

        [Fact]
        public async Task GetFlowData_ByProject_TasksForProjectRequested()
        {
            await _burndownService.GetFlowData(_project);
            
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetByProject(_project, It.IsAny<Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>>>()), Times.Once);
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetByStoryGroup(It.IsAny<StoryGroup>()), Times.Never);
        }
        
        [Fact]
        public async Task GetFlowData_BySprint_TasksForSprintRequested()
        {
            await _burndownService.GetFlowData(_sprint);
            
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetByProject(It.IsAny<Project>(),It.IsAny<Func<IQueryable<UserStoryTask>, IQueryable<UserStoryTask>>[]>()), Times.Never);
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetByStoryGroup(_sprint), Times.Once);
        }
        
        [Fact]
        public async Task GetFlowData_ByProjectNoTasks_SinglePointForProjectStart()
        {
            var lines = await _burndownService.GetFlowData(_project);

            var timeStarted = _project.StartDate.ToDateTime(TimeOnly.MinValue);

            lines.Select(entry => entry.Value).Should()
                .OnlyContain(line => line.Single().Type == BurndownPointType.None).And
                .OnlyContain(line => line.Single().Moment == timeStarted);
        }
        
        [Fact]
        public async Task GetFlowData_BySprintNoTasks_SinglePointForSprintStart()
        {
            var lines = await _burndownService.GetFlowData(_sprint);

            var timeStarted = _sprint.TimeStarted.Value;

            lines.Select(entry => entry.Value).Should()
                .OnlyContain(line => line.Single().Type == BurndownPointType.None).And
                .OnlyContain(line => line.Single().Moment == timeStarted);
        }

        [Theory]
        [EnumData(typeof(Stage))]
        public async Task GetFlowData_TaskAdded_LineForStageHasPointForAtTaskEstimate(Stage initialTaskStage)
        {
            var taskEstimate = TimeSpan.FromHours(3.5);
            var taskId = 4;
            AddTask(new UserStoryTask()
            {
                Id = taskId,
                Estimate = taskEstimate, 
                Stage = initialTaskStage,
            });

            var lines = await _burndownService.GetFlowData(_project);
            
            lines.Select(entry => entry.Value).Should()
                .OnlyContain(line => line.Count == 2).And
                .OnlyContain(line => line.Last().Id == taskId).And
                .OnlyContain(line => line.Last().Type == BurndownPointType.NewTask);

            lines[initialTaskStage].Last().Value.Should().Be(taskEstimate.TotalHours);
            lines
                .Where(entry => entry.Key != initialTaskStage)
                .Select(entry => entry.Value)
                .Should()
                .OnlyContain(line => line.All(point => point.Value == 0.0));
        }
        
        [Theory]
        [EnumData(typeof(Stage))]
        public async Task GetFlowData_TaskChangeStage_LinesSwapAroundEstimate(Stage initialTaskStage)
        {
            var values = Enum.GetValues<Stage>();
            var finalTaskStage = values[((int) initialTaskStage + 1) % values.Length];
            
            var taskEstimate = TimeSpan.FromHours(3.5);
            var taskId = 4;

            var task = new UserStoryTask()
            {
                Id = taskId,
                Estimate = taskEstimate,
                Stage = initialTaskStage,
            };
            AddTask(task);

            var changelogId = 14;
            UpdateStage(task, finalTaskStage, changelogId);

            var lines = await _burndownService.GetFlowData(_project);
            
            lines.Select(entry => entry.Value).Should()
                .OnlyContain(line => line.Count == 3).And
                .OnlyContain(line => line.First().Type == BurndownPointType.None).And
                .OnlyContain(line => line.Last().Type == BurndownPointType.StageChange).And
                .OnlyContain(line => line.Last().Id == changelogId);

            lines[initialTaskStage].ElementAt(1).Value.Should().Be(taskEstimate.TotalHours);
            lines[initialTaskStage].ElementAt(2).Value.Should().Be(0.0);
            
            lines[finalTaskStage].ElementAt(1).Value.Should().Be(0.0);
            lines[finalTaskStage].ElementAt(2).Value.Should().Be(taskEstimate.TotalHours);

            lines
                .Where(entry => entry.Key != initialTaskStage && entry.Key != finalTaskStage)
                .Select(entry => entry.Value)
                .Should().OnlyContain(line => line.All(point => point.Value == 0.0));
        }
        
        [Fact]
        public async Task GetFlowData_TaskScopeChanged_LineForStageUpdated()
        {
            var initialEstimate = TimeSpan.FromHours(3.5);
            var taskId = 4;
            var task = new UserStoryTask()
            {
                Id = taskId,
                Estimate = initialEstimate,
                Stage = Stage.Todo,
            };
            AddTask(task);
            
            var changelogId = 14;
            var newEstimate = TimeSpan.FromHours(4);
            UpdateEstimate(task, newEstimate, changelogId);

            var lines = await _burndownService.GetFlowData(_project);
            
            lines.Select(entry => entry.Value).Should()
                .OnlyContain(line => line.Count == 3).And
                .OnlyContain(line => line.Last().Type == BurndownPointType.ScopeChange).And
                .OnlyContain(line => line.Last().Id == changelogId);

            lines[task.Stage].ElementAt(1).Value.Should().Be(initialEstimate.TotalHours);
            lines[task.Stage].ElementAt(2).Value.Should().Be(newEstimate.TotalHours);
            lines
                .Where(entry => entry.Key != task.Stage)
                .Select(entry => entry.Value)
                .Should().OnlyContain(line => line.All(point => point.Value == 0.0));
        }

        [Fact]
        public async Task GenerateMessage_NewTaskPoint_GeneratesCorrectMessage()
        {
            var task = new UserStoryTask()
            {
                Id = 17,
                OriginalEstimate = TimeSpan.FromHours(2),
                Name = "Mock task"
            };
            AddTask(task);
            var point = await _burndownService.GenerateMessage(BurndownPoint<double>.NewTask(_currentTime, 0.0, task));
            var stringMessage = string.Join(' ', point.GenerateMessage().Select(m => m.ToString()));

            stringMessage.Should().Be($"Added task {task.Name} with estimate {DurationUtils.DurationStringFrom(task.OriginalEstimate)}");
        }
        
        [Fact]
        public async Task GenerateMessage_UpdatedTaskPoint_GeneratesCorrectMessage()
        {
            var task = new UserStoryTask()
            {
                Id = 17,
                OriginalEstimate = TimeSpan.FromHours(2),
                Name = "Mock task",
            };
            AddTask(task);
            var changelogEntry = UpdateEstimate(task, TimeSpan.FromHours(3), 42);
            
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.GetByIdAsync(
                    changelogEntry.Id,
                    UserStoryTaskChangelogIncludes.TaskChanged
                    ))
                .ReturnsAsync(changelogEntry);
            
            var point = await _burndownService.GenerateMessage(BurndownPoint<double>.ScopeChange(_currentTime, 0.0, changelogEntry));
            var stringMessage = string.Join(' ', point.GenerateMessage().Select(m => m.ToString()));
        
            stringMessage.Should().Be($"Estimate of {task.Name} changed {DurationUtils.DurationStringFrom(task.OriginalEstimate)} -> {DurationUtils.DurationStringFrom(task.Estimate)}");
        }
        
        [Fact]
        public async Task GenerateMessage_DeferredTaskPoint_GeneratesCorrectMessage()
        {
            var originalStage = Stage.InProgress;
            var newStage = Stage.Deferred;
            var task = new UserStoryTask()
            {
                Id = 17,
                Name = "Mock task",
                Stage = originalStage,
            };
            AddTask(task);
            var changelogEntry = UpdateStage(task, newStage, 42);
            
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.GetByIdAsync(
                    changelogEntry.Id,
                    UserStoryTaskChangelogIncludes.TaskChanged
                ))
                .ReturnsAsync(changelogEntry);
            
            var point = await _burndownService.GenerateMessage(BurndownPoint<double>.StageChange(_currentTime, 0.0, changelogEntry));
            var stringMessage = string.Join(' ', point.GenerateMessage().Select(m => m.ToString()));
        
            stringMessage.Should().Be($"Stage of {task.Name} changed {originalStage} -> {newStage}");
        }
        
        [Fact]
        public async Task GenerateMessage_WorklogEntryPoint_GeneratesCorrectMessage()
        {
            var task = new UserStoryTask()
            {
                Id = 17,
                OriginalEstimate = TimeSpan.FromHours(2),
                Name = "Mock task"
            };
            AddTask(task);
            var worklogEntry = new WorklogEntry()
            {
                Id = 13,
                Task = task,
                User = new User()
                {
                    FirstName = "Tim",
                    LastName = "Tam",
                },
                TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(5)) }, 
            };
            LogWork(worklogEntry);
            
            _mockWorklogEntryService
                .Setup(mock => mock.GetWorklogEntryByIdAsync(worklogEntry.Id, true))
                .ReturnsAsync(worklogEntry);
            
            var point = await _burndownService.GenerateMessage(BurndownPoint<double>.Worklog(_currentTime, 0.0, worklogEntry));
            var stringMessage = string.Join(' ', point.GenerateMessage().Select(m => m.ToString()));
        
            stringMessage.Should().Be($"{worklogEntry.User.GetFullName()} logged {DurationUtils.DurationStringFrom(worklogEntry.GetTotalTimeSpent())} on {task.Name}");
        }
    }
}