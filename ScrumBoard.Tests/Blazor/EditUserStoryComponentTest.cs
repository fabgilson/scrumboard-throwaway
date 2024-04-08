using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class EditUserStoryComponentTest : TestContext
    {
        private static readonly Project Project = new();

        // User id of the user that is performing actions on the CreateProject component
        private readonly User _actingUser = new() {Id = 33, FirstName = "Jeff", LastName = "Jefferson" };
        private readonly UserStory _editedStory = new() { Project = Project, StoryGroup = Project.Backlog, AcceptanceCriterias = new List<AcceptanceCriteria>() };

        private readonly UserStory _storyInProgress = new() { Project = Project, StoryGroup = Project.Backlog, Stage = Stage.InProgress };

        private readonly UserStory _storyInDone = new() { Project = Project, StoryGroup = Project.Backlog, Stage = Stage.Done };

        private readonly UserStory _storyInDeferred = new() { Project = Project, StoryGroup = Project.Backlog, Stage = Stage.Deferred };

        private readonly UserStory _storyInReview = new() { Project = Project, StoryGroup = Project.Backlog, Stage = Stage.UnderReview };

        private readonly List<UserStory> _stories;

        private IRenderedComponent<EditUserStory> _component;

        private readonly Mock<IProjectRepository> _mockProjectRepository = new();

        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new();

        private readonly Mock<IUserStoryChangelogRepository> _mockUserStoryChangelogRepository = new();

        private Mock<Action<bool>> _onSave = new();

        private Mock<Action> _onCancel = new();

        public EditUserStoryComponentTest() {
            _stories = new List<UserStory> { _editedStory, _storyInReview, _storyInProgress, _storyInDeferred, _storyInDone };
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockUserStoryRepository.Object);      
            Services.AddScoped(_ => _mockUserStoryChangelogRepository.Object);
            ComponentFactories.AddDummyFactoryFor<Markdown>();
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
            
            _component = RenderComponent<EditUserStory>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Story, _editedStory)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );
        }

        [Theory]
        [InlineData(Stage.UnderReview, true)]
        [InlineData(Stage.Todo, false)]
        [InlineData(Stage.Done, true)]
        [InlineData(Stage.InProgress, false)]
        [InlineData(Stage.Deferred, true)]
        public void FindAcceptanceCriteria_GivenStoryState_FormDisabledStateIsObserved(Stage stage, bool isDisabled)
        {
            UserStory story = _stories.Where(s => s.Stage == stage).First();
            _component = RenderComponent<EditUserStory>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Story, story)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );
            var acForm = _component.FindComponents<InputTextArea>()
                .First(c => c.Instance.AdditionalAttributes["class"].ToString().Contains("acceptance-criteria"));
            acForm.Instance.AdditionalAttributes["disabled"].Should().Be(isDisabled);
        }

        [Theory]
        [InlineData(Stage.UnderReview, true)]
        [InlineData(Stage.Todo, false)]
        [InlineData(Stage.Done, true)]
        [InlineData(Stage.InProgress, false)]
        [InlineData(Stage.Deferred, true)]
        public void FindAcceptanceCriteriaDeleteButton_GivenStoryState_ObserveDVisible(Stage stage, bool isNotPresent)
        {
            UserStory story = _stories.First(s => s.Stage == stage);
            _component = RenderComponent<EditUserStory>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Story, story)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );

            var acDisableButton = _component.FindAll(".btn-delete-acceptance-criteria");
            (acDisableButton.Count() == 0).Should().Be(isNotPresent);
        }

        [Theory]
        [InlineData(Stage.UnderReview, true)]
        [InlineData(Stage.Todo, false)]
        [InlineData(Stage.Done, true)]
        [InlineData(Stage.InProgress, false)]
        [InlineData(Stage.Deferred, true)]
        public void FindAcceptanceCriteriaAddButton_GivenStoryState_ObserveVisibleState(Stage stage, bool isNotPresent)
        {
            UserStory story = _stories.First(s => s.Stage == stage);
            _component = RenderComponent<EditUserStory>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Story, story)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );

            var acDisableButton = _component.FindAll("#add-acceptance-criteria");
            (acDisableButton.Count() == 0).Should().Be(isNotPresent);
        }

        [Theory]
        [InlineData(Stage.UnderReview, true)]
        [InlineData(Stage.Todo, false)]
        [InlineData(Stage.Done, true)]
        [InlineData(Stage.InProgress, false)]
        [InlineData(Stage.Deferred, true)]
        public void FindCanEditMessage_GivenStoryState_ObserveVisibleState(Stage stage, bool visible)
        {
            UserStory story = _stories.First(s => s.Stage == stage);
            _component = RenderComponent<EditUserStory>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Story, story)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
            );

            (_component.FindAll("#story-locked-message").Count > 0).Should().Be(visible);
        }

        [Theory]
        [InlineData("Name", "name")]
        [InlineData("Description", "description")]
        public void SetField_IsEmpty_ErrorMessageDisplayed(string fieldName, string cssName)
        {
            var input = _component.Find($"#{cssName}-input");
            input.Change("");

            _component.Find("#edit-user-story-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#{cssName}-validation-message").Any());

            var errorLabel = _component.Find($"#{cssName}-validation-message");

            var expectedErrorMessage = typeof(UserStoryForm).GetAttribute<RequiredAttribute>(fieldName).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Description")]
        public void SetStringField_LongerThanMaximum_ErrorMessageDisplayed(string fieldName)
        {
            var maxLengthAttribute = typeof(UserStoryForm).GetAttribute<MaxLengthAttribute>(fieldName);

            var stringInput = _component.Find($"#{fieldName.ToLower()}-input");
            stringInput.Change(new String('a', maxLengthAttribute.Length + 1));

            _component.Find("#edit-user-story-form").Submit();
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
            var stringInput = _component.Find($"#{fieldName.ToLower()}-input");
            stringInput.Change("*+-0");

            _component.Find("#edit-user-story-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#{fieldName.ToLower()}-validation-message").Any());

            var errorLabel = _component.Find($"#{fieldName.ToLower()}-validation-message");

            var expectedErrorMessage = typeof(UserStoryForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(fieldName).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetAcceptanceCriteria_AddButtonPressed_AcceptanceCriteriaAdded()
        {
            // There should be one acceptance criteria initially
            _component.FindAll(".acceptance-criteria").Should().HaveCount(1);

            _component.Find("#add-acceptance-criteria").Click();
            _component.FindAll(".acceptance-criteria").Should().HaveCount(2);
        }

        [Fact]
        public void SetAcceptanceCriteria_RemoveButtonPressed_AcceptanceCriteriaRemoved() 
        {
            var elem = _component.Find(".acceptance-criteria");
            var removeButton = elem.GetElementsByTagName("button").First();
            removeButton.Click();

            _component.FindAll(".acceptance-criteria").Should().HaveCount(0);
        }

        [Fact]
        public void SetAcceptanceCriteria_IsEmpty_ErrorMessageDisplayed() 
        {
            var attribute = typeof(AcceptanceCriteriaForm).GetAttribute<RequiredAttribute>("Content");

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("");

            var errorLabel = elem.GetElementsByClassName("validation-message").First();
            errorLabel.TextContent.Should().Be(attribute.ErrorMessage);
        }

        [Fact]
        public void SetAcceptanceCriteria_OnlyContainsSpecialCharacters_ErrorMessageDisplayed() 
        {
            var attribute = typeof(AcceptanceCriteriaForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>("Content");

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("*+-0");

            var errorLabel = elem.GetElementsByClassName("validation-message").First();
            errorLabel.TextContent.Should().Be(attribute.ErrorMessage);
        }

        [Fact]
        public void SetAcceptanceCriteria_LongerThanMaximum_ErrorMessageDisplayed() 
        {
            var attribute = typeof(AcceptanceCriteriaForm).GetAttribute<MaxLengthAttribute>("Content");

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change(new String('a', attribute.Length + 1));

            var errorLabel = elem.GetElementsByClassName("validation-message").First();
            errorLabel.TextContent.Should().Be(attribute.ErrorMessage);
        }

        [Fact]
        public void SetAcceptanceCriteria_AllRemoved_ErrorMessageDisplayed()
        {
            // Delete the initially provided AC
            var elem = _component.Find(".acceptance-criteria");
            var removeButton = elem.GetElementsByTagName("button").First();
            removeButton.Click();

            _component.Find("#edit-user-story-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#acceptance-criteria-validation-message").Any());

            var errorLabel = _component.Find($"#acceptance-criteria-validation-message");

            var expectedErrorMessage = typeof(UserStoryForm).GetAttribute<MinLengthAttribute>(nameof(UserStoryForm.AcceptanceCriterias)).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void CancelButton_Pressed_OnCancelCalled() {
            _component.Find("#cancel-button").Click();
            _onCancel.Verify(mock => mock(), Times.Once());
        }

        [Fact]
        public void SubmitForm_NewStoryWithValidFields_AddedWithStoryDetailsCorrect()
        {
            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;
            var priority = Priority.Critical;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find($"#estimate-select-{estimate}").Click();


            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            _component.Find("#edit-user-story-form").Submit();

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var story = arg.Value;   

            story.Name.Should().Be(name);
            story.Description.Should().Be(description);
            story.Estimate.Should().Be(estimate);
            story.Priority.Should().Be(priority);
            story.CreatorId.Should().Be(_actingUser.Id);
        }

        [Fact]
        public void SubmitForm_NewStoryWithValidFields_NoChangelogEntriesAdded()
        {
            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#estimate-select-{estimate}").Click();

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            _component.Find("#edit-user-story-form").Submit(); 

            _mockUserStoryChangelogRepository.Verify(mock => mock.AddAllAsync(It.IsAny<List<UserStoryChangelogEntry>>()), Times.Never());
            _mockUserStoryChangelogRepository.Verify(mock => mock.AddAsync(It.IsAny<UserStoryChangelogEntry>()), Times.Never());
        }

        [Fact]
        public void SubmitForm_ExistingStoryWithValidFields_ChangelogEntriesAdded()
        {
            _editedStory.Id = 13;

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#estimate-select-{estimate}").Click();

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            _component.Find("#edit-user-story-form").Submit(); 

            _mockUserStoryChangelogRepository.Verify(mock => mock.AddAllAsync(It.IsAny<List<UserStoryChangelogEntry>>()), Times.Once());
        }

        [Fact]
        public void SubmitForm_ExistingStoryWithValidFields_AddedWithStoryDetailsCorrect()
        {
            var storyId = 49;
            _editedStory.Id = storyId;

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;
            var priority = Priority.Critical;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find($"#estimate-select-{estimate}").Click();


            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            _component.Find("#edit-user-story-form").Submit();

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.UpdateAsync(arg.Capture()), Times.Once());
            var story = arg.Value;   

            story.Id.Should().Be(storyId);
            story.Name.Should().Be(name);
            story.Description.Should().Be(description);
            story.Estimate.Should().Be(estimate);
            story.Priority.Should().Be(priority);
        }

        [Theory]
        [InlineData(0, true)] // New story will have the default Id
        [InlineData(49, false)]
        public void SubmitForm_WithValidFields_OnSaveCalled(long id, bool isNewStory)
        {
            _editedStory.Id = id;

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#estimate-select-{estimate}").Click();

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            _component.Find("#edit-user-story-form").Submit(); 

            _onSave.Verify(mock => mock(isNewStory), Times.Once());
            _component.FindAll("#story-concurrency-error").Should().BeEmpty();
        }

        [Fact]
        public void SubmitForm_NewStoryWithValidFields_CreationDateCorrect()
        {
            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#estimate-select-{estimate}").Click();

            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            var before = DateTime.Now;
            _component.Find("#edit-user-story-form").Submit();
            var after = DateTime.Now;

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var story = arg.Value;   

            story.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void SubmitForm_WithValidFields_StoryAcceptanceCriteriaCorrect()
        {
            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#estimate-select-{estimate}").Click();

            _component.Find("#add-acceptance-criteria").Click();

            var acceptanceCriteria = new List<string>{
                "first ac",
                "second ac",
            };
            var input = _component.FindAll(".acceptance-criteria").First().GetElementsByTagName("textarea").First();
            input.Change(acceptanceCriteria[0]);
            // Component has re-rendered, so it must be queried again
            input = _component.FindAll(".acceptance-criteria").Last().GetElementsByTagName("textarea").First();
            input.Change(acceptanceCriteria[1]);

            _component.Find("#edit-user-story-form").Submit();

            var arg = new ArgumentCaptor<UserStory>();
            _mockUserStoryRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var story = arg.Value;

            story.AcceptanceCriterias
                .Select(ac => ac.Content)
                .Should()
                .BeEquivalentTo(acceptanceCriteria);
        }

        [Fact]
        public void SubmitForm_AnotherUserEdited_ErrorMessageDisplayed()
        {
            var storyId = 49;
            _editedStory.Id = storyId;

            var name = "Test Story Name";
            var description = "Test Story Description";
            var estimate = 13;
            var priority = Priority.Critical;

            _component.Find("#name-input").Change(name);
            _component.Find("#description-input").Change(description);
            _component.Find($"#priority-select-{priority}").Click();
            _component.Find($"#estimate-select-{estimate}").Click();


            var elem = _component.Find(".acceptance-criteria");
            var input = elem.GetElementsByTagName("textarea").First();
            input.Change("test ac");

            // Mock a concurrency exception
            _mockUserStoryRepository
                .Setup(mock =>
                    mock.UpdateAsync(It.IsAny<UserStory>()))
                .Throws(new DbUpdateConcurrencyException("Concurrency Error"));

            _component.Find("#edit-user-story-form").Submit();

            _component.FindAll("#story-concurrency-error").Should().ContainSingle();
        }
    }
}
