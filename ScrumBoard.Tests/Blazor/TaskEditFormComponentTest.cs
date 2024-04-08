using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Blazor.Modals;
using ScrumBoard.Tests.Util;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class TaskEditFormComponentTest : TestContext
    {
        private User _actingUser = new() { Id = 33, FirstName = "Jeff", LastName = "Jefferson" };

        private static User _taskCreator = new() { Id = 34, FirstName = "Thomas", LastName = "Creator" };

        private readonly Project _currentProject;

        private readonly UserStoryTaskTag _frontend = new() {Name = "Frontend", Id = 6};
        
        private readonly UserStoryTaskTag _magic    = new() {Id = 1, Name = "Magic"};
        private readonly UserStoryTaskTag _nonStory = new() {Id = 2, Name = "Non Story"};

        private UserStoryTask _editedTask;

        private readonly Sprint _currentSprint;

        private readonly UserStory _currentStory;
        
        private IRenderedComponent<TaskEditForm> _component;

        private readonly Mock<IUserStoryTaskTagRepository> _mockTaskTagRepository = new(MockBehavior.Strict);

        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new();

        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new();
        
        private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new();

        private readonly Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new();

        private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new();

        private readonly Mock<ISprintRepository> _mockSprintRepository = new();

        private readonly Mock<IJsInteropService> _mockJSInteropService = new();

        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);

        private Mock<Action> _onUpdate = new();

        private Mock<Action> _onClose = new();

        public TaskEditFormComponentTest()
        {
            _currentProject = new()
            {
                Id = 101,
            };
            _currentStory = new()
            {
                Project = _currentProject,
                ProjectId = _currentProject.Id,
                Stage = Stage.Todo,
                Name = "TEST",
            };

            _currentSprint = new Sprint()
            {
                Project = _currentProject,
                Stage = SprintStage.Started,
            };
            
            _editedTask = new UserStoryTask() { 
                UserStory = new UserStory() 
                { 
                    Project = _currentProject, 
                    Stage = Stage.Todo 
                }, 
                Stage = Stage.InProgress, 
                Creator = _taskCreator,
                Created = DateTime.Now,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>(),
                Tags = new List<UserStoryTaskTag>(),
            };
            
            _mockTaskTagRepository
                .Setup(mock => mock.GetAllAsync())
                .ReturnsAsync(new List<UserStoryTaskTag>() { _frontend });
            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetAssigneesAndReviewers(It.IsAny<UserStoryTask>()))
                .ReturnsAsync(new List<UserTaskAssociation>());
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.GetByUserStoryTaskAsync(It.IsAny<UserStoryTask>(), It.IsAny<Func<IQueryable<UserStoryTaskChangelogEntry>, IQueryable<UserStoryTaskChangelogEntry>>[]>()))
                .ReturnsAsync(new List<UserStoryTaskChangelogEntry>());
            _mockWorklogEntryService.
                Setup(mock => mock.GetWorklogEntriesForTaskAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<WorklogEntry>());
            _mockProjectRepository
                .Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(_currentProject);
            _mockUserStoryRepository
                .Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);
            _mockSprintRepository
                .Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Sprint>, IQueryable<Sprint>>[]>()))
                .ReturnsAsync(_currentSprint);
            
            Services.AddScoped(_ => _mockTaskTagRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);
            Services.AddScoped(_ => _mockJSInteropService.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockWorklogEntryService.Object);
            Services.AddScoped(_ => _mockSprintRepository.Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();

            Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);
        }

        private void SetupComponent(bool isNewTask, bool storyDone=false, bool hasSprint=false, Stage taskStage=Stage.Todo, bool isReadOnly = false)
        {
            _currentStory.Stage = Stage.Todo;
            if (storyDone)
            {
                _currentStory.Stage = Stage.Done;
                _editedTask = new UserStoryTask() { 
                    UserStory = _currentStory, 
                    Stage = Stage.InProgress,
                    Created = DateTime.Now, 
                    Creator = _taskCreator,
                    Estimate = TimeSpan.FromHours(1),
                    UserAssociations = new List<UserTaskAssociation>(),
                    Tags = new List<UserStoryTaskTag>(),
                };
            }
            _editedTask.Stage = taskStage;
            _editedTask.UserStory = _currentStory;
            _editedTask.UserStory.Tasks = new List<UserStoryTask>() { _editedTask };
            _editedTask.Id = isNewTask ? default : 42;
            if (hasSprint) {
                _editedTask.UserStory.StoryGroup = _currentSprint;
            }

            ProjectUserMembership association = new ProjectUserMembership()
            {
                Project = _currentProject, 
                ProjectId = _currentProject.Id, 
                User = _actingUser, 
                UserId = _actingUser.Id, 
                Role = ProjectRole.Developer,
            };
            List<ProjectUserMembership> associations = new List<ProjectUserMembership>() {association};
            _currentProject.MemberAssociations = associations;
            _actingUser.ProjectAssociations =  associations;

            _component = RenderComponent<TaskEditForm>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue("ProjectState", new ProjectState{IsReadOnly = isReadOnly})
                .Add(cut => cut.Task, _editedTask)
                .Add(cut => cut.OnUpdate, _onUpdate.Object)
                .Add(cut => cut.OnClose, _onClose.Object)
            );
        }

        [Fact]
        public void SetName_IsEmpty_ErrorMessageDisplayed()
        {
            SetupComponent(true);
            var input = _component.Find("#name-input");
            input.Input("");

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll("#name-validation-message").Any());

            var errorLabel = _component.Find("#name-validation-message");

            var expectedErrorMessage = typeof(UserStoryTaskForm).GetAttribute<RequiredAttribute>("Name").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("                 ")]
        public void SetDescription_IsEmpty_ErrorMessageDisplayed(string descriptionInput)
        {
            SetupComponent(true);
            var input = _component.Find("#description-input");
            input.Input(descriptionInput);

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll("#description-validation-message").Any());

            var errorLabel = _component.Find("#description-validation-message");

            var expectedErrorMessage = typeof(UserStoryTaskForm).GetAttribute<RequiredAttribute>("Description").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Description")]
        public void SetStringField_LongerThanMaximum_ErrorMessageDisplayed(string fieldName)
        {
            SetupComponent(true);
            var maxLengthAttribute = typeof(UserStoryTaskForm).GetAttribute<MaxLengthAttribute>(fieldName);

            var stringInput = _component.Find($"#{fieldName.ToLower()}-input");
            stringInput.Input(new String('a', maxLengthAttribute.Length + 1));

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#{fieldName.ToLower()}-validation-message").Any());

            var errorLabel = _component.Find($"#{fieldName.ToLower()}-validation-message");

            var expectedErrorMessage = maxLengthAttribute.ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Description")]
        public void SetStringField_OnlyContainsSpecialCharacters_ErrorMessageDisplayed(string fieldName)
        {
            SetupComponent(true);
            var stringInput = _component.Find($"#{fieldName.ToLower()}-input");
            stringInput.Input("*+-0");

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#{fieldName.ToLower()}-validation-message").Any());

            var errorLabel = _component.Find($"#{fieldName.ToLower()}-validation-message");

            var expectedErrorMessage = typeof(UserStoryTaskForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(fieldName).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetEstimate_InvalidDurationString_ErrorMessageDisplayed()
        {
            SetupComponent(true);
            var estimateInput = _component.Find($"#estimate-input");
            estimateInput.Change("seven");

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#estimate-validation-message").Any());

            var errorLabel = _component.Find($"#estimate-validation-message");

            var expectedErrorMessage = "Invalid duration format";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }
        
        [Fact]
        public void EditTask_UnsetComplexity_ErrorMessageDisplayed()
        {
            SetupComponent(false);

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#complexity-validation-message").Any());

            var errorLabel = _component.Find($"#complexity-validation-message");

            var expectedErrorMessage = "Complexity is required";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetEstimate_DurationTooSmall_ErrorMessageDisplayed()
        {
            SetupComponent(true);
            var estimateInput = _component.Find($"#estimate-input");
            estimateInput.Change($"-1s");

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#estimate-validation-message").Any());

            var errorLabel = _component.Find($"#estimate-validation-message");

            errorLabel.TextContent.Should().Be("Must be no less than 1 minute");
        }

        [Fact]
        public void SetEstimate_DurationTooLarge_ErrorMessageDisplayed()
        {
            var maximum = TimeSpan.FromDays(1);

            SetupComponent(true);
            var estimateInput = _component.Find($"#estimate-input");
            estimateInput.Change($"{(maximum + TimeSpan.FromSeconds(1)).TotalSeconds}s");

            _component.Find("#edit-task-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#estimate-validation-message").Any());

            var errorLabel = _component.Find($"#estimate-validation-message");

            errorLabel.TextContent.Should().Be("Must be no greater than 24 hours");
        }

        [Fact]
        public void NewTask_Unsaved_NoCloseButtonExists()
        {
            SetupComponent(true);
            _component.FindAll("#close-button").Should().BeEmpty();
        }

        [Fact]
        public void ExistingTask_Unsaved_CloseButtonExistsAndTriggersCloseCallback()
        {
            SetupComponent(false);
            _component.FindAll("#close-button").Should().HaveCount(1);

            _component.Find("#close-button").Click();
            _onClose.Verify(mock => mock(), Times.Once());
        }

        [Fact]
        public void NewTask_NotYetEditing_SaveAndCancelButtonsExist()
        {
            SetupComponent(true);
            _component.FindAll("#cancel-button").Should().HaveCount(1);
            _component.FindAll("#save-button").Should().HaveCount(1);
        }

        [Fact]
        public void ExistingTask_NotYetEditing_SaveAndCancelButtonsDoNotExist()
        {
            SetupComponent(false);
            _component.FindAll("#cancel-button").Should().BeEmpty();
            _component.FindAll("#save-button").Should().BeEmpty();
        }

        [Theory]
        [InlineData("name")]
        [InlineData("description")]
        public void ExistingTask_StringFieldEdited_SaveAndCancelButtonsExist(string field)
        {
            SetupComponent(false);

            _component.Find($"#{field}-input").Input("doesn't matter");

            _component.FindAll("#cancel-button").Should().HaveCount(1);
            _component.FindAll("#save-button").Should().HaveCount(1);
        }

        [Fact]
        public void ExistingTask_PriorityEdited_SaveAndCancelButtonsExist()
        {
            SetupComponent(false);

            var priority = Priority.Critical;
            _component.Find($"#priority-select-{priority}").Click();

            _component.FindAll("#cancel-button").Should().HaveCount(1);
            _component.FindAll("#save-button").Should().HaveCount(1);
        }
        
        [Fact]
        public void ExistingTask_ComplexityEdited_SaveAndCancelButtonsExist()
        {
            SetupComponent(false);

            const Complexity complexity = Complexity.High;
            _component.Find($"#complexity-select-{complexity}").Click();

            _component.FindAll("#cancel-button").Should().HaveCount(1);
            _component.FindAll("#save-button").Should().HaveCount(1);
        }

        [Fact]
        public void ExistingTask_EstimateEdited_SaveAndCancelButtonsExist()
        {
            SetupComponent(false);

            _component.Find("#estimate-input").Change("400m");

            _component.FindAll("#cancel-button").Should().HaveCount(1);
            _component.FindAll("#save-button").Should().HaveCount(1);
        }

        [Fact]
        public void SubmitForm_NewTaskWithValidFields_AddedWithDetailsCorrect()
        {
            SetupComponent(true);

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = TimeSpan.FromSeconds(400);
            var priority = Priority.Critical;
            var complexity = Complexity.Medium;

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find($"#complexity-select-{complexity}").Click();
            _component.Find($"#estimate-input").Change($"{estimate.TotalSeconds}s");
            _component.Find($"#tag-select-{_frontend.Id}").Click();

            _component.Find("#edit-task-form").Submit();

            var arg = new ArgumentCaptor<UserStoryTask>();
            _mockUserStoryTaskRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var task = arg.Value;

            task.Name.Should().Be(name);
            task.Description.Should().Be(description);
            task.Estimate.Should().Be(estimate);
            task.Priority.Should().Be(priority);
            task.Complexity.Should().Be(complexity);

            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateAsync(It.IsAny<UserStoryTask>()), Times.Never());
        }

        [Fact]
        public void SubmitForm_ExistingTaskWithValidFields_UpdatedWithDetailsCorrect()
        {
            SetupComponent(false);

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = TimeSpan.FromSeconds(400);
            var priority = Priority.Critical;
            var complexity = Complexity.High;

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find($"#complexity-select-{complexity}").Click();
            _component.Find($"#estimate-input").Change($"{estimate.TotalSeconds}s");
            _component.Find($"#tag-select-{_frontend.Id}").Click();

            _component.Find("#edit-task-form").Submit();

            var taskArg = new ArgumentCaptor<UserStoryTask>();
            var associationArg = new ArgumentCaptor<List<UserTaskAssociation>>();
            var tagsArg = new ArgumentCaptor<List<UserStoryTaskTag>>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateTaskAssociationsAndTags(taskArg.Capture(), associationArg.Capture(), tagsArg.Capture()), Times.Once());
            var task = taskArg.Value;

            task.Name.Should().Be(name);
            task.Description.Should().Be(description);
            task.Estimate.Should().Be(estimate);
            task.Priority.Should().Be(priority);
            task.Complexity.Should().Be(complexity);

            _mockUserStoryTaskRepository.Verify(mock => mock.AddAsync(It.IsAny<UserStoryTask>()), Times.Never());
            _component.FindAll("#task-concurrency-error").Should().BeEmpty();
        }

        [Fact]
        public void SubmitForm_NewTaskWithValidFields_CreatedDateAndCreatorCorrect()
        {
            SetupComponent(true);
            
            var name = "Test task name";
            var description = "Test task description";

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);
            _component.Find($"#tag-select-{_frontend.Id}").Click();
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();


            var before = DateTime.Now;
            _component.Find("#edit-task-form").Submit();
            var after = DateTime.Now;

            var arg = new ArgumentCaptor<UserStoryTask>();
            _mockUserStoryTaskRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var task = arg.Value;

            task.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            task.CreatorId.Should().Be(_actingUser.Id);
        }

        [Fact]
        public void SubmitForm_ExitingTaskWithValidFieldsButNoTag_TaskNotUpdated()
        {
            var created = new DateTime(2012, 12, 21);
            var creator = new User() { FirstName = "Tim", LastName = "Tam", Id = 77 };

            _editedTask.Created = created;
            _editedTask.Creator = creator;

            SetupComponent(false);
            
            var name = "Test task Name";
            var description = "Test task description";

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);            
            _component.Find("#edit-task-form").Submit();

            var arg = new ArgumentCaptor<UserStoryTask>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Never());         
        }

        [Fact]
        public void SubmitForm_ExitingTaskWithValidFieldsButNoEstimate_TaskNotUpdated()
        {
            var created = new DateTime(2012, 12, 21);
            var creator = new User() { FirstName = "Tim", LastName = "Tam", Id = 77 };

            _editedTask.Created = created;
            _editedTask.Creator = creator;

            bool isNewTask = false;
            bool storyDone = false;
            bool hasSprint = true;
            SetupComponent(isNewTask, storyDone, hasSprint);
            
            var name = "Test task Name";
            var description = "Test task description";

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);            
            _component.Find("#edit-task-form").Submit();

            var arg = new ArgumentCaptor<UserStoryTask>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Never());         
        }

        [Fact]
        public void SubmitForm_ExitingTaskWithValidFields_CreatedDateAndCreatorUnchanged()
        {
            var created = new DateTime(2012, 12, 21);
            var creator = new User() { FirstName = "Tim", LastName = "Tam", Id = 77 };

            _editedTask.Created = created;
            _editedTask.Creator = creator;
            _editedTask.CreatorId = creator.Id;

            SetupComponent(false);
            
            var name = "Test task Name";
            var description = "Test task description";

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);
            _component.Find($"#tag-select-{_frontend.Id}").Click();
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();

            var taskArg = new ArgumentCaptor<UserStoryTask>();
            var associationArg = new ArgumentCaptor<List<UserTaskAssociation>>();
            var tagsArg = new ArgumentCaptor<List<UserStoryTaskTag>>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateTaskAssociationsAndTags(taskArg.Capture(), associationArg.Capture(), tagsArg.Capture()), Times.Once());
            var task = taskArg.Value;

            task.Created.Should().Be(created);
            task.CreatorId.Should().Be(creator.Id);
        }

        [Theory]
        [InlineData("#nav-task-changelog-tab")]
        [InlineData("#nav-task-worklog-tab")]
        [InlineData("#nav-task-changelog")]
        [InlineData("#nav-task-worklog")]
        public void FindTaskDetails_IsExistingTask_DetailsViewable(string selector)
        {
            SetupComponent(false);

            _component.FindAll(selector).Should().ContainSingle();
        }

        [Theory]
        [InlineData("#nav-task-changelog")]
        [InlineData("#nav-task-changelog-tab")]
        [InlineData("#nav-task-worklog-tab")]
        [InlineData("#nav-task-worklog")]
        public void FindTaskDetails_IsNewTask_DetailsNotViewable(string selector)
        {
            SetupComponent(true);

            _component.FindAll(selector).Should().BeEmpty();
        }

        [Fact]
        public void ChangeTaskStage_TaskStageIsUpdated()
        {
            SetupComponent(false, hasSprint: true);
            Stage newStage = Stage.UnderReview;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName);
            _component.Find("#description-input").Input(newDescription);  
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();     
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();

            var taskArg = new ArgumentCaptor<UserStoryTask>();
            var associationArg = new ArgumentCaptor<List<UserTaskAssociation>>();
            var tagsArg = new ArgumentCaptor<List<UserStoryTaskTag>>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateTaskAssociationsAndTags(taskArg.Capture(), associationArg.Capture(), tagsArg.Capture()), Times.Once());
            var task = taskArg.Value;
            task.Name.Should().Be(newName);
            task.Stage.Should().Be(newStage);
        }
        
        [Fact]
        public void ChangeTaskComplexity_TaskComplexityIsUpdated()
        {
            SetupComponent(false);
            const Complexity newComplexity = Complexity.High;
            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName);
            _component.Find("#description-input").Input(newDescription);  
            _component.Find($"#complexity-select-{newComplexity}").Click();
            _component.Find("#edit-task-form").Submit();

            var taskArg = new ArgumentCaptor<UserStoryTask>();
            var associationArg = new ArgumentCaptor<List<UserTaskAssociation>>();
            var tagsArg = new ArgumentCaptor<List<UserStoryTaskTag>>();
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateTaskAssociationsAndTags(taskArg.Capture(), associationArg.Capture(), tagsArg.Capture()), Times.Once());
            var task = taskArg.Value;
            task.Name.Should().Be(newName);
            task.Description.Should().Be(newDescription);
            task.Complexity.Should().Be(newComplexity);
        }
        
        [Theory]
        [EnumData(typeof(Stage))]
        public void ChangeTaskStage_TaskWithoutSprint_TaskStageChangerDisabled(Stage stage)
        {
            SetupComponent(false, hasSprint: false);
            
            _component.FindAll($"#status-select-{stage}").Should().BeEmpty();
        }
        
        [Theory]
        [EnumData(typeof(Stage))]
        public void ChangeTaskStage_SprintNotStarted_TaskStageChangerDisabled(Stage stage)
        {
            _currentSprint.Stage = SprintStage.Created;
            SetupComponent(false, hasSprint: true);
            
            _component.FindAll($"#status-select-{stage}").Should().BeEmpty();
        }

        [Fact]
        public void ChangeTaskStage_AllStoryTasksDone_StoryUpdated()
        {
            SetupComponent(false, hasSprint: true);
            _currentStory.Tasks.Add(new UserStoryTask() {
                Name = "A test task",            
                Stage = Stage.Done, 
                Creator = _taskCreator,
                Created = DateTime.Now,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>() 
            });
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);
            Stage newStage = Stage.Done;

            var newName = "New random name";
            var newDescription = "NewDescription";

            _component.Find("#name-input").Input(newName);
            _component.Find("#description-input").Input(newDescription);
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();      
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Done);
        }

        [Fact]
        public void ChangeTaskStage_AllStoryTasksDeferred_StoryUpdated()
        {
            SetupComponent(false, hasSprint: true);
            _currentStory.Tasks.Add(new UserStoryTask() {
                Name = "A test task",            
                Stage = Stage.Deferred, 
                Creator = _taskCreator,
                Created = DateTime.Now,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>() 
            });
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);
           
            Stage newStage = Stage.Deferred;            

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();   
            _component.Find($"#tag-select-{_frontend.Id}").Click();     
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();                     

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Deferred);
        }

        [Fact]
        public void ChangeTaskStage_StoryDone_MakeTaskNotDone_ConfirmStoryInProgressModalShown()
        {
            var storyDone = true;
            var hasSprint = true;
            SetupComponent(false, storyDone, hasSprint);
            Stage newStage = Stage.InProgress;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();    
            _component.Find($"#tag-select-{_frontend.Id}").Click();    
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            _component.FindAll(".modal.show").Should().ContainSingle();   
            _component.FindAll("#tasks-not-done-modal").Should().ContainSingle(); 
        }

        [Fact]
        public void ChangeTaskStage_StoryDone_MakeTaskNotDone_ConfirmChange_StoryUpdated()
        {
            var storyDone = true;
            var hasSprint = true;
            SetupComponent(false, storyDone, hasSprint);
            Stage newStage = Stage.InProgress;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();   
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            _component.FindAll(".modal.show").Should().ContainSingle();   
            _component.FindAll("#tasks-not-done-modal").Should().ContainSingle(); 

            _component.Find("#confirm-story-update").Click();

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.InProgress);
        }

        [Fact]
        public void ChangeTaskStage_StoryDone_TaskDone_MakeTaskDeferred_StoryUpdated()
        {
            var storyDone = true;
            var hasSprint  = true;
            SetupComponent(false, storyDone, hasSprint);            
            _editedTask.Stage = Stage.Done;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            Stage newStage = Stage.Deferred;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();      
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Deferred);
        }

        [Fact]
        public void ChangeTaskStage_StoryDone_TaskDone_MakeTaskDeferred_AnotherTaskDone_ConfirmUpdate_StoryUpdated()
        {
            var storyDone = true;
            var hasSprint  = true;
            SetupComponent(false, storyDone, hasSprint);         
            _currentStory.Tasks.Add(new UserStoryTask() { 
                UserStory = _currentStory, 
                Stage = Stage.Done,
                Created = DateTime.Now, 
                Creator = _taskCreator,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>(),
                Tags = new List<UserStoryTaskTag>(),
            });
            _editedTask.Stage = Stage.Done;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            Stage newStage = Stage.Deferred;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();       
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();     

            _component.FindAll(".modal.show").Should().ContainSingle();   
            _component.FindAll("#tasks-done-modal").Should().ContainSingle(); 

            _component.Find("#change-story-to-deferred").Click();       

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Deferred);
        }

        [Fact]
        public void ChangeTaskStage_StoryDeferred_TaskDeferred_MakeTaskDone_AnotherTaskDeferred_ConfirmUpdate_StoryUpdated()
        {
            var storyDone = true;
            var hasSprint  = true;
            SetupComponent(false, storyDone, hasSprint);         
            _currentStory.Tasks.Add(new UserStoryTask() { 
                UserStory = _currentStory, 
                Stage = Stage.Deferred,
                Created = DateTime.Now, 
                Creator = _taskCreator,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>(),
                Tags = new List<UserStoryTaskTag>(),
            });
            _currentStory.Stage = Stage.Deferred;
            _editedTask.Stage = Stage.Deferred;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            Stage newStage = Stage.Done;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();    
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();     

            _component.FindAll(".modal.show").Should().ContainSingle();   
            _component.FindAll("#tasks-done-modal").Should().ContainSingle(); 

            _component.Find("#change-story-to-done").Click();       

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Done);
        }

        [Fact]
        public void ChangeTaskStage_StoryDeferred_TaskDeferred_MakeTaskDone_CancelUpdate_TaskAndStoryStagesNotUpdated()
        {
            var storyDone = true;
            var hasSprint  = true;
            SetupComponent(false, storyDone, hasSprint);         
            _currentStory.Tasks.Add(new UserStoryTask() { 
                UserStory = _currentStory, 
                Stage = Stage.Deferred,
                Created = DateTime.Now, 
                Creator = _taskCreator,
                Estimate = TimeSpan.FromHours(1),
                UserAssociations = new List<UserTaskAssociation>(),
                Tags = new List<UserStoryTaskTag>(),
            });
            _currentStory.Stage = Stage.Deferred;
            _editedTask.Stage = Stage.Deferred;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            Stage newStage = Stage.Done;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();  
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();     

            _component.FindAll(".modal.show").Should().ContainSingle();   
            _component.FindAll("#tasks-done-modal").Should().ContainSingle(); 

            _component.Find("#close-modal").Click();      

            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(It.IsAny<UserStory>()), Times.Never());
            
            _mockUserStoryTaskRepository.Verify(mock => mock.UpdateTaskAssociationsAndTags(
                It.IsAny<UserStoryTask>(), 
                It.IsAny<List<UserTaskAssociation>>(), 
                It.IsAny<List<UserStoryTaskTag>>()), 
                Times.Never());
        }

        [Fact]
        public void ChangeTaskStage_StoryDeferred_TaskDeferred_MakeTaskDone_StoryUpdated()
        {
            bool storyDone = true;
            var hasSprint = true;
            SetupComponent(false, storyDone, hasSprint);      
            _currentStory.Stage = Stage.Deferred;
            _editedTask.Stage = Stage.Deferred;
            _mockUserStoryRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<UserStory>, IQueryable<UserStory>>[]>()))
                .ReturnsAsync(_currentStory);

            Stage newStage = Stage.Done;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription); 
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();     
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.Done);
        }

        [Fact]
        public void AddTaskAssignee_IsExistingTask_CannotAlsoAddThemAsReviewer()
        {
            SetupComponent(false);

            _component.Find($"#assignee-user-select-{_actingUser.Id}").Click();

            _component.FindAll($"#reviewer-user-select-{_actingUser.Id}").Should().BeEmpty();
        }

        [Fact]
        public void AddTaskReviewer_IsExistingTask_CannotAlsoAddThemAsAssignee()
        {
            SetupComponent(false);

            _component.Find($"#reviewer-user-select-{_actingUser.Id}").Click();

            _component.FindAll($"#assignee-user-select-{_actingUser.Id}").Should().BeEmpty();

        }

        [Fact]
        public void ChangeTaskStage_FromTodoToInProgess_AllOtherTasksInTodo_StoryInProgress()
        {
            var storyDone = false;
            var isNewTask = false;
            var hasSprint = true;
            SetupComponent(isNewTask, storyDone, hasSprint);
            Stage newStage = Stage.InProgress;

            var newName = "New random name";
            var newDescription = "New description";

            _component.Find("#name-input").Input(newName); 
            _component.Find("#description-input").Input(newDescription);
            _component.Find($"#status-select-{newStage}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();     
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find("#edit-task-form").Submit();            

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;
            story.Stage.Should().Be(Stage.InProgress);
        }

        [Fact]
        public void AddTaskWorklog_StoryInBacklog_ErrorMessageDisplayed()
        {
            Sprint emptySprint = null;
            _mockSprintRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Sprint>, IQueryable<Sprint>>[]>()))
                .ReturnsAsync(emptySprint);
            SetupComponent(false);            

            _component.Find($"#add-worklog-entry-button").HasAttribute("disabled").Should().BeTrue();

            var errorMessages = _component.FindAll($"#add-worklog-error-message");
            errorMessages.Should().HaveCount(1);
            errorMessages.First().TextContent.Trim().Should().Be("Story in backlog");
        }

        [Fact]
        public void AddTaskWorklog_SprintNotStarted_ErrorMessageDisplayed()
        {
            _currentSprint.Stage = SprintStage.Created;
            bool isNewTask = false;
            bool storyDone = false;
            bool hasSprint = true;
            SetupComponent(isNewTask, storyDone, hasSprint);

            _component.Find($"#add-worklog-entry-button").HasAttribute("disabled").Should().BeTrue();

            var errorMessages = _component.FindAll($"#add-worklog-error-message");
            errorMessages.Should().HaveCount(1);
            errorMessages.First().TextContent.Trim().Should().Be("Sprint not started");
        }

        [Fact]
        public void AddTaskWorklog_SprintStarted_ButtonEnabled()
        {
            bool isNewTask = false;
            bool storyDone = false;
            bool hasSprint = true;
            _currentSprint.Stage = SprintStage.Started;            
            SetupComponent(isNewTask, storyDone, hasSprint, Stage.InProgress);         

            _component.Find($"#add-worklog-entry-button").HasAttribute("disabled").Should().BeFalse();

            _component.FindAll($"#add-worklog-error-message").Should().BeEmpty();
        }

        [Fact]
        public void AddTaskWorklog_TaskInTodo_ButtonDisabled()
        {
            bool isNewTask = false;
            bool storyDone = false;
            bool hasSprint = true;
            _currentSprint.Stage = SprintStage.Started;            
            SetupComponent(isNewTask, storyDone, hasSprint, Stage.Todo);          

            _component.Find($"#add-worklog-entry-button").HasAttribute("disabled").Should().BeTrue();

            var errorMessages = _component.FindAll($"#add-worklog-error-message");
            errorMessages.Should().HaveCount(1);
            errorMessages.First().TextContent.Trim().Should().Be("Task is inTo Do");
        }

        [Fact]
        public void AddTaskWorklog_TaskInDeferred_ButtonDisabled()
        {
            bool isNewTask = false;
            bool storyDone = false;
            bool hasSprint = true;
            _currentSprint.Stage = SprintStage.Started;            
            SetupComponent(isNewTask, storyDone, hasSprint, Stage.Deferred);            

            _component.Find($"#add-worklog-entry-button").HasAttribute("disabled").Should().BeTrue();

            var errorMessages = _component.FindAll($"#add-worklog-error-message");
            errorMessages.Should().HaveCount(1);
            errorMessages.First().TextContent.Trim().Should().Be("Task is inDeferred");
        }

        [Fact]
        public void SubmitForm_AnotherUserEdited_ErrorMessageDisplayed()
        {
            SetupComponent(false);

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = TimeSpan.FromSeconds(400);
            var priority = Priority.Critical;

            _component.Find("#name-input").Input(name);
            _component.Find("#description-input").Input(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find("#estimate-input").Change($"{estimate.TotalSeconds}s");
            _component.Find($"#complexity-select-{Complexity.Medium}").Click();
            _component.Find($"#tag-select-{_frontend.Id}").Click();

            // Mock a concurrency exception
            _mockUserStoryTaskRepository
                .Setup(mock =>
                    mock.UpdateTaskAssociationsAndTags(It.IsAny<UserStoryTask>(), It.IsAny<List<UserTaskAssociation>>(), It.IsAny<List<UserStoryTaskTag>>()))
                .Throws(new DbUpdateConcurrencyException("Concurrency Error"));

            _component.Find("#edit-task-form").Submit();

            _component.FindAll("#task-concurrency-error").Should().ContainSingle();

        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_IsReadOnly_InputMembersDisabledBasedOnReadOnly(bool isReadOnly)
        {
            SetupComponent(false, isReadOnly: isReadOnly);

            var inputMembers = _component.FindComponents<InputMember>();
            inputMembers.Should().HaveCount(2);
            inputMembers.Should().OnlyContain(input => input.Instance.Disabled == isReadOnly);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_IsReadOnly_EagerInputTextAreaDisabledBasedOnReadOnly(bool isReadOnly)
        {
            SetupComponent(false, isReadOnly: isReadOnly);

            var inputTexts = _component.FindComponents<EagerInputTextArea>();
            inputTexts.Should().HaveCount(2);
            inputTexts.Should().OnlyContain(input => input.Instance.Disabled == isReadOnly);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_IsReadOnly_InputDurationDisabledBasedOnReadOnly(bool isReadOnly)
        {
            SetupComponent(false, isReadOnly: isReadOnly);

            var inputDurations = _component.FindComponents<InputDuration>();
            inputDurations.Should().HaveCount(1);
            inputDurations.Should().OnlyContain(input => input.Instance.Disabled == isReadOnly);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_IsReadOnly_InputPriorityDisabledBasedOnReadOnly(bool isReadOnly)
        {
            SetupComponent(false, isReadOnly: isReadOnly);

            var inputPriorities = _component.FindComponents<InputPriority>();
            inputPriorities.Should().HaveCount(1);
            inputPriorities.Should().OnlyContain(input => input.Instance.Disabled == isReadOnly);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_IsReadOnly_InputComplexityDisabledBasedOnReadOnly(bool isReadOnly)
        {
            SetupComponent(false, isReadOnly: isReadOnly);

            var inputComplexities = _component.FindComponents<InputComplexity>();
            inputComplexities.Should().HaveCount(1);
            inputComplexities.Should().OnlyContain(input => input.Instance.Disabled == isReadOnly);
        }
        
        [Fact]
        public void Rendered_SprintInReview_FieldsReadOnly()
        {
            _currentSprint.Stage = SprintStage.InReview;
            SetupComponent(false, hasSprint: true);

            var inputTexts = _component.FindComponents<EagerInputTextArea>();
            inputTexts.Should().HaveCount(2).And.OnlyContain(input => input.Instance.Disabled);
            
            var inputPriorities = _component.FindComponents<InputPriority>();
            inputPriorities.Should().HaveCount(1).And.OnlyContain(input => input.Instance.Disabled);
            
            var inputDurations = _component.FindComponents<InputDuration>();
            inputDurations.Should().HaveCount(1).And.OnlyContain(input => input.Instance.Disabled);
            
            var inputMembers = _component.FindComponents<InputMember>();
            inputMembers.Should().HaveCount(2).And.OnlyContain(input => input.Instance.Disabled);
        }

        [Fact]
        public void TaskStatusChanger_TaskDone_HasCorrectColourCSS()
        {
            var storyDone = false;
            var isNewTask = false;
            var hasSprint = true;
            SetupComponent(isNewTask, storyDone, hasSprint);
            Stage newStage = Stage.Done;
                      
            _component.Find($"#status-select-{newStage}").Click();                   

            _component.Find("#task-status-button").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Done].GetCss());

            _component.Find("#status-select-color-Todo").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Todo].GetCss());
            _component.Find("#status-select-color-InProgress").ClassName.Should().Contain(StageDetails.StageStyles[Stage.InProgress].GetCss());
            _component.Find("#status-select-color-UnderReview").ClassName.Should().Contain(StageDetails.StageStyles[Stage.UnderReview].GetCss());
            _component.Find("#status-select-color-Done").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Done].GetCss());
            _component.Find("#status-select-color-Deferred").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Deferred].GetCss());  
        }

        [Fact]
        public void TaskStatusChanger_TaskTodo_HasCorrectColourCSS()
        {
            var storyDone = false;
            var isNewTask = false;
            var hasSprint = true;
            SetupComponent(isNewTask, storyDone, hasSprint);          
               
            _component.Find("#task-status-button").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Todo].GetCss());

            _component.Find("#status-select-color-Todo").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Todo].GetCss());
            _component.Find("#status-select-color-InProgress").ClassName.Should().Contain(StageDetails.StageStyles[Stage.InProgress].GetCss());
            _component.Find("#status-select-color-UnderReview").ClassName.Should().Contain(StageDetails.StageStyles[Stage.UnderReview].GetCss());
            _component.Find("#status-select-color-Done").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Done].GetCss());
            _component.Find("#status-select-color-Deferred").ClassName.Should().Contain(StageDetails.StageStyles[Stage.Deferred].GetCss()); 
        }
    }
}
