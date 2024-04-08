using System;
using System.Collections.Generic;
using FluentAssertions;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using Xunit;

namespace ScrumBoard.Tests.Unit.Extensions
{
    public class UserStoryExtentionsTest
    {
        private UserStoryTask _task1 = new UserStoryTask() { Id = 101, Stage = Stage.Todo, Estimate = TimeSpan.FromHours(1) };    
        private UserStoryTask _task2 = new UserStoryTask() { Id = 102, Stage = Stage.Done, Estimate = TimeSpan.FromHours(1) };
        private UserStoryTask _task3 = new UserStoryTask() { Id = 103, Stage = Stage.Deferred, Estimate = TimeSpan.FromHours(1) };
        private UserStoryTask _task4 = new UserStoryTask() { Id = 104, Stage = Stage.Todo, Estimate = TimeSpan.FromHours(1) };
        private UserStoryTask _task5 = new UserStoryTask() { Id = 105, Stage = Stage.Todo, Estimate = TimeSpan.FromHours(1) };  
        private UserStoryTask _task6 = new UserStoryTask() { Id = 105, Stage = Stage.Done, Estimate = TimeSpan.FromHours(1) };    

        [Fact]
        public void GetStoryCompletionRate_ZeroTasks_ReturnsZero()
        {
            UserStory _story = new UserStory() { Tasks = new List<UserStoryTask>()};
            
            double expectedResult = 0;

            _story.GetStoryCompletionRate().Should().Be(expectedResult);
        }

        [Fact]
        public void GetStoryCompletionRate_OnlyTodoTasks_ReturnsZero()
        {
            UserStory _story = new UserStory() { Tasks = new List<UserStoryTask>() { _task1, _task4, _task5 }};
            
            double expectedResult = 0;

            _story.GetStoryCompletionRate().Should().Be(expectedResult);
        }

        [Fact]
        public void GetStoryCompletionRate_OnlyDoneTasks_Returns100()
        {
            UserStory _story = new UserStory() { Tasks = new List<UserStoryTask>() { _task2, _task6 }};
            
            double expectedResult = 100;

            _story.GetStoryCompletionRate().Should().Be(expectedResult);
        }

        [Fact]
        public void GetStoryCompletionRate_AllTasks_ReturnsIgnoringDeferred()
        {
            UserStory _story = new UserStory() { Tasks = new List<UserStoryTask>() { _task1, _task2, _task3, _task4, _task5, _task6 }};
            
            // Two done, one deferred, three todo
            double expectedResult = 40;

            _story.GetStoryCompletionRate().Should().Be(expectedResult);
        }
    }
}
