using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Filters;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class WorklogTableComponentTest : TestContext
    {
        private readonly User _actingUser = new() {Id = 33, FirstName = "Jeff", LastName = "Jefferson"};

        private readonly User _pairUser = new() { Id = 101, FirstName = "Pair", LastName = "User" };

        private static readonly User _taskCreator = new() {Id = 34, FirstName = "Thomas", LastName = "Creator"};      

        private Project _currentProject;
        
        private IRenderedComponent<WorklogTable> _component;
        
        //Mocks
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new();
        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new();
        private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new();
        private readonly Mock<IProjectRepository> _mockProjectRepository = new();
        private readonly Mock<IUserStoryTaskTagRepository> _mockTaskTagRepository = new(MockBehavior.Strict);
        private readonly Mock<IWorklogTagRepository> _mockWorklogTagRepository = new(MockBehavior.Strict);
        private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();

        public WorklogTableComponentTest()
        {
            _mockTaskTagRepository
                .Setup(mock => mock.GetAllAsync())
                .ReturnsAsync(new List<UserStoryTaskTag>());
            _mockWorklogTagRepository
                .Setup(mock => mock.GetAllAsync())
                .ReturnsAsync(new List<WorklogTag>());
            _mockStateStorageService
                .Setup(mock => mock.GetTableColumnConfiguration())
                .ReturnsAsync((List<TableColumnConfiguration>)null);
            
            Services.AddScoped(_ => _mockTaskTagRepository.Object);
            Services.AddScoped(_ => _mockWorklogTagRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockWorklogEntryService.Object);
            Services.AddScoped(_ => _mockStateStorageService.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
            ComponentFactories.AddDummyFactoryFor<SortableList<TableColumnConfiguration>>();
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
        }

        private async Task SetupComponent(bool hasSprint=false, bool hasArchivedSprint=false, bool noCurrentSprint=false)
        {            
            _currentProject = new Project
            {
                Id = 1,
                StartDate = DateOnly.FromDateTime(DateTime.Now),    
                Sprints = new List<Sprint>()                     
            };            
            _currentProject.MemberAssociations.Add(new ProjectUserMembership { User = _actingUser, Project = _currentProject, UserId = _actingUser.Id, ProjectId = _currentProject.Id, Role = ProjectRole.Developer});
            _currentProject.MemberAssociations.Add(new ProjectUserMembership { User = _pairUser, Project = _currentProject, UserId = _pairUser.Id, ProjectId = _currentProject.Id, Role = ProjectRole.Developer});
            _currentProject.MemberAssociations.Add(new ProjectUserMembership { User = _taskCreator, Project = _currentProject, UserId = _taskCreator.Id, ProjectId = _currentProject.Id, Role = ProjectRole.Developer});

            _mockProjectRepository
                .Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(_currentProject);
            
            if (hasSprint) {
                // Current sprint data
                await AddCurrentSprintWorklogs();  
            }               

            if (hasArchivedSprint) {
                // Archived sprint data
                AddArchivedSprintWorklogs();  
            }

            var projectWorklogEntries = _currentProject.Sprints.SelectMany(sprint => sprint.Stories).SelectMany(story => story.Tasks).SelectMany(task => task.Worklog).AsQueryable();
            var entryList = await PaginatedList<WorklogEntry>.CreateAsync(projectWorklogEntries, 1, 10);
            _mockWorklogEntryService.Setup(x => x.GetByProjectFilteredAndPaginatedAsync(
                It.IsAny<long>(),
                It.IsAny<WorklogEntryFilter>(),
                It.IsAny<TableColumn>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<long?>()
            )).ReturnsAsync(entryList);
            
            Sprint addedSprint = _currentProject.Sprints.FirstOrDefault();
            if (addedSprint != null && !noCurrentSprint) {
                _component = RenderComponent<WorklogTable>(parameters => parameters
                    .AddCascadingValue("Self", _actingUser)
                    .AddCascadingValue("ProjectState", new ProjectState { ProjectId = _currentProject.Id })
                    .Add(cut => cut.Sprint, addedSprint)
                    .Add(cut => cut.IsMarkingTable, false)
                );
            } else {
                _component = RenderComponent<WorklogTable>(parameters => parameters
                    .AddCascadingValue("ProjectState", new ProjectState { ProjectId = _currentProject.Id })
                    .AddCascadingValue("Self", _actingUser)
                    .Add(cut => cut.IsMarkingTable, false)
                );
            }            
        }

        private async Task AddCurrentSprintWorklogs()
        {
            Sprint currentSprint = new() {
                Name = "A test sprint", 
                Stage = SprintStage.Started,
                StartDate = _currentProject.StartDate.AddDays(-2), 
                EndDate = _currentProject.StartDate.AddDays(100),                
                Created = DateTime.Now, 
                Creator = _actingUser,
                TimeStarted = DateTime.Now,
                Project = _currentProject
            };

            UserStory currentStory1 = new() {
                Project = _currentProject,
                StoryGroup = currentSprint,
                Creator = _actingUser,
                Created = DateTime.Now,
                Stage = Stage.Todo, 
                Estimate = 13, 
                Name = "Story1 Name", 
                Description = "Story Description",
            };
            currentSprint.Stories = new List<UserStory>() { currentStory1 };

            
            UserStoryTask currentTask1 = new() {
                Id = 1234,
                Name="Task1 Name" , 
                Description="Task description", 
                Tags=new List<UserStoryTaskTag>(),
                Created=DateTime.Now, 
                Creator=_actingUser,
                Priority=Priority.High,
                Stage=Stage.Todo,
                Estimate = TimeSpan.FromHours(2),
                UserStory = currentStory1,
                OriginalEstimate = TimeSpan.FromHours(2)           
            };            

            for (int i=0; i < 26; i++) {
                currentTask1.Worklog.Add(new WorklogEntry() { 
                    User = _actingUser,
                    PairUser = _pairUser,
                    Task = currentTask1, 
                    TaskId = currentTask1.Id,
                    Description = "description", 
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now.AddDays(i), 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromMinutes(20)) }, 
                });
            }            
            currentStory1.Tasks = new List<UserStoryTask>() { currentTask1 }; 

            var sprintWorklogEntries = currentSprint.Stories.SelectMany(story => story.Tasks).SelectMany(task => task.Worklog).AsQueryable();
            PaginatedList<WorklogEntry> sprintEntryList = await PaginatedList<WorklogEntry>.CreateAsync(sprintWorklogEntries, 1, 10);
            _mockWorklogEntryService.Setup(x => x.GetByProjectFilteredAndPaginatedAsync(
                It.IsAny<long>(),
                It.IsAny<WorklogEntryFilter>(),
                It.IsAny<TableColumn>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<long?>()
            )).ReturnsAsync(sprintEntryList); 

            _mockWorklogEntryService
                .Setup(x => x.GetWorklogEntriesForProjectAsync(It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<long?>()))
                .ReturnsAsync(sprintWorklogEntries);
            _mockWorklogEntryService
                .Setup(x => x.GetWorklogEntriesForTaskAsync(currentTask1.Id))
                .ReturnsAsync(currentTask1.Worklog);
            
            _currentProject.Sprints.Add(currentSprint);        
        }

        private void AddArchivedSprintWorklogs() {
            Sprint archivedSprint = new() {
                Name = "A test sprint", 
                Stage = SprintStage.Started,
                StartDate = _currentProject.StartDate.AddDays(-10), 
                EndDate = _currentProject.StartDate.AddDays(5),                
                Created = DateTime.Now.AddDays(-11), 
                TimeStarted = DateTime.Now,
                Project = _currentProject             
            };

            UserStory archivedStory = new() {
                Project = _currentProject,
                StoryGroup = archivedSprint,
                Creator = _actingUser,
                Created = DateTime.Now,
                Stage = Stage.Todo, 
                Estimate = 13, 
                Name = "Archived Story Name", 
                Description = "Archived Story Description",
            };
            archivedSprint.Stories = new List<UserStory>() { archivedStory };
            
            UserStoryTask archivedTask = new() {
                Id = 12345,
                Name="Archived Task Name" , 
                Description="Task description", 
                Tags=new List<UserStoryTaskTag>(),
                Created=DateTime.Now, 
                Creator=_actingUser,
                Priority=Priority.High,
                Stage=Stage.Todo,
                Estimate = TimeSpan.FromHours(2),
                UserStory = archivedStory,
                OriginalEstimate = TimeSpan.FromHours(2)           
            };            

            for (var i=0; i < 20; i++) {
                archivedTask.Worklog.Add(new WorklogEntry() { 
                    User = _actingUser,
                    PairUser = _pairUser,
                    Task = archivedTask, 
                    TaskId = archivedTask.Id,
                    Description = "description", 
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now.AddDays(i), 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromMinutes(20)) }, 
                });
            }            
            archivedStory.Tasks = new List<UserStoryTask>() { archivedTask }; 
            _currentProject.Sprints.Add(archivedSprint);

            var allWorklogs = _currentProject.Sprints.SelectMany(sprint => sprint.Stories).SelectMany(story => story.Tasks).SelectMany(task => task.Worklog).AsQueryable();

            _mockWorklogEntryService
                .Setup(x => x.GetWorklogEntriesForProjectAsync(It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<long?>()))
                .ReturnsAsync(allWorklogs);
            _mockWorklogEntryService
                .Setup(x => x.GetWorklogEntriesForTaskAsync(archivedTask.Id))
                .ReturnsAsync(archivedTask.Worklog);
        }

        [Fact]
        public async Task DefaultState_TableContainsEntries_PageButtonsVisible()
        {
            await SetupComponent(true);

            _component.WaitForState(() => _component.FindAll("#pagination-container").Count == 1);
            _component.FindAll("#pagination-container").Should().HaveCount(1);
        }

        [Fact]
        public async Task NoEntries_PageButtonsNotVisibleAndPlaceholderDisplayed()
        {
            await SetupComponent();

            _component.FindAll("#pagination-container").Should().BeEmpty();
            _component.FindAll("#table-row-placeholder").Should().ContainSingle();
        }

        [Fact]
        public async Task DefaultState_ProjectHasSprints_WorklogsDisplayed()
        {        
            await SetupComponent(true);            
            _component.WaitForAssertion(() => _component.FindAll(".worklog-table-entry").Should().HaveCount(10));           
        }

        [Fact]
        public async Task DefaultState_ProjectHasNoSprints_NoWorklogsDisplayed() {            
            await SetupComponent(false);

            _component.FindAll(".worklog-table-entry").Should().BeEmpty();
        }

        [Fact]
        public async Task DefaultState_WorklogHasStories_DisplaysCorrectTotal() {
            await SetupComponent(true);
            _component.Find("#total-stories-worked").TextContent.Should().Contain("1");
        }

        [Fact]
        public async Task DefaultState_WorklogsHaveDurations_DisplaysCorrectTotal() {
            await SetupComponent(true);
            _component.Find("#total-time-logged").TextContent.Should().Contain(DurationUtils.DurationStringFrom(TimeSpan.FromMinutes(20).Multiply(26)));
        }

        [Fact]
        public async Task DefaultState_WorklogHasStoryPoints_DisplaysCorrectTotal() {
            await SetupComponent(true);
            _component.Find("#total-story-points").TextContent.Should().Contain("13");
        }

        [Fact]
        public async Task DefaultState_CurrentSprintSelected_OnlyCurrentSprintWorklogsSummarised() {
            await SetupComponent(true);
            _component.Find("#total-story-points").TextContent.Should().Contain("13");
            _component.Find("#total-stories-worked").TextContent.Should().Contain("1");
            _component.Find("#total-time-logged").TextContent.Should().Contain(DurationUtils.DurationStringFrom(TimeSpan.FromMinutes(20).Multiply(26)));
        }

        [Fact]
        public async Task DefaultState_ChangeTableScope_AllSprintsSelected_AllWorklogsSummarised() {
            await SetupComponent(true, true, true);  
            _component.WaitForAssertion(() => _component.Find("#total-story-points").TextContent.Should().Contain("26"), TimeSpan.FromSeconds(3));          
            _component.Find("#total-stories-worked").TextContent.Should().Contain("2");
            _component.Find("#total-time-logged").TextContent.Should().Contain(DurationUtils.DurationStringFrom(TimeSpan.FromMinutes(20).Multiply(46)));
        }

        [Fact]
        public async Task FilterByOneAssignee_SummaryRowDisplayed() {
            await SetupComponent(true);
            _component.Find($"#table-user-select-{_actingUser.Id}").Click();
            _component.FindAll("#table-summary-row").Should().ContainSingle();            
        }

        [Fact]
        public async Task FilterByMultipleAssignees_SummaryRowDisplayed() {
            await SetupComponent(true);
            _component.Find($"#table-user-select-{_actingUser.Id}").Click();
            _component.Find($"#table-user-select-{_taskCreator.Id}").Click();
            _component.FindAll("#table-summary-row").Should().ContainSingle();
            _component.Find("#total-time-filtered").TextContent.Should().Contain("Filtered Users");
        }

        [Fact]
        public async Task NotFilteringByAssignee_SummaryRowDisplayedWithAllUserSummary() {
            await SetupComponent(true);            
            _component.FindAll("#table-summary-row").Should().ContainSingle();
            _component.Find("#total-time-filtered").TextContent.Should().Contain("All Users");
        }

        [Theory]
        [EnumData(typeof(TableColumn))]
        public async Task Rendered_SingleColumnSelected_SingleColumnShown(TableColumn column)
        {
            if (column is TableColumn.IssueTags) return; // pass the issue tags column, which is not present on the work logs page 
            var columnConfigurations = Enum.GetValues<TableColumn>()
                .Select(other => new TableColumnConfiguration() {Column = other, Hidden = other != column})
                .ToList();

            _mockStateStorageService
                .Setup(mock => mock.GetTableColumnConfiguration())
                .ReturnsAsync(columnConfigurations);

            await SetupComponent(true);

            var columns = _component.Find("tbody").QuerySelectorAll("td");
            columns.Should().HaveCount(10);
            columns.Select(element => element.Id).Should().OnlyContain(id => id.Contains(column.ToString()));
        }
        
        [Fact]
        public async Task Rendered_AllColumnsSelected_EveryColumnShown()
        {
            await SetupComponent(true);

            var columns = _component.Find("tbody").QuerySelectorAll("td");
            // exclude the columns which are only shown on the marking report, which is not included on the work logs page
            columns.Should().HaveCount(10 * Enum.GetValues<TableColumn>().Count(x => !x.IsForMarkingTableOnly())); 
        }

        [Fact]
        public async Task Rendered_UpdateConfiguration_ConfigurationSaved()
        {
            await SetupComponent(true);

            var newConfiguration = new List<TableColumnConfiguration>()
            {
                new() { Column = TableColumn.Occurred, Hidden = false },
                new() { Column = TableColumn.CurrentEstimate, Hidden = true },
            };

            _mockStateStorageService
                .Setup(mock => mock.SetTableColumnConfiguration(newConfiguration))
                .Returns(Task.CompletedTask);

            var configurationSortableList = _component.FindComponent<Dummy<SortableList<TableColumnConfiguration>>>();
            await _component.InvokeAsync(() => configurationSortableList.Instance.GetParam(l => l.ItemsChanged).InvokeAsync(newConfiguration));
            
            _mockStateStorageService
                .Verify(mock => mock.SetTableColumnConfiguration(newConfiguration), Times.Once);
        }
    }
}