using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using System;
using System.Linq;
using ScrumBoard.Tests.Util;
using Xunit;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Validators;
using ScrumBoard.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrumBoard.Repositories;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Tests.Blazor.Modals;

namespace ScrumBoard.Tests.Blazor
{
    public class EditSprintComponentTest : TestContext
    {
        private readonly User _actingUser = new User() { Id = 33, FirstName = "Jeff", LastName = "Jefferson" };

        private Project _currentProject;

        private Sprint _currentSprint;      

        private readonly Mock<ISprintRepository> _mockSprintRepository = new();

        private readonly Mock<ILogger<EditSprint>> _mockLogger = new();

        private readonly Mock<ISortableService<UserStory>> _mockSortableService = new();

        private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new();

        private readonly Mock<IJsInteropService> _mockJSInteropService = new();

        private readonly Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new();

        private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new();

        private readonly Mock<IProjectRepository> _mockProjectRepository = new();

        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new();
        
        private readonly Mock<IUserStoryTaskService> _mockUserStoryTaskService = new(MockBehavior.Strict);
        
        private readonly Mock<IUserStoryService> _mockUserStoryService = new(MockBehavior.Strict);

        private readonly Mock<RemoveUserStoryModal> _mockRemoveUserStoryModal = new();

        private IRenderedComponent<EditSprint> _component;

        private Mock<Action> _onSave = new();

        private Mock<Action> _onCancel = new();

        private List<UserStory> _currentBacklog = new();

        public EditSprintComponentTest()
        {
            Services.AddScoped(_ => _mockSprintRepository.Object);     
            Services.AddScoped(_ => _mockLogger.Object);           
            Services.AddScoped(_ => _mockSortableService.Object);
            Services.AddScoped(_ => _mockSprintChangelogRepository.Object);
            Services.AddScoped(_ => _mockJSInteropService.Object);
            Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskService.Object);
            Services.AddScoped(_ => _mockUserStoryService.Object);

            ComponentFactories.AddMockComponent(_mockRemoveUserStoryModal);
        }

        /// <summary>
        /// Sets up the strict mocks for saving updates to occur
        /// </summary>
        private void SetupForEdit()
        {
            _mockUserStoryService
                .Setup(mock => mock.UpdateStages(_actingUser, It.IsAny<IEnumerable<UserStory>>(), It.IsAny<Func<Stage, Stage>>()))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskService
                .Setup(mock => mock.UpdateStages(_actingUser, It.IsAny<IEnumerable<UserStoryTask>>(), It.IsAny<Func<Stage, Stage>>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupComponent(ProjectRole role = ProjectRole.Leader, bool isExistingSprint = false)
        {           
            if (isExistingSprint) {
                _currentProject = new Project() { Id = 1 };
                _currentSprint = new Sprint() { 
                    Project = _currentProject, 
                    Id = 3, 
                    Stage = SprintStage.Started, 
                    Name = "Starting Sprint name",
                    StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
                    Stories = new List<UserStory>()
                    {
                        new()
                        {
                            Id = 4,
                            Estimate = 10, 
                            Tasks = new List<UserStoryTask>() { new() { Estimate = TimeSpan.FromSeconds(10) } }
                        }
                    }
                }; 
            } else {
                _currentProject = new Project() { Id = 1 };
                _currentSprint = new Sprint() { Project = _currentProject };  
            }
            _mockProjectRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>())).ReturnsAsync(_currentProject);
            
            _component = RenderComponent<EditSprint>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue("ProjectState", new ProjectState() { IsReadOnly = false, ProjectId = _currentProject.Id, ProjectRole = role})
                .Add(cut => cut.Sprint, _currentSprint)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );
        }

        [Fact]
        public void SetName_IsEmpty_ErrorMessageDisplayed()
        {
            SetupComponent();
            var input = _component.Find("#name-input");
            input.Change("");

            _component.Find("#create-sprint-form").Submit();
            _component.WaitForState(() => _component.FindAll("#name-validation-message").Any());

            var errorLabel = _component.Find("#name-validation-message");

            var expectedErrorMessage = typeof(SprintForm).GetAttribute<RequiredAttribute>("Name").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]       
        public void SetName_LongerThanMaximum_ErrorMessageDisplayed()
        {
            SetupComponent();
            var maxLengthAttribute = typeof(SprintForm).GetAttribute<MaxLengthAttribute>("Name");

            var stringInput = _component.Find($"#name-input");
            stringInput.Change(new String('a', maxLengthAttribute.Length + 1));

            _component.Find("#create-sprint-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#name-validation-message").Any());

            var errorLabel = _component.Find($"#name-validation-message");

            var expectedErrorMessage = maxLengthAttribute.ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("$")]
        [InlineData("@@@@@@@@@@")]
        [InlineData("!@#$%^&*()")]    
        public void SetName_OnlySpecialCharacters_ErrorMessageDisplayed(string input)
        {
            SetupComponent();
            var stringInput = _component.Find($"#name-input");
            stringInput.Change(input);

            _component.Find("#create-sprint-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#name-validation-message").Any());

            var errorLabel = _component.Find($"#name-validation-message");

            var expectedErrorMessage = typeof(SprintForm).GetAttribute<NotEntirelySpecialCharacters>("Name").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SubmitCreateForm_WithValidFields_NewSprintSaved() 
        {
            SetupComponent();
            var name = "Test Sprint Name";           
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(20);

            _component.Find("#name-input").Change(name);         
            _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));           
        
            _component.Find("#create-sprint-form").Submit();    

            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
        }

        [Fact]
        public void SubmitCreateForm_WithValidFields_SprintDetailsCorrect() 
        {
            SetupComponent();
            var name = "Test Sprint Name";           
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(20);

            _component.Find("#name-input").Change(name);           
            _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));          

            var before = DateTime.Now;
            _component.Find("#create-sprint-form").Submit();
            var after = DateTime.Now;

            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var sprint = arg.Value;   

            sprint.Name.Should().Be(name);           
            sprint.StartDate.Should().Be(startDate);
            sprint.EndDate.Should().Be(endDate);

            sprint.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void SubmitEditForm_WithValidFields_SprintUpdated() 
        {
            SetupComponent(ProjectRole.Leader, true);
            
            var name = "New Sprint Name";           
            var endDate = _currentSprint.StartDate.AddDays(20);

            _component.Find("#name-input").Change(name);      
            _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

            SetupForEdit();
        
            _component.Find("#create-sprint-form").Submit();    

            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var sprint = arg.Value;

            sprint.Name.Should().Be(name);           
            sprint.EndDate.Should().Be(endDate);
            _component.FindAll("#sprint-concurrency-error").Should().BeEmpty();
            
            _mockRemoveUserStoryModal
                .Verify(mock => mock.Show(It.IsAny<Sprint>(), It.IsAny<ICollection<UserStory>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void SubmitStartDate_SprintHasStarted_SprintNotUpdated() {
            SetupComponent(ProjectRole.Leader, true);
            var startDate = _currentSprint.StartDate.AddDays(1);
            _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));

            var arg = new ArgumentCaptor<Sprint>();
            _mockSprintRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Never());
        }

        [Fact]
        public void CreateSprint_CancelSprintButtonNotVisible() {
            SetupComponent(ProjectRole.Leader, true);
            _component.FindAll("#cancel-sprint-button").Should().BeEmpty();            
        }

        [Fact]
        public void SubmitEditForm_AnotherUserEdited_ErrorMessageDisplayed() 
        {
            SetupComponent(ProjectRole.Leader, true);
            
            var name = "New Sprint Name";           
            var endDate = _currentSprint.StartDate.AddDays(20);

            _component.Find("#name-input").Change(name);      
            _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));    

            // Mock a concurrency exception
            _mockSprintRepository
                .Setup(mock =>
                    mock.UpdateAsync(It.IsAny<Sprint>()))
                .Throws(new DbUpdateConcurrencyException("Concurrency Error"));       
        
            _component.Find("#create-sprint-form").Submit();    

            _component.FindAll("#sprint-concurrency-error").Should().ContainSingle();
        }

        [Fact]
        public void SubmitEditForm_WithRemovedStories_StoriesAndTasksInTodo()
        {
            SetupComponent(ProjectRole.Leader, true);
            var initialStories = _component.Instance.Model.Stories.ToList();
            var initialTasks = initialStories.SelectMany(story => story.Tasks).ToList();
            _component.Instance.Model.Stories.Clear();

            SetupForEdit();

            _component.Find("#create-sprint-form").Submit();

            var stageMappingCaptor = new ArgumentCaptor<Func<Stage, Stage>>();
            var storyCaptor = new ArgumentCaptor<IEnumerable<UserStory>>();
            _mockUserStoryService
                .Verify(mock => mock.UpdateStages(_actingUser, storyCaptor.Capture(), stageMappingCaptor.Capture()),
                    Times.Once);
            storyCaptor.Value.Should().BeEquivalentTo(initialStories);

            var taskCaptor = new ArgumentCaptor<IEnumerable<UserStoryTask>>();
            _mockUserStoryTaskService
                .Verify(mock => mock.UpdateStages(_actingUser, taskCaptor.Capture(), stageMappingCaptor.Capture()),
                    Times.Once);
            taskCaptor.Value.Should().BeEquivalentTo(initialTasks);

            foreach (var stageMapping in stageMappingCaptor.Values)
                Enum.GetValues<Stage>().Select(stageMapping).Should().AllBeEquivalentTo(Stage.Todo);
        }

        [Fact]
        public void SubmitEditForm_WithRemovedStoriesAsDeveloper_RemoveUserStoryModalShownWithCannotRemove()
        {
            SetupComponent(ProjectRole.Developer, true);
            
            var initialStories = _component.Instance.Model.Stories.ToList();
            _component.Instance.Model.Stories.Clear();
        
            SetupForEdit();

            _component.Find("#create-sprint-form").Submit();
            
            _mockRemoveUserStoryModal
                .Verify(mock => mock.Show(_currentSprint, initialStories, false), Times.Once);
        }

        [Fact]
        public void SubmitEditForm_WithRemovedStoriesAsLeader_RemoveUserStoryModalShownCanRemove()
        {
            SetupComponent(ProjectRole.Leader, true);
            
            var initialStories = _component.Instance.Model.Stories.ToList();
            _component.Instance.Model.Stories.Clear();
        
            SetupForEdit();

            _component.Find("#create-sprint-form").Submit();
            _mockRemoveUserStoryModal
                .Verify(mock => mock.Show(_currentSprint, initialStories, true), Times.Once);
        }
        
        [Fact]
        public void SubmitEditForm_WithRemovedStoriesRemoveUserStoryModalCancelled_NoChangesMade()
        {
            SetupComponent(ProjectRole.Leader, true);
            
            _component.Instance.Model.Stories.Clear();
            _mockRemoveUserStoryModal
                .Setup(mock => mock.Show(_currentSprint, It.IsAny<ICollection<UserStory>>(), true))
                .ReturnsAsync(true);
            
            _component.Find("#create-sprint-form").Submit();
            _mockSprintRepository.Verify(mock => mock.UpdateAsync(It.IsAny<Sprint>()), Times.Never());
        }
    }
}