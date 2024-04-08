using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Extensions;
using Xunit;
using Castle.Core.Internal;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;

namespace ScrumBoard.Tests.Unit.Models.Forms;

public class SprintFormTest
{
    private readonly User _actingUser = new User() {
        Id = 5,
        FirstName = "Tim",
        LastName = "Tam",
    };
    private static readonly string _name = "test name";
    private static readonly Project _project = new Project();
    private static readonly DateOnly _start = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
    private static readonly DateOnly _end = DateOnly.FromDateTime(DateTime.Now.AddDays(10));
    private static readonly UserStory _story = new UserStory() {
        Name = "Test User Story",
        Order = 1
    };
    private static readonly UserStory _anotherStory = new UserStory() {
        Name = "Second Test User Story",
        Order = 2
    };

    private static readonly DateOnly _earliestDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-10);

    private static Sprint _sprint;

    private SprintForm _model = new SprintForm(_earliestDate) {
        Name = _name, 
        StartDateOptional = _start,
        EndDateOptional = _end, 
        Stories = new List<UserStory>() { _story, _anotherStory }
    };

    private static readonly UserStoryTask _taskWithEstimate = new UserStoryTask() { Estimate = TimeSpan.FromSeconds(10) };
    private static readonly UserStoryTask _taskWithoutEstimate = new UserStoryTask();
    private static readonly UserStory _storyWithEstimatedTasks = new UserStory() { Estimate = 10, Tasks = new List<UserStoryTask>() { _taskWithEstimate } };
    private static readonly UserStory _storyWithoutTask = new UserStory() { Estimate = 10 };
    private static readonly UserStory _storyWithUnestimatedTask = new UserStory() { Estimate = 10, Tasks = new List<UserStoryTask>() { _taskWithoutEstimate } };
    private static readonly UserStory _unestimatedStory = new UserStory() { Tasks = new List<UserStoryTask> { _taskWithEstimate } };

    private Sprint _validationSprint;

    public SprintFormTest() {
        // Makes sure that some of the custom TypeConverters required for some ChangelogEntry types are registered
        _sprint = new Sprint() {
            Name = _name, 
            StartDate = _start, 
            EndDate = _end, 
            Stories = new List<UserStory> { _story, _anotherStory },
            Project = _project
        };
        TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));

    }

    private List<ValidationResult> ValidateModel(SprintForm model) {
        List<ValidationResult> results = new();
        ValidationContext context = new ValidationContext(model);
        var result = Validator.TryValidateObject(model, context, results, true);

        if (model.StoryStartForms != null) {
            foreach (UserStoryStartForm form in model.StoryStartForms) {
                ValidationContext storyContext = new ValidationContext(form);
                var storyResult = Validator.TryValidateObject(form, storyContext, results, true);
            }  
        }                       
            
        return results;
    }       

    private void CheckSprintValues() {
        _sprint.Name.Should().Be(_name);
        _sprint.StartDate.Should().Be(_start);
        _sprint.EndDate.Should().Be(_end);
        _sprint.Stories.Should().BeEquivalentTo(new List<UserStory>() { _story, _anotherStory });
    }

    private void CreateValidationSprint(UserStory story) {
        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        List<UserStory> stories = new();
        if (story != null) {
            stories = new List<UserStory>() { story };
        }
        _validationSprint = new() { 
            Name = "Test Sprint",
            Creator = _actingUser, 
            Created = DateTime.Now, 
            StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
            Stage = SprintStage.Started,
            Stories = stories
        };
    }

    [Fact]
    public void ApplyChanges_AllPropertiesSet_ChangesAppliedAndRecorded() {
        _sprint = new Sprint() { Project = _project };

        var changes = _model.ApplyChanges(_actingUser, _sprint);
        changes.Should().HaveCount(5);
        CheckSprintValues();
    }

    [Fact]
    public void ApplyChanges_NoPropertiesChanged_NoChangesAppliedOrRecorded() {
        var changes = _model.ApplyChanges(_actingUser, _sprint);
        changes.Should().BeEmpty();
        CheckSprintValues();
    }

    [Fact]
    public void ApplyChanges_NameChanged_NameChangeAppliedAndRecorded() {
        var initialName = "Initial name";
        _sprint.Name = initialName;

        var changes = _model.ApplyChanges(_actingUser, _sprint);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.SprintChangedId.Should().Be(_sprint.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(Sprint.Name));
        change.FromValueObject.Should().Be(initialName);
        change.ToValueObject.Should().Be(_name);

        CheckSprintValues();
    }

    [Fact]
    public void ApplyChanges_StartDateChanged_StartDateChangeAppliedAndRecorded() {
        var initialStartDate = DateOnly.FromDateTime(DateTime.Today);
        _sprint.StartDate = initialStartDate;

        var changes = _model.ApplyChanges(_actingUser, _sprint);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.SprintChangedId.Should().Be(_sprint.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(Sprint.StartDate));
        change.FromValueObject.Should().Be(initialStartDate);
        change.ToValueObject.Should().Be(_start);

        CheckSprintValues();
    }

    [Fact]
    public void ApplyChanges_EndDateChanged_EndDateChangeAppliedAndRecoreded() {
        var initialEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        _sprint.EndDate = initialEndDate;

        var changes = _model.ApplyChanges(_actingUser, _sprint);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.SprintChangedId.Should().Be(_sprint.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(Sprint.EndDate));
        change.FromValueObject.Should().Be(initialEndDate);
        change.ToValueObject.Should().Be(_end);

        CheckSprintValues();
    }

    [Fact]
    public void ApplyChanges_StoryOrderChanged_StoryOrderAppliedAndRecorded() {
        _sprint.Stories.First().Should().Be(_story);
            
        _model.Stories = new List<UserStory>() { _anotherStory, _story };

        _model.ApplyChanges(_actingUser, _sprint);            

        _sprint.Stories.First().Should().Be(_anotherStory);
    }

    [Fact]
    public void ValidateStartDate_DateInPastButNotChanged_ReturnsSuccess() {
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        _sprint.StartDate = startDate;
        _model = new SprintForm(_earliestDate, _sprint);
        
        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateStartDate_StartDatePriorToCurrentDate_ReturnsSuccess() {
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        _model = new SprintForm(_earliestDate, _sprint);
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateStartDate_StartDateIsCurrentDate_ReturnsSuccess() {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        _model = new SprintForm(_earliestDate, _sprint);
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateStartDate_SprintInCreated_ReturnsSuccess() {
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateEndDate_EndDatePriorToCurrentDate_ReturnsErrorMessage() {
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        _model = new SprintForm(_earliestDate, _sprint);
        _model.EndDateOptional = endDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().BeEquivalentTo(new ValidationResult("Must be in the future", new[] { context.MemberName }));
    }

    [Fact]
    public void ValidateEndDate_EndDatePriorToCurrentDateAndIsNewSprint_ReturnsErrorMessage() {
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        _model.EndDateOptional = endDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().BeEquivalentTo(new ValidationResult("Must be in the future", new[] { context.MemberName }));
    }

    [Fact]
    public void ValidateEndDate_DateInPastButNotChangedAndNotBeforeStartDAte_ReturnsSuccess() {
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        _sprint.StartDate = endDate.AddDays(-1);
        _sprint.EndDate = endDate;
        _model = new SprintForm(_earliestDate, _sprint);
        
        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateEndDate_DateBeforeStartDateAndNotInPast_ReturnsErrorMessage() {
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        _sprint.StartDate = endDate.AddDays(1);
        _sprint.EndDate = endDate;
        _model = new SprintForm(_earliestDate, _sprint);
        
        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().BeEquivalentTo(new ValidationResult("Cannot be before start date", new[] { context.MemberName }));
    }

    [Theory]
    [InlineData(SprintStage.Started)]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public void ValidateStartDate_GivenSprintState_ReturnsErrorMessage(SprintStage stage) {
        _sprint.Stage = stage;
        _model = new SprintForm(_earliestDate, _sprint);

        var startDate = _sprint.StartDate.AddDays(1);
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().BeEquivalentTo(new ValidationResult("Cannot be edited after the sprint has started", new[] { context.MemberName }));
    }

    [Theory]
    [InlineData(SprintStage.Started)]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public void ValidateStartDate_GivenSprintStateButDateNotChanged_ReturnsSuccess(SprintStage stage) {
        _sprint.Stage = stage;
        _model = new SprintForm(_earliestDate, _sprint);

        var startDate = _sprint.StartDate;
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateStartDate_SprintIsNull_ReturnsSuccess() {
        _model = new SprintForm(_earliestDate);

        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        _model.StartDateOptional = startDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(startDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateEndDate_SprintIsNull_ReturnsSuccess() {
        _model = new SprintForm(_earliestDate);

        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        _model.EndDateOptional = endDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateStartDate(endDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData(SprintStage.Created)]
    [InlineData(SprintStage.Started)]
    public void ValidateEndDate_GivenSprintState_ReturnsSuccess(SprintStage stage) {
        _sprint.Stage = stage;
        _model = new SprintForm(_earliestDate, _sprint);

        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5));
        _model.EndDateOptional = endDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public void ValidateEndDate_GivenSprintState_ReturnsErrorMessage(SprintStage stage) {
        _sprint.Stage = stage;
        _model = new SprintForm(_earliestDate, _sprint);

        var endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5));
        _model.EndDateOptional = endDate;

        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().BeEquivalentTo(new ValidationResult($"Cannot be edited after the sprint has finished", new[] { context.MemberName }));
    }

    [Theory]
    [InlineData(SprintStage.Started)]
    [InlineData(SprintStage.ReadyToReview)]
    [InlineData(SprintStage.InReview)]
    [InlineData(SprintStage.Reviewed)]
    [InlineData(SprintStage.Closed)]
    public void ValidateEndDate_GivenSprintStateButDateNotChanged_ReturnsSuccess(SprintStage stage) {
        _sprint.Stage = stage;
        var endDate = _sprint.EndDate;
        _model = new SprintForm(_earliestDate, _sprint);
        
        var context = new ValidationContext(_model);

        var result = SprintForm.ValidateEndDate(endDate, context);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateSprintForm_SprintStarted_StoryValid_IsSuccess() {
        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(_storyWithEstimatedTasks);

        SprintForm sprintForm = new(earliestDate, _validationSprint);

        var result = ValidateModel(sprintForm);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSprintForm__SprintStarted_StoryNoTasks_IsNotSuccess() {
        var attribute = "Tasks";          

        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(_storyWithoutTask);

        SprintForm sprintForm = new(earliestDate, _validationSprint);          

        var result = ValidateModel(sprintForm);
        result.Should().HaveCount(1);

        var expectedErrorMessage = "Must have at least one task";
        result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));
    }

    [Fact]
    public void ValidateSprintForm_SprintStarted_StoryNotEstimated_IsNotSuccess() {
        var attribute = "Estimate";          

        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(_unestimatedStory);

        SprintForm sprintForm = new(earliestDate, _validationSprint);           

        var result = ValidateModel(sprintForm);
        result.Should().HaveCount(1);           
           
        var expectedErrorMessage = "Story must have estimate";
        result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));           
    }

    [Fact]
    public void ValidateSprintForm_SprintStarted_TaskNotEstimated_IsNotSuccess() {
        var attribute = "Tasks"; 

        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(_storyWithUnestimatedTask);
        SprintForm sprintForm = new(earliestDate, _validationSprint);
            
        var result = ValidateModel(sprintForm);
        result.Should().HaveCount(1);

        var expectedErrorMessage = "Some tasks are missing estimates";
        result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));
    }

    [Fact]
    public void ValidateSprintForm_SprintStarted_NoStories_IsNotSuccess() {
        var attribute = "StoryStartForms"; 

        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(null);
        SprintForm sprintForm = new(earliestDate, _validationSprint);
            
        var result = ValidateModel(sprintForm);
        result.Should().HaveCount(1);

        var expectedErrorMessage = "Must have at least one story";
        result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));
    }

    [Fact]
    public void ValidateSprintForm_SprintNotStarted_NoStories_IsSuccess() {       

        DateOnly earliestDate = DateOnly.Parse("2019-01-01");
        CreateValidationSprint(null);
        SprintForm sprintForm = new(earliestDate);
        sprintForm.Name = "Test sprint name";
        sprintForm.StartDateOptional = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        sprintForm.EndDateOptional = DateOnly.FromDateTime(DateTime.Now.AddDays(4));
            
        var result = ValidateModel(sprintForm);
        result.Should().BeEmpty();
       
    }
}