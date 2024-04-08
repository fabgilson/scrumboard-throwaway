using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using System;
using System.Linq;
using ScrumBoard.Tests.Util;
using Xunit;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Validators;
using ScrumBoard.Shared.Inputs;
using System.Threading.Tasks;
using System.Collections.Generic;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Tests.Blazor
{
    public class EditOverheadEntryComponentTest : TestContext
    {

        
        // User that is performing actions on the EditOverheadEntry component
        private readonly User _actingUser;

        private readonly User _otherUser;
        
        private OverheadEntry _entry;

        private readonly List<OverheadSession> _overheadSessions;

        private readonly OverheadSession _firstSession;
        private readonly OverheadSession _secondSession;
        private readonly OverheadSession _thirdSession;

        private IRenderedComponent<EditOverheadEntry> _component;

        private readonly Sprint _sprint;

        private readonly Mock<IOverheadEntryRepository> _mockOverheadEntryRepository = new(MockBehavior.Strict);

        private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
        
        private readonly Mock<IOverheadEntryChangelogRepository> _mockOverheadEntryChangelogRepository = new(MockBehavior.Strict);

        private readonly Mock<IOverheadSessionRepository> _mockOverheadSessionRepository = new(MockBehavior.Strict);

        private readonly Mock<Action> _onClose = new(MockBehavior.Strict);

        public EditOverheadEntryComponentTest()
        {
            _actingUser = new User
            {
                Id = 33, 
                FirstName = "Jeff", 
                LastName = "Jefferson",
            };

            _otherUser = new User
            {
                Id = 34,
                FirstName = "Jimmy",
                LastName = "Neutron",
            };

            _sprint = new()
            {
                Id = 80,
                StartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
                EndDate = DateOnly.FromDateTime(DateTime.Now).AddDays(1),
                Name = "Test sprint",
            };
            
            _firstSession  = new() { Id = 3, Name = "first"  };
            _secondSession = new() { Id = 4, Name = "second" };
            _thirdSession  = new() { Id = 5, Name = "third"  };

            _overheadSessions = new()
            {
                _firstSession,
                _secondSession,
                _thirdSession,
            };

            _entry = new OverheadEntry()
            {
                Sprint = _sprint,
            };

            _mockOverheadSessionRepository
                .Setup(mock => mock.GetAllAsync(It.IsAny<Func<IQueryable<OverheadSession>,IQueryable<OverheadSession>>>()))
                .ReturnsAsync(_overheadSessions);
            
            Services.AddScoped(_ => _mockOverheadEntryRepository.Object);
            Services.AddScoped(_ => _mockOverheadEntryChangelogRepository.Object);
            Services.AddScoped(_ => _mockOverheadSessionRepository.Object);
            Services.AddScoped(_ => _mockSprintRepository.Object);

            ComponentFactories.AddDummyFactoryFor<Markdown>();
        }

        private void CreateComponent() {

            _component = RenderComponent<EditOverheadEntry>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.Entry, _entry)
                .Add(cut => cut.OnClose, _onClose.Object)
            );
        }

        /// <summary>
        /// Sets up the mocks for saving a new valid overhead entry
        /// </summary>
        private void SetupForCreate()
        {
            _mockOverheadEntryRepository
                .Setup(mock => mock.AddAsync(It.IsAny<OverheadEntry>()))
                .Returns(Task.CompletedTask);
            _onClose
                .Setup(mock => mock());
        }
        
        /// <summary>
        /// Sets up the mocks for updating a existing valid overhead entry
        /// </summary>
        private void SetupForEdit()
        {
            _mockOverheadEntryRepository
                .Setup(mock => mock.UpdateAsync(It.IsAny<OverheadEntry>()))
                .Returns(Task.CompletedTask);
            _mockOverheadEntryChangelogRepository
                .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<OverheadEntryChangelogEntry>>()))
                .Returns(Task.CompletedTask);
            _onClose
                .Setup(mock => mock());
        }

        /// <summary>
        /// Sets up the current overhead entry as if it were an existing entry
        /// </summary>
        private void SetupExistingOverheadEntry()
        {
            _entry.Id = 70;
            _entry.Description = "existing description";
            _entry.Session = _firstSession;
            _entry.Duration = TimeSpan.FromHours(2);
            _entry.Created = new DateTime(2012, 12, 21);
            _entry.User = _otherUser;
        }

        [Fact]
        public void Rendered_CancelPressed_OnCloseTriggered()
        {
            CreateComponent();

            _onClose.Setup(mock => mock());
            _component.Find("#cancel-button").Click();
            _onClose.Verify(mock => mock(), Times.Once);
        }

        [Fact]
        public void SetDescription_Empty_ErrorMessageDisplayed()
        {
            CreateComponent();
            var requiredAttribute = typeof(OverheadEntryForm).GetAttribute<RequiredAttribute>(nameof(OverheadEntryForm.Description));

            var stringInput = _component.Find($"#description-input");
            stringInput.Change("");

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#description-validation-message").Any());

            var errorLabel = _component.Find($"#description-validation-message");

            var expectedErrorMessage = requiredAttribute.ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }
        
        [Fact]
        public void SetDescription_LongerThanMaximum_ErrorMessageDisplayed()
        {
            CreateComponent();
            var maxLengthAttribute = typeof(OverheadEntryForm).GetAttribute<MaxLengthAttribute>(nameof(OverheadEntryForm.Description));

            var stringInput = _component.Find($"#description-input");
            stringInput.Change(new String('a', maxLengthAttribute.Length + 1));

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#description-validation-message").Any());

            var errorLabel = _component.Find($"#description-validation-message");

            var expectedErrorMessage = maxLengthAttribute.ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetDescription_OnlyContainsSpecialCharacters_ErrorMessageDisplayed()
        {
            CreateComponent();
            var stringInput = _component.Find($"#description-input");
            stringInput.Change("*+-0");

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#description-validation-message").Any());

            var errorLabel = _component.Find($"#description-validation-message");

            var expectedErrorMessage = typeof(OverheadEntryForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(OverheadEntryForm.Description)).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }
        
        [Fact]
        public void SetDuration_InvalidValue_ErrorMessageDisplayed() {
            CreateComponent();
            var estimateInput = _component.Find($"#duration-input");
            estimateInput.Change("seven");

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#duration-validation-message").Any());

            var errorLabel = _component.Find($"#duration-validation-message");

            var expectedErrorMessage = "Invalid duration format";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetDuration_NotPositive_ErrorMessageDisplayed() {
            CreateComponent();
            var estimateInput = _component.Find($"#duration-input");
            estimateInput.Change("0s");

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#duration-validation-message").Any());

            var errorLabel = _component.Find($"#duration-validation-message");

            var expectedErrorMessage = "Must be no less than 1 minute";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void SetDuration_OverOneDay_ErrorMessageDisplayed()
        {
            CreateComponent();
            var estimateInput = _component.Find($"#duration-input");
            estimateInput.Change("25h");

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#duration-validation-message").Any());

            var errorLabel = _component.Find($"#duration-validation-message");

            var expectedErrorMessage = "Must be no greater than 24 hours";
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }
        
        [Fact]
        public void SetSession_NotProvided_ErrorMessageDisplayed() {
            CreateComponent();

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#session-validation-message").Any());

            var errorLabel = _component.Find($"#session-validation-message");

            var expectedErrorMessage = typeof(OverheadEntryForm).GetAttribute<RequiredAttribute>(nameof(OverheadEntryForm.Session)).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }
        
        [Fact]
        public void SetDateOccurred_InFuture_ErrorMessageDisplayed() {
            CreateComponent();
            
            _component.Find("#date-input").Change(DateOnly.FromDateTime(DateTime.Now).AddDays(2).ToString("yyyy-MM-dd"));

            _component.Find("#edit-overhead-entry-form").Submit();
            _component.WaitForState(() => _component.FindAll($"#date-occurred-validation-message").Any());

            var errorLabel = _component.Find($"#date-occurred-validation-message");

            var expectedErrorMessage = typeof(OverheadEntryForm).GetAttribute<DateInPast>(nameof(OverheadEntryForm.DateOccurred)).ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
        }

        [Fact]
        public void Submit_ValidFields_NewOverheadEntrySaved()
        {
            CreateComponent();

            var description = "test description";
            var duration = TimeSpan.FromHours(3);
            var session = _secondSession;
            var occurred = DateTime.Now.AddDays(-2).AddMinutes(40).TrimSeconds();
            
            _component.Find("#description-input").Change(description);
            _component.Find("#duration-input").Change(DurationUtils.DurationStringFrom(duration));
            _component.Find("#session-input")
                .QuerySelectorAll(".dropdown-item")
                .Single(elem => elem.TextContent.Contains(session.Name))
                .Click();
            _component.Find("#date-input").Change(DateOnly.FromDateTime(occurred).ToString("yyyy-MM-dd"));
            _component.Find("#time-input").Change(TimeOnly.FromDateTime(occurred).ToString());


            SetupForCreate();
            
            _component.Find("#edit-overhead-entry-form").Submit();

            var captor = new ArgumentCaptor<OverheadEntry>();
            _mockOverheadEntryRepository
                .Verify(mock => mock.AddAsync(captor.Capture()), Times.Once);
            var value = captor.Value;

            value.Description.Should().Be(description);
            value.Duration.Should().Be(duration);
            value.SessionId.Should().Be(session.Id);
            value.UserId.Should().Be(_actingUser.Id);
            value.Occurred.Should().Be(occurred);
        }
        
        [Fact]
        public void Submit_ValidFields_OnCloseCalled()
        {
            CreateComponent();

            var description = "test description";
            var duration = TimeSpan.FromHours(3);
            var session = _secondSession;
            
            _component.Find("#description-input").Change(description);
            _component.Find("#duration-input").Change(DurationUtils.DurationStringFrom(duration));
            _component.Find("#session-input")
                .QuerySelectorAll(".dropdown-item")
                .Single(elem => elem.TextContent.Contains(session.Name))
                .Click();


            SetupForCreate();
            
            _component.Find("#edit-overhead-entry-form").Submit();

            _onClose.Verify(mock => mock(), Times.Once);
        }
        
        [Fact]
        public void Submit_ValidFields_CreatedTimeCorrect()
        {
            CreateComponent();

            var description = "test description";
            var duration = TimeSpan.FromHours(3);
            var session = _secondSession;
            
            _component.Find("#description-input").Change(description);
            _component.Find("#duration-input").Change(DurationUtils.DurationStringFrom(duration));
            _component.Find("#session-input")
                .QuerySelectorAll(".dropdown-item")
                .Single(elem => elem.TextContent.Contains(session.Name))
                .Click();


            SetupForCreate();

            var before = DateTime.Now;
            _component.Find("#edit-overhead-entry-form").Submit();
            var after = DateTime.Now;

            var captor = new ArgumentCaptor<OverheadEntry>();
            _mockOverheadEntryRepository
                .Verify(mock => mock.AddAsync(captor.Capture()), Times.Once);
            var value = captor.Value;

            value.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void Submit_ExistingOverheadEntryNoFieldsChanged_FieldsSame()
        {
            SetupExistingOverheadEntry();
            
            CreateComponent();
            
            SetupForEdit();
            
            _component.Find("#edit-overhead-entry-form").Submit();
            
            var captor = new ArgumentCaptor<OverheadEntry>();
            _mockOverheadEntryRepository
                .Verify(mock => mock.UpdateAsync(captor.Capture()), Times.Once);
            var value = captor.Value;
            
            // Refresh _entry fields since we got clobbered
            SetupExistingOverheadEntry();

            value.Id.Should().Be(_entry.Id);
            value.Created.Should().Be(_entry.Created);
            value.UserId.Should().Be(_entry.User.Id);
            value.Description.Should().Be(_entry.Description);
            value.Duration.Should().Be(_entry.Duration);
            value.SessionId.Should().Be(_entry.Session.Id);
        }
        
        [Fact]
        public void Submit_ExistingOverheadEntryNoFieldsChanged_NoChangesMade()
        {
            SetupExistingOverheadEntry();
            
            CreateComponent();
            
            SetupForEdit();
            
            _component.Find("#edit-overhead-entry-form").Submit();
            
            var captor = new ArgumentCaptor<List<OverheadEntryChangelogEntry>>();
            _mockOverheadEntryChangelogRepository
                .Verify(mock => mock.AddAllAsync(captor.Capture()), Times.Once);
            captor.Value.Should().BeEmpty();
        }
        
        [Fact]
        public void Submit_ExistingOverheadEntryFieldsChanged_ChangelogEntriesAdded()
        {
            SetupExistingOverheadEntry();
            
            CreateComponent();
            
            SetupForEdit();
            
            var description = "new test description";
            var duration = TimeSpan.FromHours(3);
            var session = _secondSession;
            
            _component.Find("#description-input").Change(description);
            _component.Find("#duration-input").Change(DurationUtils.DurationStringFrom(duration));
            _component.Find("#session-input")
                .QuerySelectorAll(".dropdown-item")
                .Single(elem => elem.TextContent.Contains(session.Name))
                .Click();
            
            _component.Find("#edit-overhead-entry-form").Submit();
            
            var captor = new ArgumentCaptor<List<OverheadEntryChangelogEntry>>();
            _mockOverheadEntryChangelogRepository
                .Verify(mock => mock.AddAllAsync(captor.Capture()), Times.Once);
            var changes = captor.Value;
            
            // Refresh _entry fields since we got clobbered
            SetupExistingOverheadEntry();

            changes.Should().HaveCount(3);
            changes.Should().OnlyContain(change => change.OverheadEntryChangedId == _entry.Id);
            changes.Should().OnlyContain(change => change.CreatorId == _actingUser.Id);

            {
                var change = changes
                    .Where(change => change.FieldChanged == nameof(OverheadEntry.Description))
                    .Should()
                    .ContainSingle()
                    .Which;
                change.FromValueObject.Should().Be(_entry.Description);
                change.ToValueObject.Should().Be(description);
            }
            {
                var change = changes
                    .Where(change => change.FieldChanged == nameof(OverheadEntry.Duration))
                    .Should()
                    .ContainSingle()
                    .Which;
                change.FromValueObject.Should().Be(_entry.Duration);
                change.ToValueObject.Should().Be(duration);
            }
            {
                var change = changes
                    .OfType<OverheadEntrySessionChangelogEntry>()
                    .Should()
                    .ContainSingle()
                    .Which;
                change.OldSessionId.Should().Be(_entry.Session.Id);
                change.NewSessionId.Should().Be(session.Id);
            }
        }
        
        [Fact]
        public void Submit_ExistingOverheadEntryConcurrencyError_ErrorShown()
        {
            SetupExistingOverheadEntry();
            
            CreateComponent();
            
            _component.FindAll("#overhead-concurrency-error").Should().BeEmpty();

            _mockOverheadEntryRepository
                .Setup(mock => mock.UpdateAsync(It.IsAny<OverheadEntry>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            
            _component.Find("#edit-overhead-entry-form").Submit();
            
            _component.FindAll("#overhead-concurrency-error").Should().ContainSingle();
        }
    }
}
