using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Repositories;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Shared.SprintBoard;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class SprintBoardTaskCardComponentTest : TestContext
    {
        private User _actingUser = new User() { Id = 101, FirstName = "John", LastName = "Smith" };

        private User _assigneeOne;

        private User _assigneeTwo;

        private User _reviewerOne;

        private User _reviewerTwo;

        private UserStoryTask _task;

        private UserStory _userStory;

        private readonly Mock<IUserStoryTaskTagRepository> _mockTaskTagRepository = new(MockBehavior.Strict);
        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskChangelogRepository> _mockUserStoryTaskChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new(MockBehavior.Strict);

        private Mock<Action> _onMembersChanged = new();

        private IRenderedComponent<SprintBoardTask> _component;

        private readonly UserStoryTaskTag _magic = new UserStoryTaskTag() {Name = "Magic"};
        private readonly UserStoryTaskTag _break = new UserStoryTaskTag() {Name = "Break"};

        public SprintBoardTaskCardComponentTest()
        {
            _mockTaskTagRepository
                .Setup(mock => mock.GetAllAsync())
                .ReturnsAsync(new List<UserStoryTaskTag>() {_magic, _break});

            
            _assigneeOne = new User() { Id = 102, FirstName = "John", LastName = "Doe" };
            _assigneeTwo = new User() { Id = 103, FirstName = "Jim", LastName = "Jones" };
            _reviewerOne = new User() { Id = 104, FirstName = "Sally", LastName = "Jones" };
            _reviewerTwo = new User() { Id = 105, FirstName = "Sarah", LastName = "OShaye" };
            _userStory = new UserStory() { Project = new Project() };
            _task = new UserStoryTask() {
                Id = 101, 
                Name = "This is a test task", 
                Created = DateTime.Now, 
                Creator = new User() { 
                    Id = 101, 
                    FirstName = "John", 
                    LastName = "Smith"
                }, 
                UserStory = _userStory, 
                Priority = Priority.Normal, 
                Tags = new List<UserStoryTaskTag>() { _magic, _break },
            };
            _assigneeOne.AssignTask(_task);
            _assigneeTwo.AssignTask(_task);
            _reviewerOne.ReviewTask(_task);
            _reviewerTwo.ReviewTask(_task);
            
            Services.AddScoped(_ => _mockTaskTagRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskChangelogRepository.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockWorklogEntryService.Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
            
            _mockProjectRepository.Setup(x =>
                    x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(new Project());
            
            _mockWorklogEntryService.Setup(x => x.GetWorklogEntriesForTaskAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<WorklogEntry>());
        }

        private void CreateComponent(bool readOnly = false)
        {
            _component = RenderComponent<SprintBoardTask>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue("ProjectState", new ProjectState{IsReadOnly = readOnly})
                .Add(p => p.TaskModel, _task)
                .Add(p => p.MembersChanged, _onMembersChanged.Object)
            );
        }

        [Fact]
        public void ComponentRendered_FindTaskCard_ElementFound()
        {
            CreateComponent();
            _component.Find($"#task-card-{_task.Id}");
        }

        [Fact]
        public void ComponentRendered_FindCardTitle_ContainsTaskName()
        {
            CreateComponent();
            _component.Find(".task-name").TextContent.Trim().Should().Contain(_task.Name);
        }

        [Fact]
        public void ComponentRendered_FindCardEstimate_ContainsTaskEstimate()
        {
            CreateComponent();
            _component.Find("#time-estimated").TextContent.Trim().Should().Be(DurationUtils.DurationStringFrom(_task.Estimate));
        }

        [Fact]
        public void ComponentRendered_FindCardTags_ContainsTaskTags()
        {
            CreateComponent();
            var expected = _task.Tags;
            var tagElems = _component.FindAll(".card-tag");
            foreach (var (tag, tagElem) in expected.Zip(tagElems)){
                tagElem.TextContent.Trim().Should().Be(tag.Name);
            }
        }

        [Fact]
        public void ComponentRendered_FindCardPriority_ContainsTaskPriority()
        {
            CreateComponent();
            _component.FindComponent<PriorityIndicator>().Instance.Priority.Should().Be(_task.Priority);
        }
        
        [Fact]
        public void ComponentRendered_FindCardComplexity_ContainsTaskComplexity()
        {
            CreateComponent();
            _component.FindComponent<ComplexityIndicator>().Instance.Complexity.Should().Be(_task.Complexity);
        }

        [Fact]
        public async Task ComponentRendered_UpdateAssigneeSelection_AssigneesUpdatedAndPersisted() {
            CreateComponent();
            var inputMembers = _component.FindComponents<InputMember>()
                .Where(component => component.FindAll("#assignee-select").Any()).ToList();
            inputMembers.Should().HaveCount(1);
            var inputMember = inputMembers.First();

            _mockUserStoryTaskRepository
                .Setup(mock => mock.UpdateAssociations(_task))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()))
                .Returns(Task.CompletedTask);
            
            await _component.InvokeAsync(() => inputMember.Instance.ValueChanged.InvokeAsync(new List<User>() { _reviewerTwo }));

            _mockUserStoryTaskRepository
                .Verify(mock => mock.UpdateAssociations(_task), Times.Once);
            _mockUserStoryTaskChangelogRepository
                .Verify(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()), Times.Once);

            _task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Assigned)).Should().BeEquivalentTo(new List<UserTaskAssociation> {
                new() { UserId = _reviewerTwo.Id, User = _reviewerTwo, TaskId = _task.Id, Task = _task, Role = TaskRole.Assigned },
            });
        }

        [Fact]
        public async Task ComponentRendered_UpdateReviewerSelection_ReviewersUpdatedAndPersisted() 
        {
            CreateComponent();
            var inputMembers = _component.FindComponents<InputMember>()
                .Where(component => component.FindAll("#reviewer-select").Any()).ToList();
            inputMembers.Should().HaveCount(1);
            var inputMember = inputMembers.First();
            
            _mockUserStoryTaskRepository
                .Setup(mock => mock.UpdateAssociations(_task))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()))
                .Returns(Task.CompletedTask);
            
            await _component.InvokeAsync(() => inputMember.Instance.ValueChanged.InvokeAsync(new List<User>() { _assigneeOne }));

            _mockUserStoryTaskRepository
                .Verify(mock => mock.UpdateAssociations(_task), Times.Once);
            _mockUserStoryTaskChangelogRepository
                .Verify(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()), Times.Once);

            _task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Reviewer)).Should().BeEquivalentTo(new List<UserTaskAssociation> {
                new() { UserId = _assigneeOne.Id, User = _assigneeOne, TaskId = _task.Id, Task = _task, Role = TaskRole.Reviewer },
            });
        }

        [Theory]
        [InlineData("#assignee-select")]
        [InlineData("#reviewer-select")]
        public async Task ComponentRendered_UpdateReviewerOrAssignees_NotifiedOfMembersChanged(string selectorId)
        {
            CreateComponent();
            var inputMembers = _component.FindComponents<InputMember>()
                .Where(component => component.FindAll(selectorId).Any()).ToList();
            inputMembers.Should().HaveCount(1);
            var inputMember = inputMembers.First();
            
            _mockUserStoryTaskRepository
                .Setup(mock => mock.UpdateAssociations(_task))
                .Returns(Task.CompletedTask);
            _mockUserStoryTaskChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<UserStoryTaskChangelogEntry>>()))
                .Returns(Task.CompletedTask);
            
            await _component.InvokeAsync(() => inputMember.Instance.ValueChanged.InvokeAsync(new List<User>() { _assigneeOne }));
            
            _onMembersChanged.Verify(mock => mock(), Times.Once());
        }
        

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ComponentRendered_IsReadOnlySet_InputMemberSelectionsDisabledBasedOnReadOnly(bool readOnly)
        {
            CreateComponent(readOnly);

            var inputMembers = _component.FindComponents<InputMember>();
            inputMembers.Should().HaveCount(2);
            inputMembers.Should().OnlyContain(input => input.Instance.Disabled == readOnly);
        }
    }
}