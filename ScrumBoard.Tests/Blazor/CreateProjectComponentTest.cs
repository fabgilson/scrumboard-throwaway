using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Tests.Util;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class CreateProjectComponentTest : TestContext
    {
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly User _actingUser = FakeDataGenerator.CreateFakeUser();

        private readonly IRenderedComponent<CreateProject> _componentUnderTest;
        
        public CreateProjectComponentTest() 
        {
            var mockUserRepository = new Mock<IUserRepository>();
            mockUserRepository.Setup(x => x.GetByIdAsync(_actingUser.Id)).ReturnsAsync(_actingUser);
            Services.AddScoped(_ => mockUserRepository.Object);

            _mockProjectRepository = new Mock<IProjectRepository>();
            Services.AddScoped(_ => _mockProjectRepository.Object);

            var mockStateStorageService = new Mock<IScrumBoardStateStorageService>();
            Services.AddScoped(_ => mockStateStorageService.Object);
            
            _componentUnderTest = RenderComponent<CreateProject>(p => p
                .AddCascadingValue("Self", _actingUser)
            );
        }

        [Theory]
        [InlineData("Name", "name")]
        [InlineData("Description", "description")]
        [InlineData("EndDate", "end-date")]
        public void SetField_IsEmpty_ErrorMessageDisplayed(string fieldName, string cssName)
        {
            var input = _componentUnderTest.Find($"#{cssName}-input");
            input.Change("");

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll($"#{cssName}-validation-message").Any());

            var errorLabel = _componentUnderTest.Find($"#{cssName}-validation-message");

            var expectedErrorMessage = typeof(ProjectCreateForm).GetAttribute<RequiredAttribute>(fieldName).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Description")]
        public void SetStringField_LongerThanMaximum_ErrorMessageDisplayed(string fieldName)
        {
            var maxLengthAttribute = typeof(ProjectCreateForm).GetAttribute<MaxLengthAttribute>(fieldName);

            var stringInput = _componentUnderTest.Find($"#{fieldName.ToLower()}-input");
            stringInput.Change(new String('a', maxLengthAttribute.Length + 1));

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll($"#{fieldName.ToLower()}-validation-message").Any());

            var errorLabel = _componentUnderTest.Find($"#{fieldName.ToLower()}-validation-message");

            var expectedErrorMessage = maxLengthAttribute.ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Description")]
        public void SetStringField_OnlyContainsSpecialCharacters_ErrorMessageDisplayed(string fieldName)
        {
            var stringInput = _componentUnderTest.Find($"#{fieldName.ToLower()}-input");
            stringInput.Change("*+-0");

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll($"#{fieldName.ToLower()}-validation-message").Any());

            var errorLabel = _componentUnderTest.Find($"#{fieldName.ToLower()}-validation-message");

            var expectedErrorMessage = typeof(ProjectCreateForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(fieldName).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData("StartDate", "start-date")]
        [InlineData("EndDate", "end-date")]
        public void SetDate_IsInvalid_ErrorMessageDisplayed(string fieldName, string cssName) 
        {
            var dateInput = _componentUnderTest.Find($"#{cssName}-input");
            dateInput.Change("foo bar");

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll($"#{cssName}-validation-message").Any());

            var errorLabel = _componentUnderTest.Find($"#{cssName}-validation-message");

            var expectedErrorMessage = $"The {fieldName} field must be a date.";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }


        [Theory]
        [InlineData("StartDate", "start-date", "2020-04-31")]
        [InlineData("EndDate", "end-date", "2020-04-31")]
        [InlineData("StartDate", "start-date", "2021-02-29")]
        [InlineData("EndDate", "end-date", "2021-02-29")]
        [InlineData("StartDate", "start-date", "2021-13-29")]
        [InlineData("EndDate", "end-date", "2021-13-29")]
        public void SetDate_IsInvalid_WrongDaysAndMonths_ErrorMessageDisplayed(string fieldName, string cssName, string dateString) 
        {
            var nameInput = _componentUnderTest.Find($"#{cssName}-input");
            nameInput.Change(dateString);

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll($"#{cssName}-validation-message").Any());

            var errorLabel = _componentUnderTest.Find($"#{cssName}-validation-message");

            var expectedErrorMessage = $"The {fieldName} field must be a date.";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetStartDate_IsInPast_ErrorMessageDisplayed() 
        {
            var startDateInput = _componentUnderTest.Find("#start-date-input");
            startDateInput.Change("1/1/2020");

            _componentUnderTest.Find("#create-project-form").Submit();
            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll("#start-date-validation-message").Any());

            var errorLabel = _componentUnderTest.Find("#start-date-validation-message");

            var expectedErrorMessage = typeof(ProjectCreateForm).GetAttribute<DateInFuture>("StartDate").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SubmitForm_WithValidFields_NewProjectSaved() {
            var name = "Test Project Name";
            var description = "Test Project Description";
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(42);

            _componentUnderTest.Find("#name-input").Change(name);
            _componentUnderTest.Find("#description-input").Change(description);
            _componentUnderTest.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _componentUnderTest.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

            var selectedUsers = new List<User>{
                new() { Id = 7, FirstName = "Alex", LastName = "Johnson" }, 
            };
            _componentUnderTest.Instance.UpdateSelectedUsers(selectedUsers, null);
        
            _componentUnderTest.Find("#create-project-form").Submit();    

            var arg = new ArgumentCaptor<Project>();
            _mockProjectRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
        }

        [Fact]
        public void SubmitForm_WithValidFields_ProjectDetailsCorrect() 
        {
            var name = "Test Project Name";
            var description = "Test Project Description";
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(42);

            _componentUnderTest.Find("#name-input").Change(name);
            _componentUnderTest.Find("#description-input").Change(description);
            _componentUnderTest.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _componentUnderTest.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

            var selectedUsers = new List<User>{
                new() { Id = 7, FirstName = "Alex", LastName="Rider" }, 
            };
            _componentUnderTest.Instance.UpdateSelectedUsers(selectedUsers, null);

            var before = DateTime.Now;
            _componentUnderTest.Find("#create-project-form").Submit();
            var after = DateTime.Now;

            var arg = new ArgumentCaptor<Project>();
            _mockProjectRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var project = arg.Value;   

            project.Name.Should().Be(name);
            project.Description.Should().Be(description);
            project.StartDate.Should().Be(startDate);
            project.EndDate.Should().Be(endDate);

            project.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void SubmitForm_WithValidFields_ProjectCreatorCorrect()
        {
            var name = "Test Project Name";
            var description = "Test Project Description";
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(42);

            _componentUnderTest.Find("#name-input").Change(name);
            _componentUnderTest.Find("#description-input").Change(description);
            _componentUnderTest.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _componentUnderTest.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

            var selectedUsers = new List<User>{
                new() { Id = 7, FirstName = "Alex", LastName="Rider" }, 
            };
            _componentUnderTest.Instance.UpdateSelectedUsers(selectedUsers, null);
            
            _componentUnderTest.Find("#create-project-form").Submit();

            var arg = new ArgumentCaptor<Project>();
            _mockProjectRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var project = arg.Value;   

            project.CreatorId.Should().Be(_actingUser.Id);
        }

        [Fact]
        public void SubmitForm_WithValidFields_HasExpectedMemberAssociations()
        {
            var name = "Test Project Name";
            var description = "Test Project Description";
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(42);

            _componentUnderTest.Find("#name-input").Change(name);
            _componentUnderTest.Find("#description-input").Change(description);
            _componentUnderTest.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _componentUnderTest.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

            var selectedUsers = new List<User>{
                new() { Id = 7, FirstName = "Alex", LastName="Rider" }, 
            };
            _componentUnderTest.Instance.UpdateSelectedUsers(selectedUsers, null);
            
            _componentUnderTest.Find("#create-project-form").Submit();

            var arg = new ArgumentCaptor<Project>();
            _mockProjectRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var project = arg.Value;   

            project.MemberAssociations.Should()
                .BeEquivalentTo(
                    selectedUsers.Select(user => new ProjectUserMembership() { UserId = user.Id, Role = ProjectRole.Developer})
                );
        }

        [Fact]
        public void SubmitForm_WithValidFields_HasCurrentUserAsLeaderByDefault() {
            var name = "Test Project Name";
            var description = "Test Project Description";
            var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
            var endDate = startDate.AddDays(42);

            _componentUnderTest.Find("#name-input").Change(name);
            _componentUnderTest.Find("#description-input").Change(description);
            _componentUnderTest.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
            _componentUnderTest.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));            
        
            _componentUnderTest.Find("#create-project-form").Submit();    

            var arg = new ArgumentCaptor<Project>();
            _mockProjectRepository.Verify(mock => mock.AddAsync(arg.Capture()), Times.Once());
            var project = arg.Value;   

            var memberships = new List<ProjectUserMembership>() {
                new() { 
                    UserId = _actingUser.Id, 
                    Role = ProjectRole.Leader
                    }
            };

            project.MemberAssociations.Should().BeEquivalentTo(memberships);
        }
    }
}
