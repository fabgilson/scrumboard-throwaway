using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.Core.Internal;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Unit.Models.Forms
{
    public class SprintStartFormTest
    {
        private static readonly UserStoryTask _taskWithEstimate = new UserStoryTask() { Estimate = TimeSpan.FromSeconds(10) };
        private static readonly UserStoryTask _taskWithoutEstimate = new UserStoryTask();
        private static readonly UserStory _storyWithEstimatedTasks = new UserStory() { Estimate = 10, Tasks = new List<UserStoryTask>() { _taskWithEstimate } };
        private static readonly UserStory _storyWithoutTask = new UserStory() { Estimate = 10 };
        private static readonly UserStory _storyWithUnestimatedTask = new UserStory() { Estimate = 10, Tasks = new List<UserStoryTask>() { _taskWithoutEstimate } };
        private static readonly UserStory _unestimatedStory = new UserStory() { Tasks = new List<UserStoryTask> { _taskWithEstimate } };

        private List<ValidationResult> ValidateModel(UserStoryStartForm model) {
            List<ValidationResult> results = new();
            ValidationContext context = new ValidationContext(model);
            var result = Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void ValidateStoryStartForm_StoryValid_IsSuccess() {
            UserStoryStartForm story = new(_storyWithEstimatedTasks);

            var result = ValidateModel(story);
            result.Should().BeEmpty();
        }

        [Fact]
        public void ValidateStoryStartForm_StoryNoTasks_IsNotSuccess() {
            var attribute = "Tasks";
            var lengthAttribute = typeof(UserStoryStartForm).GetAttribute<MinLengthAttribute>(attribute);

            UserStoryStartForm story = new(_storyWithoutTask);

            var result = ValidateModel(story);
            result.Should().HaveCount(1);

            var expectedErrorMessage = lengthAttribute.ErrorMessage;
            result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));
        }

        [Fact]
        public void ValidateStoryStartForm_StoryNotEstimated_IsNotSuccess() {
            var attribute = "Estimate";
            var rangeAttribute = typeof(UserStoryStartForm).GetAttribute<RangeAttribute>(attribute);

            UserStoryStartForm story = new(_unestimatedStory);

            var result = ValidateModel(story);
            result.Should().HaveCount(1);
            
            var expectedErrorMessage = rangeAttribute.ErrorMessage;
            result.Should().ContainEquivalentOf(new ValidationResult(expectedErrorMessage, new[] { attribute }));
        }

        [Fact]
        public void ValidateStoryStartForm_TaskNotEstimated_IsNotSuccess() {
            UserStoryStartForm story = new(_storyWithUnestimatedTask);
            
            var result = ValidateModel(story);
            result.Should().HaveCount(1);

            result.Should().ContainEquivalentOf(new ValidationResult("Some tasks are missing estimates", new[] { "Tasks" }));
        }
    }
}
