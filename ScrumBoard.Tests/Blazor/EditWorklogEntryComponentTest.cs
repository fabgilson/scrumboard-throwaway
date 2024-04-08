using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bogus;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Tests.Util;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class EditWorklogEntryComponentTest : BaseProjectScopedComponentTestContext<EditWorklogEntry>
{
    private readonly WorklogTag _featureTag  = new() { Name = "Feature",  Id = 1 };
    private readonly WorklogTag _choreTag    = new() { Name = "Chore",    Id = 2 };
    private readonly WorklogTag _documentTag = new() { Name = "Document", Id = 3 };

    private const long _existingWorklogEntryId = 12345;
    
    private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new(MockBehavior.Loose);
    private readonly Mock<ISprintRepository> _mockSprintRepository = new(MockBehavior.Strict);
    private readonly Mock<IUsageDataService> _mockUsageDataService = new(MockBehavior.Loose);
    private readonly Mock<IGitlabService> _mockGitlabService = new(MockBehavior.Strict);
    private readonly Mock<IGitlabCommitRepository> _mockGitlabCommitRepository = new(MockBehavior.Strict);
    private readonly Mock<IWorklogTagService> _mockWorklogTagService = new(MockBehavior.Strict);
    private readonly Mock<IUserFlagService> _mockUserFlagService = new();
    
    private readonly Mock<Action> _mockOnCloseCallback = new(MockBehavior.Loose);
    private readonly Mock<Action> _mockOnUpdateCallback = new(MockBehavior.Loose);
    
    private readonly Sprint _sprint;
    private readonly UserStory _userStory;
    private readonly UserStoryTask _userStoryTask;
    private readonly TaggedWorkInstanceForm _taggedWorkInstanceForm;

    private IElement DescriptionTextArea => ComponentUnderTest.Find("#description-input");
    private IElement DescriptionValidationMessage => ComponentUnderTest.Find("#description-validation-message");
    
    private IRenderedComponent<DateTimePicker> OccurredPicker => ComponentUnderTest.FindComponent<DateTimePicker>();
    private IElement OccurredValidationMessage => ComponentUnderTest.WaitForElement("#start-validation-message");
    
    private IRenderedComponent<TaggedWorkInstanceInput> TaggedWorkInstanceInput => ComponentUnderTest.FindComponent<TaggedWorkInstanceInput>();
    private IElement TaggedWorkInstanceInputValidationMessage => ComponentUnderTest.Find("#tagged-work-instance-input-validation-message");
    
    private Func<IElement> GetSelectPairUserMenuButton => () => ComponentUnderTest.Find("#user-menu-button");
    private IEnumerable<IElement> SelectPairButtons => ComponentUnderTest.FindAll("[id^=\"pair-user-select-\"]");
    private Func<IElement> GetRemovePairUserButton => () => ComponentUnderTest.Find("#remove-user-button");
    
    private IElement EditFormElement => ComponentUnderTest.Find("#edit-worklog-entry-form");

    private IElement CancelButton => ComponentUnderTest.Find("#cancel-button");

    private Func<IElement> GetConcurrencyErrorMessage => () => ComponentUnderTest.Find("#worklog-concurrency-error");
    
    public EditWorklogEntryComponentTest()
    {
        _mockWorklogTagService.Setup(mock => mock.GetAllAsync())
            .ReturnsAsync(new List<WorklogTag> { _featureTag, _choreTag, _documentTag });

        _sprint = FakeDataGenerator.CreateFakeSprint(CurrentProject, timeStarted: DateTime.Now.AddDays(-1), stage: SprintStage.Started);
        _userStory = FakeDataGenerator.CreateFakeUserStory(_sprint);
        _userStoryTask = FakeDataGenerator.CreateFakeTask(_userStory);
        _taggedWorkInstanceForm = FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag);
        
        _mockSprintRepository.Setup(mock => mock.GetByWorklogEntry(
            It.IsAny<WorklogEntry>(), It.IsAny<Func<IQueryable<Sprint>, IQueryable<Sprint>>[]>())
        ).ReturnsAsync(_sprint);
        
        Services.AddScoped(_ => _mockWorklogEntryService.Object);
        Services.AddScoped(_ => _mockSprintRepository.Object);
        Services.AddScoped(_ => _mockUsageDataService.Object);
        Services.AddScoped(_ => _mockGitlabService.Object);
        Services.AddScoped(_ => _mockGitlabCommitRepository.Object);
        Services.AddScoped(_ => _mockWorklogTagService.Object);
        Services.AddScoped(_ => _mockUserFlagService.Object);
    }

    private void SetupComponent(IEnumerable<User> fakeDevelopers=null, User initialPairUser=null, bool isExistingWorklog=false)
    {
        var worklogEntry = new WorklogEntry
        {
            Id = initialPairUser is null && !isExistingWorklog ? default : _existingWorklogEntryId, // ID must not be default for initial pair user to be set
            Task = _userStoryTask,
            PairUser = initialPairUser,
            PairUserId = initialPairUser?.Id,
            TaggedWorkInstances = new List<TaggedWorkInstance>()
        };
        CreateComponentUnderTest(
            otherDevelopersOnTeam: fakeDevelopers, 
            extendParameterBuilder: parameters => parameters
                .Add(x => x.Entry, worklogEntry)
                .Add(x => x.OnClose, _mockOnCloseCallback.Object)
                .Add(x => x.OnUpdate, _mockOnUpdateCallback.Object)
        );
    }
    
    private async Task SetTaggedWorkInstanceInputValues(params TaggedWorkInstanceForm[] taggedWorkInstanceForms)
    {
        await TaggedWorkInstanceInput.InvokeAsync(async () =>
            await TaggedWorkInstanceInput.Instance.TaggedWorkInstanceFormsChanged.InvokeAsync(
                taggedWorkInstanceForms
            ));
    }

    private async Task SetOccurredDateTimeValue(DateTime dateTime)
    {
        await OccurredPicker.InvokeAsync(async () => await OccurredPicker.Instance.ValueChanged.InvokeAsync(dateTime));
        ComponentUnderTest.WaitForState(() => ComponentUnderTest.Instance.Model.Occurred == dateTime);
    }
    
    [Fact]
    public void SetDescription_Empty_ErrorMessageDisplayed()
    {
        SetupComponent();
        EditFormElement.Submit();

        var expectedErrorText = typeof(WorklogEntryForm).GetErrorMessage<RequiredAttribute>(nameof(WorklogEntryForm.Description));
        DescriptionValidationMessage.TextContent.Should().Be(expectedErrorText);
    }
    
    [Fact]
    public void SetDescription_LongerThanMaximum_ErrorMessageDisplayed()
    {
        var maxLengthAttribute = typeof(WorklogEntryForm).GetAttribute<MaxLengthAttribute>(nameof(WorklogEntryForm.Description));
        
        SetupComponent();
        DescriptionTextArea.Change(new Faker().Lorem.Letter(maxLengthAttribute.Length+1));
        EditFormElement.Submit();
        
        DescriptionValidationMessage.TextContent.Should().Be(maxLengthAttribute.ErrorMessage);
    }
    
    [Fact]
    public void SetDescription_OnlyContainsNumbersOrSpecialCharacters_ErrorMessageDisplayed()
    {
        SetupComponent();
        DescriptionTextArea.Change("@#!$%^&*() 1234567890");
        EditFormElement.Submit();
        
        var expectedErrorText = typeof(WorklogEntryForm).GetErrorMessage<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(WorklogEntryForm.Description));
        DescriptionValidationMessage.TextContent.Should().Be(expectedErrorText);
    }

    [Fact]
    public async Task SetOccurred_BeforeSprintStartDate_ErrorMessageDisplayed()
    {
        SetupComponent();
        // Set occurred to 9am two days before sprint started
        await SetOccurredDateTimeValue(_sprint.StartDate.AddDays(-2).ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9))));
        
        await EditFormElement.SubmitAsync();

        // Can't easily pull this error message from reflection, so we hard code here
        const string expectedErrorText = "Cannot log work before the sprint has started";
        OccurredValidationMessage.TextContent.Should().Be(expectedErrorText);
    }
    
    [Fact]
    public async Task SetOccurred_InFuture_ErrorMessageDisplayed()
    {
        SetupComponent();
        // Set occurred to 9am tomorrow
        await SetOccurredDateTimeValue(DateTime.Today.AddDays(1).AddHours(9));
        
        await EditFormElement.SubmitAsync();

        // Can't easily pull this error message from reflection, so we hard code here
        const string expectedErrorText = "Cannot log work in the future";
        OccurredValidationMessage.TextContent.Should().Be(expectedErrorText);
    }

    [Fact]
    public async Task SetTaggedWorkInstances_Empty_ErrorMessageDisplayed()
    {
        SetupComponent();
        await SetTaggedWorkInstanceInputValues();

        await EditFormElement.SubmitAsync();

        var expectedErrorText = typeof(WorklogEntryForm).GetErrorMessage<CollectionNotEmptyAttribute>(nameof(WorklogEntryForm.TaggedWorkInstanceForms));
        TaggedWorkInstanceInputValidationMessage.TextContent.Should().Be(expectedErrorText);
    }
    
    [Fact]
    public void SelectPairUser_NoPairAlreadySelected_SelectionChangesAndButtonHidden()
    {
        var fakeDevelopers = FakeDataGenerator.CreateMultipleFakeUsers(1).ToList();
        SetupComponent(fakeDevelopers: fakeDevelopers);

        ComponentUnderTest.Instance.Model.PairUser.Should().BeNull();
        
        GetSelectPairUserMenuButton.Should().NotThrow();
        SelectPairButtons.First().Click();

        ComponentUnderTest.Instance.Model.PairUser.Should().Be(fakeDevelopers[0]);
        
        GetSelectPairUserMenuButton.Should().Throw<ElementNotFoundException>();
        GetRemovePairUserButton.Should().NotThrow();
    }

    [Fact]
    public void RemoveSelectedPairUser_PairAlreadySelected_SelectionClearedAndButtonHidden()
    {
        var fakeDevelopers = FakeDataGenerator.CreateMultipleFakeUsers(1).ToList();
        SetupComponent(fakeDevelopers: fakeDevelopers, initialPairUser: fakeDevelopers[0]);

        GetRemovePairUserButton().Click();

        ComponentUnderTest.Instance.Model.PairUser.Should().BeNull();
        
        GetRemovePairUserButton.Should().Throw<ElementNotFoundException>();
        GetSelectPairUserMenuButton.Should().NotThrow();
    }
    
    [Fact]
    public void ViewSelectablePairUsers_NoUserCurrentlySelected_AllOtherUsersAvailable() 
    {      
        var fakeDevelopers = FakeDataGenerator.CreateMultipleFakeUsers(6).ToList();
        SetupComponent(fakeDevelopers: fakeDevelopers);

        SelectPairButtons.Should().HaveCount(6);
        foreach (var teamMember in fakeDevelopers)
        {
            SelectPairButtons.Should().Contain(x => x.TextContent == teamMember.GetFullName());
        }
    }
    
    [Fact]
    public void ViewSelectablePairUsers_NoUserCurrentlySelected_ActingUserNotSelectable() 
    {      
        var fakeDevelopers = FakeDataGenerator.CreateMultipleFakeUsers(6).ToList();
        SetupComponent(fakeDevelopers: fakeDevelopers);

        SelectPairButtons.Should().NotContain(x => x.TextContent == ActingUser.GetFullName());
    }
    
    [Fact]
    public void CancelButtonPressed_OnCloseCalled() 
    {
        SetupComponent();
        CancelButton.Click();
        _mockOnCloseCallback.Verify(x => x(), times: Times.Once);
    }

    private async Task<IEnumerable<User>> SetupFormWithValidValues(bool isExistingWorklog)
    {
        var fakeDevelopers = FakeDataGenerator.CreateMultipleFakeUsers(1).ToList();
        SetupComponent(fakeDevelopers: fakeDevelopers, isExistingWorklog: isExistingWorklog);
        
        await SetTaggedWorkInstanceInputValues(_taggedWorkInstanceForm);
        await SetOccurredDateTimeValue(DateTime.Now);
        DescriptionTextArea.Change("A valid description");
        SelectPairButtons.First().Click();

        return fakeDevelopers;
    }
    
    [Fact]
    public async Task SubmitForm_NewWorklogEntryWithValidFields_OnCloseAndOnUpdateCallbacksInvoked()
    {
        await SetupFormWithValidValues(false);
        
        await EditFormElement.SubmitAsync();
        
        _mockOnCloseCallback.Verify(x => x(), times: Times.Once);
        _mockOnUpdateCallback.Verify(x => x(), times: Times.Once);
    }

    [Fact]
    public async Task SubmitForm_NewWorklogEntryWithValidFields_ServiceLayerCalledCorrectly()
    {
        var fakeDevelopers = await SetupFormWithValidValues(false);
        
        await EditFormElement.SubmitAsync();
        
        _mockWorklogEntryService.Verify(x => x.CreateWorklogEntryAsync(
            ComponentUnderTest.Instance.Model,
            ActingUser.Id,
            _userStoryTask.Id,
            new List<TaggedWorkInstanceForm> { _taggedWorkInstanceForm },
            fakeDevelopers.First().Id,
            new List<GitlabCommit>()
        ), times: Times.Once);
    }
    
    [Fact]
    public async Task SubmitForm_ExistingWorklogEntryWithValidFields_OnCloseAndOnUpdateCallbacksInvoked()
    {
        await SetupFormWithValidValues(true);
        
        await EditFormElement.SubmitAsync();
        
        _mockOnCloseCallback.Verify(x => x(), times: Times.Once);
        _mockOnUpdateCallback.Verify(x => x(), times: Times.Once);
    }
    
    [Fact]
    public async Task SubmitForm_ExistingWorklogEntryWithValidFields_ServiceLayerCalledCorrectly()
    {
        var fakeDevelopers = await SetupFormWithValidValues(true);
        
        await EditFormElement.SubmitAsync();
        
        _mockWorklogEntryService.Verify(x => x.UpdateWorklogEntryAsync(
            _existingWorklogEntryId,
            ComponentUnderTest.Instance.Model,
            ActingUser.Id
        ), times: Times.Once);
        
        _mockWorklogEntryService.Verify(x => x.UpdatePairUserAsync(
            _existingWorklogEntryId,
            ActingUser.Id,
            fakeDevelopers.First().Id
        ), times: Times.Once);
        
        _mockWorklogEntryService.Verify(x => x.SetTaggedWorkInstancesAsync(
            _existingWorklogEntryId,
            ActingUser.Id,
            new List<TaggedWorkInstanceForm> { _taggedWorkInstanceForm }
        ), times: Times.Once);
        
        _mockWorklogEntryService.Verify(x => x.SetLinkedGitlabCommitsOnWorklogAsync(
            _existingWorklogEntryId,
            ActingUser.Id,
            new List<GitlabCommit>()
        ), times: Times.Once);
    }
    
    [Fact]
    public async Task SubmitForm_ExistingWorklogEntryWithDateAfterSprintStartAndBeforeNow_NoErrorMessageAndCallbacksInvoked()
    {
        await SetupFormWithValidValues(true);
        await SetOccurredDateTimeValue(_sprint.StartDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(0))));

        await EditFormElement.SubmitAsync();

        ComponentUnderTest.FindAll("#start-validation-message").Should().BeEmpty();
        _mockOnCloseCallback.Verify(x => x(), times: Times.Once);
        _mockOnUpdateCallback.Verify(x => x(), times: Times.Once);
    }
       
    [Fact]
    public async Task SubmitForm_ExistingWorklogEntryWithDateBeforeSprintStart_ErrorMessageShownAndNoCallbacksInvoked()
    {
        await SetupFormWithValidValues(true);
        await SetOccurredDateTimeValue(_sprint.StartDate.AddDays(-1).ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(0))));

        await EditFormElement.SubmitAsync();

        ComponentUnderTest.FindAll("#start-validation-message").Count.Should().Be(1);
        
        _mockOnCloseCallback.Verify(x => x(), times: Times.Never);
        _mockOnUpdateCallback.Verify(x => x(), times: Times.Never);
    }
    
    [Fact]
    public async Task SubmitForm_DbUpdateConcurrencyErrorThrown_ErrorMessageShownAndNoCallbacksInvoked()
    {
        _mockWorklogEntryService.Setup(x => 
                x.UpdateWorklogEntryAsync(
                    It.IsAny<long>(), 
                    It.IsAny<WorklogEntryForm>(),
                    It.IsAny<long>())
                ).ThrowsAsync(new DbUpdateConcurrencyException());
        await SetupFormWithValidValues(true);

        GetConcurrencyErrorMessage.Should().Throw<ElementNotFoundException>();
        await EditFormElement.SubmitAsync();

        GetConcurrencyErrorMessage.Should().NotThrow();
        _mockOnCloseCallback.Verify(x => x(), times: Times.Never);
        _mockOnUpdateCallback.Verify(x => x(), times: Times.Never);
    }
}