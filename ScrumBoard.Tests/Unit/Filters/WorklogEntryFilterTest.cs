using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Filters;
using ScrumBoard.Models.Entities;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Unit.Filters
{
    public class WorklogEntryFilterTest
    {
        private WorklogEntryFilter _worklogEntryFilter = new();

        private static readonly UserStory _story = new UserStory {  };

        private static readonly User _creator = new User { Id = 101 };
        
        private static readonly User _assigneeOne = new User() { Id = 1 };
        
        private static readonly User _assigneeTwo = new User() { Id = 2 };

        private static readonly User _assigneeThree = new User() { Id = 3 };

        private static readonly User _assigneeFour = new User() { Id = 4 };

        private static readonly DateTime _created = DateTime.Now.AddDays(-10);
        
        private static readonly WorklogTag Chore    = new() { Id = 11, Name = "Chore"    };
        private static readonly WorklogTag Feature  = new() { Id = 12, Name = "Feature"  };
        private static readonly WorklogTag Document = new() { Id = 13, Name = "Document" };
        private static readonly WorklogTag Fix      = new() { Id = 14, Name = "Fix"      };
        private static readonly WorklogTag Test     = new() { Id = 15, Name = "Test"     };
        
        private static readonly UserStoryTaskTag NonStory = new() { Id = 21, Name = "Non Story" };
        private static readonly UserStoryTaskTag Backend  = new() { Id = 22, Name = "Backend"   };
        private static readonly UserStoryTaskTag Database = new() { Id = 23, Name = "Database"  };
        private static readonly UserStoryTaskTag Frontend = new() { Id = 24, Name = "Frontend"  };

        private readonly List<WorklogTag> _allWorklogTags = new() { Chore, Feature, Fix, Document, Test };
        private readonly List<UserStoryTaskTag> _allTaskTags = new() { NonStory, Backend, Database, Frontend };

        private UserStoryTask _taskOne = new() {
            Name = "First task",
            Description = "Is first task",
            OriginalEstimate = TimeSpan.FromHours(2),
            Estimate = TimeSpan.FromHours(3),
            Created = _created,
            Creator = _creator,
            UserStory = _story,
            Tags = new List<UserStoryTaskTag>() { NonStory },
        };

        private UserStoryTask _taskTwo = new() {
            Name = "Second task",
                Description = "Is second task",
            OriginalEstimate = TimeSpan.FromHours(4),
            Estimate = TimeSpan.FromHours(4),
            Created = _created,
            Creator = _creator,
            UserStory = _story,
            Tags = new List<UserStoryTaskTag>() { Backend, Database },
        };

        private UserStoryTask _taskThree = new UserStoryTask() {
            Name = "Third task",
            Description = "Is third task",
            OriginalEstimate = TimeSpan.FromHours(4),
            Estimate = TimeSpan.FromHours(4),
            Created = _created,
            Creator = _creator,
            UserStory = _story,
            Tags = new List<UserStoryTaskTag>() { Frontend },
        };

        private UserStoryTask _taskFour = new UserStoryTask() {
            Name = "Fourth task",
            Description = "Is fourth task",
            OriginalEstimate = TimeSpan.FromHours(4),
            Estimate = TimeSpan.FromHours(4),
            Created = _created,
            Creator = _creator,
            UserStory = _story,
            Tags = new List<UserStoryTaskTag>() { NonStory },
        };

        private List<WorklogEntry> _worklogEntries;

        public WorklogEntryFilterTest() {
            _worklogEntryFilter = new WorklogEntryFilter();
            _worklogEntries = new List<WorklogEntry> {
                new WorklogEntry { 
                    Id = 1,
                    Task = _taskOne,
                    Created = _created.AddDays(1), 
                    Occurred = _created.AddDays(1),
                    User = _assigneeOne,
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.5), tagId: Fix.Id) }, 
                }, 
                new WorklogEntry {
                    Id = 2,
                    Task = _taskTwo,
                    Created = _created.AddDays(1), 
                    Occurred = _created.AddDays(2),
                    User = _assigneeTwo,
                    PairUser = _assigneeFour,
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.5), tagId: Document.Id) }, 
                }, 
                new WorklogEntry {
                    Id = 3,
                    Task = _taskOne, 
                    Created = _created.AddDays(1), 
                    Occurred = _created.AddDays(3),
                    User = _assigneeOne, 
                    TaggedWorkInstances = new []
                    {
                        FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.25), tagId: Chore.Id),
                        FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.25), tagId: Fix.Id)
                    }, 
                },
                new WorklogEntry {
                    Id = 4,
                    Task = _taskOne, 
                    Created = _created.AddDays(1), 
                    Occurred = _created.AddDays(4),
                    User = _assigneeThree, 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.5), tagId: Feature.Id) }, 
                }, 
                new WorklogEntry {
                    Id = 5,
                    Task = _taskThree, 
                    Created = _created.AddDays(1),
                    Occurred = _created.AddDays(5),
                    User = _assigneeFour, 
                    PairUser = _assigneeThree, 
                    TaggedWorkInstances = new [] { FakeDataGenerator.CreateFakeTaggedWorkInstance(TimeSpan.FromHours(0.5), tagId: Test.Id) }, 
                }
            };
        }

        [Fact]
        public void FilterWorklogs_NoFilters_AllWorklogsReturned()
        {
            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.Should().BeEquivalentTo(_worklogEntries);
        }

        [Fact]
        public void FilterWorklogs_FilterByAssignees_GivenAssigneesReturned() {
            List<User> expected = new List<User> { _assigneeOne, _assigneeTwo };
            _worklogEntryFilter.AssigneeFilter = expected;
            
            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.All(e => expected.Contains(e.User) || expected.Contains(e.PairUser)).Should().BeTrue();
        }

        [Fact]
        public void FilterWorklogs_FilterByUsersIncludingPaired_FilteredLogsIncludeUserAsPair()
        {
            List<User> assignees = new List<User> { _assigneeFour };
            _worklogEntryFilter.AssigneeFilter = assignees;
            _worklogEntryFilter.IncludePairAssigneeEnabled = true;

            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.All(e => assignees.Contains(e.User) || assignees.Contains(e.PairUser)).Should().BeTrue();
        }

        [Fact]
        public void FilterWorklogs_FilterByUsersNotIncludingPaired_FilteredLogsOnlyContainsUserAssigned()
        {
            List<User> assignees = new List<User> { _assigneeFour };
            _worklogEntryFilter.AssigneeFilter = assignees;
            _worklogEntryFilter.IncludePairAssigneeEnabled = false;

            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.All(e => assignees.Contains(e.User) && !assignees.Contains(e.PairUser)).Should().BeTrue();
        }

        [Theory]
        [InlineData("Non Story")]
        [InlineData("Backend")]
        [InlineData("Frontend", "Non Story")]
        [InlineData("Non Story", "Backend", "Database", "Frontend")]
        public void FilterWorklogs_FilterByTaskTags_FilteredLogsContainGivenTags(params string[] tagNames) {
            List<UserStoryTaskTag> expected = tagNames.Select(tagName => _allTaskTags.First(tag => tag.Name == tagName)).ToList();
            _worklogEntryFilter.TaskTagsFilter = expected;
            
            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.Should().OnlyContain(e => e.Task.Tags.Intersect(expected).Any());
        }

        [Theory]
        [InlineData("Chore")]
        [InlineData("Document")]
        [InlineData("Feature", "Test")]
        [InlineData("Chore", "Document", "Feature", "Fix", "Test")]
        public void FilterWorklogs_FilterByWorklogTags_FilteredLogsContainGivenTags(params string[] tagNames) {
            var expectedTags = tagNames.Select(tagName => _allWorklogTags.First(tag => tag.Name == tagName)).ToList();
            var expectedTagIds = expectedTags.Select(tag => tag.Id).ToList();
            _worklogEntryFilter.WorklogTagsFilter = expectedTags;
            
            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.Should().OnlyContain(e => e.TaggedWorkInstances.Select(x => x.WorklogTagId).Intersect(expectedTagIds).Any());
        }

        [Fact]
        public void FilterWorklogs_FilterByAssigneeAndTags_FilteredWorklogsContainAssigneeAndTags() {
            List<User> assignees = new List<User> { _assigneeThree };
            _worklogEntryFilter.WorklogTagsFilter = new List<WorklogTag>() { Test };
            _worklogEntryFilter.AssigneeFilter = assignees;
            _worklogEntryFilter.IncludePairAssigneeEnabled = true;

            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.First().Id.Should().Be(5);
        }

        [Theory]
        [InlineData(1, 2, 2)]
        [InlineData(0, 2, 2)]
        [InlineData(2, 2, 1)]
        [InlineData(1, 5, 5)]
        [InlineData(3, 4, 2)]
        public void FilterWorklogs_FilterByDateRange_FilteredWorklogsOnlyInDateRange(int dateRangeStart, int dateRangeEnd, int expectedEntryCount) {
            List<User> expected = new List<User> { _assigneeOne, _assigneeTwo };
            _worklogEntryFilter.DateRangeFilterEnabled = true;
            var startDate = _created.AddDays(dateRangeStart);
            var endDate = _created.AddDays(dateRangeEnd);
            _worklogEntryFilter.DateRangeStart = DateOnly.FromDateTime(startDate);
            _worklogEntryFilter.DateRangeEnd = DateOnly.FromDateTime(endDate);
            
            var filter = _worklogEntryFilter.Predicate.Compile();
            List<WorklogEntry> filteredEntries = _worklogEntries.Where(filter).ToList();
            filteredEntries.Should().HaveCount(expectedEntryCount);
            filteredEntries.Should().OnlyContain(e => e.Occurred >= startDate && e.Occurred <= endDate);
        }
    }
}