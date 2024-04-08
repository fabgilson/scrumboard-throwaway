using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bogus;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Services;

namespace ScrumBoard.Tests.Util;

public static class FakeDataGenerator
{
    public const long DefaultUserId = 10000;

    private static long _currentId = 10001;
    public static long NextId => _currentId++;

    public static Project CreateFakeProject(
        string namePrefix = "Fake project",
        IEnumerable<User> developers = null,
        bool includeUserObjectInMembershipEntities = false,
        long? projectId = null
    )
    {
        var id = projectId ?? NextId;
        var projectMemberships = (developers ?? Array.Empty<User>())
            .Select(user => new ProjectUserMembership
            {
                UserId = user.Id,
                User = includeUserObjectInMembershipEntities ? user : null,
                ProjectId = id,
                Role = ProjectRole.Developer
            });

        return new Project
        {
            Id = id,
            Created = DateTime.Now,
            Name = $"{namePrefix} #{id}",
            StartDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
            EndDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10),
            Description = $"Description for new fake project with ID={id}",
            MemberAssociations = projectMemberships.ToList(),
            CreatorId = DefaultUserId
        };
    }

    public static IEnumerable<Project> CreateMultipleFakeProjects(
        int count,
        string namePrefix = "",
        ICollection<User> developersInProjects = null,
        bool includeUserObjectInMembershipEntities = false
    )
    {
        for (var i = 0; i < count; i++)
        {
            yield return CreateFakeProject(
                namePrefix: namePrefix,
                developers: developersInProjects,
                includeUserObjectInMembershipEntities: includeUserObjectInMembershipEntities
            );
        }
    }

    public static Sprint CreateFakeSprint(Project project, SprintStage stage = SprintStage.Created, DateTime? timeStarted = null)
    {
        var id = NextId;
        return new Sprint
        {
            Id = id,
            Created = DateTime.Now,
            Name = $"Sprint with ID={id} for project with name '{project.Name}'",
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = DateOnly.FromDateTime(DateTime.Now).AddDays(5),
            Project = project,
            SprintProjectId = project.Id,
            Stage = stage,
            TimeStarted = timeStarted,
            CreatorId = DefaultUserId
        };
    }

    public static Sprint CreateFakeSprintWithDatabaseProject(Project project, DateOnly? startDate = null, DateOnly? endDate = null,
        SprintStage stage = SprintStage.Created, DateTime? timeStarted = null)
    {
        var id = NextId;
        return new Sprint
        {
            Id = id,
            Created = DateTime.Now,
            Name = $"Sprint with ID={id} for project with name '{project.Name}'",
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Now),
            EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Now).AddDays(5),
            SprintProjectId = project.Id,
            Stage = stage,
            TimeStarted = timeStarted,
            CreatorId = DefaultUserId
        };
    }

    public static UserStory CreateFakeUserStoryWithDatabaseSprint(Sprint sprint)
    {
        var id = NextId;
        return new UserStory
        {
            Id = id,
            StoryGroupId = sprint.Id,
            ProjectId = sprint.SprintProjectId,
            Created = DateTime.Now,
            Name = $"UserStory with ID={id} for sprint with name '{sprint.Name}'",
            Description = new Faker().Lorem.Sentence(),
            CreatorId = DefaultUserId
        };
    }

    public static AcceptanceCriteria CreateAcceptanceCriteria(
        UserStory story = null,
        AcceptanceCriteriaStatus? status = null,
        string reviewComments = null,
        bool storyIsAlreadyInDatabase = false,
        bool generateReviewComments = true
    )
    {
        var faker = new Faker();
        return new AcceptanceCriteria
        {
            Id = NextId,
            UserStory = storyIsAlreadyInDatabase ? null : story,
            UserStoryId = story?.Id ?? default,
            Content = faker.Lorem.Sentence(),
            Status = status,
            ReviewComments = reviewComments ?? (generateReviewComments ? faker.Lorem.Sentence() : null)
        };
    }

    public static IEnumerable<AcceptanceCriteria> CreateMultipleAcceptanceCriteria(int count, bool generateReviewComments = false)
    {
        var faker = new Faker();
        for (var i = 0; i < count; i++)
        {
            yield return CreateAcceptanceCriteria(generateReviewComments: generateReviewComments);
        }
    }

    public static UserStory CreateFakeUserStory(Sprint sprint, bool generateReviewComments = false, Stage stage = Stage.Todo)
    {
        var id = NextId;
        var faker = new Faker();
        return new UserStory
        {
            Id = id,
            StoryGroup = sprint,
            StoryGroupId = sprint.Id,
            ProjectId = sprint.SprintProjectId,
            Created = DateTime.Now,
            Name = $"UserStory with ID={id} for sprint with name '{sprint.Name}'",
            Description = faker.Lorem.Sentence(),
            ReviewComments = generateReviewComments ? faker.Lorem.Sentence() : null,
            CreatorId = DefaultUserId,
            Stage = stage
        };
    }

    public static StandUpMeeting CreateFakeStandUp(Sprint sprint)
    {
        var id = NextId;
        return new StandUpMeeting
        {
            Id = id,
            Created = DateTime.Now,
            Name = $"Stand-up meeting with ID={id}",
            Sprint = sprint,
            CreatorId = DefaultUserId
        };
    }

    public static IEnumerable<StandUpMeeting> CreateMultipleFakeStandUps(int count, Sprint sprint)
    {
        for (var i = 0; i < count; i++) yield return CreateFakeStandUp(sprint);
    }

    public static User CreateFakeUser(
        string firstName = null,
        string lastName = null,
        string ldapUsername = null,
        string email = null
    )
    {
        var faker = new Faker();
        var fakePerson = faker.Person;
        return new User
        {
            Id = NextId,
            FirstName = firstName ?? fakePerson.FirstName,
            LastName = lastName ?? fakePerson.LastName,
            LDAPUsername = ldapUsername ?? fakePerson.UserName,
            Email = email ?? fakePerson.Email
        };
    }

    public static IEnumerable<User> CreateMultipleFakeUsers(int count)
    {
        for (var i = 0; i < count; i++) yield return CreateFakeUser();
    }

    public static WorklogTag CreateWorklogTag(
        string name = null,
        BadgeStyle style = BadgeStyle.Primary
    )
    {
        return new WorklogTag
        {
            Id = NextId,
            Name = name ?? new Faker().Random.Word(),
            Style = style
        };
    }

    public static TaggedWorkInstance CreateFakeTaggedWorkInstance(
        TimeSpan? duration = null,
        long? worklogEntryId = null,
        long? tagId = null,
        string worklogTagName = "Test Worklog Tag"
    )
    {
        return new TaggedWorkInstance
        {
            Duration = duration ?? TimeSpan.FromMinutes(30),
            WorklogEntryId = worklogEntryId ?? default,
            WorklogTagId = tagId ?? default,
            WorklogTag = new WorklogTag { Id = tagId ?? default, Name = worklogTagName }
        };
    }

    public static TaggedWorkInstance CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(
        WorklogTag worklogTag,
        TimeSpan? duration = null
    )
    {
        return new TaggedWorkInstance
        {
            Duration = duration ?? TimeSpan.FromMinutes(new Faker().Random.Int(1, 120)),
            WorklogTagId = worklogTag.Id
        };
    }

    public static TaggedWorkInstance CreateFakeTaggedWorkInstanceForDatabaseWorklogTagAndEntry(
        WorklogTag worklogTag,
        WorklogEntry worklogEntry,
        TimeSpan? duration = null
    )
    {
        return new TaggedWorkInstance
        {
            Duration = duration ?? TimeSpan.FromMinutes(new Faker().Random.Int(1, 120)),
            WorklogTagId = worklogTag.Id,
            WorklogEntryId = worklogEntry.Id
        };
    }


    public static TaggedWorkInstanceForm CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(
        WorklogTag worklogTag,
        TimeSpan? duration = null
    )
    {
        return new TaggedWorkInstanceForm
        {
            Duration = duration ?? TimeSpan.FromMinutes(new Faker().Random.Int(1, 120)),
            WorklogTagId = worklogTag.Id
        };
    }

    public static UserStoryTask CreateFakeTask(UserStory userStory = null)
    {
        return new UserStoryTask
        {
            Id = NextId,
            UserStory = userStory,
            Description = "Test task description",
            CreatorId = DefaultUserId
        };
    }

    public static IEnumerable<UserStoryTask> CreateMultipleFakeTasks(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return CreateFakeTask();
        }
    }

    public static UserStoryTask CreateFakeTaskForDatabaseUserStory(UserStory story)
    {
        var id = NextId;
        return new UserStoryTask
        {
            Id = id,
            UserStoryId = story.Id,
            Name = $"Task with ID={id} for story with ID={story.Id}",
            Description = new Faker().Lorem.Sentence(),
            CreatorId = DefaultUserId
        };
    }

    public static WorklogEntryForm CreateWorklogEntryForm(DateTime? occurred = null, string description = null, User pairUser = null)
    {
        var faker = new Faker();
        return new WorklogEntryForm
        {
            Occurred = occurred ?? DateTime.Now,
            Description = description ?? faker.Lorem.Paragraph(),
            PairUser = pairUser
        };
    }

    public static GitlabCommit CreateFakeGitlabCommit()
    {
        var faker = new Faker();
        var fakePerson = faker.Person;
        return new GitlabCommit
        {
            Id = faker.Random.Hash(),
            Message = faker.Lorem.Paragraph(),
            Title = faker.Lorem.Sentence(),
            AuthoredDate = DateTime.Today,
            AuthorEmail = fakePerson.Email,
            AuthorName = fakePerson.FullName,
            WebUrl = new Uri(faker.Internet.Url())
        };
    }

    public static OverheadEntry CreateFakeOverheadEntry(User user, Sprint sprint, DateTime? occurred = null)
    {
        var faker = new Faker();
        return new OverheadEntry
        {
            Description = faker.Lorem.Sentence(),
            Created = DateTime.Now,
            Occurred = occurred ?? sprint.StartDate.ToDateTime(TimeOnly.MaxValue),
            UserId = user.Id,
            SprintId = sprint.Id,
            Session = new OverheadSession { Name = faker.Lorem.Sentence() },
            DurationTicks = faker.Random.Int()
        };
    }

    public static WorklogEntry CreateFakeWorkLogEntry(User user, Sprint sprint, UserStoryTask task, DateTime? occurred = null)
    {
        var faker = new Faker();
        return new WorklogEntry
        {
            Created = DateTime.Now,
            Description = faker.Lorem.Sentence(),
            Occurred = occurred ?? sprint.StartDate.ToDateTime(TimeOnly.MaxValue),
            UserId = user.Id,
            TaskId = task.Id
        };
    }


    public static WeeklyTimeSpan GenerateEmptyWeeklyTimespan(DateOnly weekStart, Sprint sprint = null)
    {
        return new WeeklyTimeSpan
        {
            WeekStart = weekStart,
            Ticks = 0,
            SprintId = sprint?.Id,
            SprintName = sprint?.Name
        };
    }

    public static IEnumerable<UserStory> CreateMultipleFakeStories(
        int count,
        Sprint sprint,
        int acsPerStory = 0,
        bool generateReviewComments = false,
        bool generateAcceptanceCriteriaReviewComments = false
    )
    {
        for (var i = 0; i < count; i++)
        {
            var story = CreateFakeUserStory(sprint, generateReviewComments: generateReviewComments);
            story.AcceptanceCriterias = new List<AcceptanceCriteria>(
                CreateMultipleAcceptanceCriteria(
                    acsPerStory,
                    generateReviewComments: generateAcceptanceCriteriaReviewComments
                )
            );
            yield return story;
        }
    }

    public static WeeklyReflectionCheckIn CreateWeeklyReflectionCheckIn(Project project, User user, int? isoWeekNum = null, int? year = null,
        bool onlyUseForeignKeys = false, CheckInCompletionStatus completionStatus = CheckInCompletionStatus.NotYetStarted)
    {
        var id = NextId;
        isoWeekNum ??= ISOWeek.GetWeekOfYear(DateTime.Now);
        year ??= ISOWeek.GetYear(DateTime.Now);
        
        return new WeeklyReflectionCheckIn
        {
            Id = id,
            ProjectId = project.Id,
            Project = onlyUseForeignKeys ? null : project,
            UserId = user.Id,
            User = onlyUseForeignKeys ? null : user,
            IsoWeekNumber = isoWeekNum.Value,
            Year = year.Value,
            WhatIDidWell = $"Reflection did well comments for id={id}",
            WhatIDidNotDoWell = $"Reflection did not do well comments for id={id}",
            WhatIWillDoDifferently = $"Reflection will do differently comments for id={id}",
            AnythingElse = $"Reflection anything else comments for id={id}",
            CompletionStatus = completionStatus
        };
    }
}