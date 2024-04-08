using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class WorklogEntryServiceTest : BaseIntegrationTestFixture
{
    private readonly IWorklogEntryService _worklogEntryService;

    private User _user, _secondUser, _thirdUser;
    private UserStoryTask _userStoryTask;
    private WorklogTag _featureTag, _testTag, _worklogTag, _secondWorklogTag;
    private WorklogEntryForm _worklogEntry;
    private GitlabCommit _firstCommit, _secondCommit, _commitNotYetInDb;
    private Sprint _sprint;
    
    public WorklogEntryServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _worklogEntryService = ServiceProvider.GetRequiredService<IWorklogEntryService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        _user = FakeDataGenerator.CreateFakeUser();
        _secondUser = FakeDataGenerator.CreateFakeUser();
        _thirdUser = FakeDataGenerator.CreateFakeUser();
        await dbContext.Users.AddRangeAsync(_user, _secondUser, _thirdUser);

        _firstCommit = FakeDataGenerator.CreateFakeGitlabCommit();
        _secondCommit = FakeDataGenerator.CreateFakeGitlabCommit();
        _commitNotYetInDb = FakeDataGenerator.CreateFakeGitlabCommit();
        await dbContext.GitlabCommits.AddRangeAsync(_firstCommit, _secondCommit);

        _featureTag = FakeDataGenerator.CreateWorklogTag(name: "Feature");
        _testTag = FakeDataGenerator.CreateWorklogTag(name: "Test");
        await dbContext.WorklogTags.AddRangeAsync(_featureTag, _testTag);

        var project = FakeDataGenerator.CreateFakeProject(developers: new []{ _user });
        await dbContext.Projects.AddAsync(project);

        _sprint = FakeDataGenerator.CreateFakeSprintWithDatabaseProject(project);
        await dbContext.Sprints.AddAsync(_sprint);

        var userStory = FakeDataGenerator.CreateFakeUserStoryWithDatabaseSprint(_sprint);
        await dbContext.UserStories.AddAsync(userStory);
        
        _userStoryTask = FakeDataGenerator.CreateFakeTaskForDatabaseUserStory(userStory);
        await dbContext.UserStoryTasks.AddAsync(_userStoryTask);
        
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a sample worklog entry directly into the DB, bypassing the service layer, so no validation or changelog
    /// generation occurs. Good for testing updating logic in isolation.
    /// </summary>
    /// <returns>The sample worklog entry created</returns>
    private async Task<WorklogEntry> CreateExistingWorklogEntryInDb(
        DateTime? occurred=null, 
        long? pairId=null, 
        IEnumerable<GitlabCommit> existingLinkedCommits=null
    ) {
        var worklogForm = FakeDataGenerator.CreateWorklogEntryForm(occurred: occurred);
        var worklog = new WorklogEntry
        {
            UserId = _user.Id,
            PairUserId = pairId,
            Description = worklogForm.Description,
            Occurred = worklogForm.Occurred,
            TaskId = _userStoryTask.Id,
            TaggedWorkInstances = [ FakeDataGenerator.CreateFakeTaggedWorkInstanceForDatabaseWorklogTag(_featureTag) ]
        };
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await context.WorklogEntries.AddAsync(worklog);
        await context.SaveChangesAsync();
        await context.WorklogCommitJoins.AddRangeAsync((existingLinkedCommits ?? [])
            .Select(x => new WorklogCommitJoin { CommitId = x.Id, EntryId = worklog.Id }));
        return worklog;
    }

    private Task UpdateExistingWorklogEntry(WorklogEntry existingWorklog, string newDescription=null, DateTime? newOccurred=null)
    {
        var form = new WorklogEntryForm(existingWorklog, existingWorklog.Occurred)
        {
            Description = newDescription ?? existingWorklog.Description,
            Occurred = newOccurred ?? existingWorklog.Occurred
        };
        return _worklogEntryService.UpdateWorklogEntryAsync(existingWorklog.Id, form, _user.Id);
    }
    
    private Task AddWorklogToDefaultTask(params TaggedWorkInstanceForm[] taggedWorkInstanceForms)
    {
        return AddWorklogToDefaultTask(new List<GitlabCommit>(), taggedWorkInstanceForms: taggedWorkInstanceForms);
    }
    
    private async Task AddWorklogToDefaultTask(ICollection<GitlabCommit> linkedCommits, WorklogEntryForm worklogEntry = null,
        params TaggedWorkInstanceForm[] taggedWorkInstanceForms)
    {
        var worklogEntryForm = worklogEntry ?? FakeDataGenerator.CreateWorklogEntryForm();
        await _worklogEntryService.CreateWorklogEntryAsync(
            worklogEntryForm, _user.Id, _userStoryTask.Id, taggedWorkInstanceForms,
            linkedCommits: linkedCommits
        );
    }
    
    [Fact]
    public async Task CreateWorklogEntry_EmptyTaggedWorkInstances_ExceptionThrown()
    {
        var action = async () => await AddWorklogToDefaultTask();
        (await action.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("At least one taggedWorkInstanceForm must be provided");
    }
    
    [Fact]
    public async Task CreateWorklogEntry_NullTaggedWorkInstances_ExceptionThrown()
    {
        var worklogEntryForm = FakeDataGenerator.CreateWorklogEntryForm();
        var action = async () => await _worklogEntryService.CreateWorklogEntryAsync(
            worklogEntryForm, _user.Id, _userStoryTask.Id, null
        );
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
    
    [Fact]
    public async Task CreateWorklogEntry_SingleWorkInstance_WorklogEntryAndWorkInstanceSaved()
    {
        await AddWorklogToDefaultTask(FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag));
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogEntries.Should().ContainSingle();
        context.TaggedWorkInstances.Should().ContainSingle(x => x.WorklogTagId == _featureTag.Id);
    }
    
    [Fact]
    public async Task CreateWorklogEntry_MultipleWorkInstancesDifferentTags_MultipleWorkInstancesSaved()
    {
        await AddWorklogToDefaultTask(
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogEntries.Should().ContainSingle();
        context.TaggedWorkInstances.Should().Contain(x => x.WorklogTagId == _featureTag.Id);
        context.TaggedWorkInstances.Should().Contain(x => x.WorklogTagId == _testTag.Id);
    }

        
    [Fact]
    public async Task CreateWorklogEntry_MultipleWorkInstancesSameTag_ExceptionThrown()
    {
        var action = async () => await AddWorklogToDefaultTask(
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );
        (await action.Should().ThrowAsync<ArgumentException>())
            .WithMessage("No more than one TaggedWorkInstance may be supplied for any WorklogTag");
    }

    [Fact]
    public async Task CreateWorklogEntry_SingleWorkInstance_SingleWorklogEntryChangelogCreated()
    {
        await AddWorklogToDefaultTask(FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag));
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var changelog = await context.WorklogEntryChangelogEntries.FirstAsync();
        var worklog = await context.WorklogEntries.FirstAsync();

        changelog.WorklogEntryChangedId.Should().Be(worklog.Id);
        changelog.Type.Should().Be(ChangeType.Create);
    }
    
    [Fact]
    public async Task CreateWorklogEntry_MultipleWorkInstances_SingleWorklogEntryChangelogCreated()
    {
        await AddWorklogToDefaultTask(
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var changelog = await context.WorklogEntryChangelogEntries.FirstAsync();
        var worklog = await context.WorklogEntries.FirstAsync();

        changelog.WorklogEntryChangedId.Should().Be(worklog.Id);
        changelog.Type.Should().Be(ChangeType.Create);
    }
    
    [Fact]
    public async Task CreateWorklogEntry_SingleWorkInstance_SingleWorkInstanceChangelogEntryCreated()
    {
        await AddWorklogToDefaultTask(FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag));
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var changelog = await context.TaggedWorkInstanceChangelogEntries.FirstAsync();
        var taggedWorkInstance = await context.TaggedWorkInstances.FirstAsync();

        changelog.WorklogTagId.Should().Be(_featureTag.Id);
        changelog.TaggedWorkInstanceId.Should().NotBeNull();
        changelog.Type.Should().Be(ChangeType.Create);
        changelog.FromValue.Should().BeNull();
        changelog.ToValue.Should().Be(JsonSerializer.Serialize(taggedWorkInstance));
    }
    
    [Fact]
    public async Task CreateWorklogEntry_MultipleWorkInstances_MultipleWorkInstanceChangelogEntriesCreated()
    {
        await AddWorklogToDefaultTask(
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var featureChangelog = await context.TaggedWorkInstanceChangelogEntries.FirstAsync(x => x.WorklogTagId == _featureTag.Id);
        var testChangelog = await context.TaggedWorkInstanceChangelogEntries.FirstAsync(x => x.WorklogTagId == _testTag.Id);
        
        var featureWorkInstance = await context.TaggedWorkInstances.FirstAsync(x => x.WorklogTagId == _featureTag.Id);
        var testWorkInstance = await context.TaggedWorkInstances.FirstAsync(x => x.WorklogTagId == _testTag.Id);
        
        featureChangelog.WorklogTagId.Should().Be(_featureTag.Id);
        featureChangelog.TaggedWorkInstanceId.Should().NotBeNull();
        featureChangelog.Type.Should().Be(ChangeType.Create);
        featureChangelog.FromValue.Should().BeNull();
        featureChangelog.ToValue.Should().Be(JsonSerializer.Serialize(featureWorkInstance));
        
        testChangelog.WorklogTagId.Should().Be(_testTag.Id);
        testChangelog.TaggedWorkInstanceId.Should().NotBeNull();
        testChangelog.Type.Should().Be(ChangeType.Create);
        testChangelog.FromValue.Should().BeNull();
        testChangelog.ToValue.Should().Be(JsonSerializer.Serialize(testWorkInstance));
    }
    
    [Fact]
    public async Task SetTaggedWorkInstances_NoWorklogForGivenId_ExceptionThrown()
    {
        var action = async () => await _worklogEntryService.SetTaggedWorkInstancesAsync(
            10000000, _user.Id, new List<TaggedWorkInstanceForm>());
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetTaggedWorkInstances_OverrideExistingWorkInstance_UpdateChangelogCreated()
    {
        var featureWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag);
        var initialDuration = featureWorkInstance.Duration;
        var testWorkInstance = FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag);
        await AddWorklogToDefaultTask(featureWorkInstance, testWorkInstance);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.SingleAsync();
        featureWorkInstance.Duration = initialDuration.Add(TimeSpan.FromHours(1));
        await _worklogEntryService.SetTaggedWorkInstancesAsync(worklog.Id, _user.Id, [ featureWorkInstance, testWorkInstance ]);

        var changelog = await context.TaggedWorkInstanceChangelogEntries
            .SingleAsync(x => x.Type == ChangeType.Update);
        changelog.WorklogTagId.Should().Be(_featureTag.Id);
        ((TaggedWorkInstance)changelog.FromValueObject).Duration.Should().Be(initialDuration);
        ((TaggedWorkInstance)changelog.ToValueObject).Duration.Should().Be(initialDuration.Add(TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task CreateWorklogEntry_SingleCommitAlreadyInDbLinked_CommitJoinSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();
        context.WorklogCommitJoins.Should().ContainSingle(x => 
            x.CommitId == _firstCommit.Id && x.EntryId == worklog.Id
        );
    }    
    
    [Fact]
    public async Task CreateWorklogEntry_SingleCommitNotYetInDbLinked_CommitJoinSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _commitNotYetInDb ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();
        context.WorklogCommitJoins.Should().ContainSingle(x => 
            x.CommitId == _commitNotYetInDb.Id && x.EntryId == worklog.Id
        );
    }
    
    [Fact]
    public async Task CreateWorklogEntryWithMultipleCommitsLinked_AllCommitsAlreadyInDb_CommitJoinsSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits:[ _firstCommit, _secondCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();
        context.WorklogCommitJoins.Should().HaveCount(2);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _firstCommit.Id && x.EntryId == worklog.Id);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _secondCommit.Id && x.EntryId == worklog.Id);
    }
        
    [Fact]
    public async Task CreateWorklogEntryWithMultipleCommitsLinked_NotAllCommitsAlreadyInDb_CommitJoinsSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits:[ _firstCommit, _commitNotYetInDb ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();
        context.WorklogCommitJoins.Should().HaveCount(2);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _firstCommit.Id && x.EntryId == worklog.Id);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _commitNotYetInDb.Id && x.EntryId == worklog.Id);
    }
    
    [Fact]
    public async Task CreateWorklogEntry_SingleCommitLinked_SingleCommitChangelogSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();
        
        context.WorklogEntryCommitChangelogEntries.Should().ContainSingle(x => 
            x.CommitChangedId == _firstCommit.Id 
            && x.Type == ChangeType.Create 
            && x.WorklogEntryChangedId == worklog.Id
        );
    }
    
    [Fact]
    public async Task CreateWorklogEntry_MultipleCommitsLinked_MultipleCommitChangelogsSaved()
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit, _secondCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag)
        );

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var worklog = await context.WorklogEntries.FirstAsync();

        context.WorklogEntryCommitChangelogEntries.Should().HaveCount(2);
        context.WorklogEntryCommitChangelogEntries.Should().Contain(x => 
            x.CommitChangedId == _firstCommit.Id 
            && x.Type == ChangeType.Create 
            && x.WorklogEntryChangedId == worklog.Id
        );
        context.WorklogEntryCommitChangelogEntries.Should().Contain(x => 
            x.CommitChangedId == _secondCommit.Id 
            && x.Type == ChangeType.Create 
            && x.WorklogEntryChangedId == worklog.Id
        );
    }

    [Fact]
    public async Task SetLinkedGitlabCommits_NoWorklogForGivenId_ExceptionThrown()
    {
        var action = async () => await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(
            10000000, _user.Id, new [] { _firstCommit });
        await action.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task SetLinkedGitlabCommits_AddingNewCommitAlreadyInDb_NewCommitLinkedSuccessfully()
    {
        var existingWorklogWithoutCommits = await CreateExistingWorklogEntryInDb();
        await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(
            existingWorklogWithoutCommits.Id, 
            _user.Id, 
            [_firstCommit]
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogCommitJoins.Should().ContainSingle(x => 
            x.CommitId == _firstCommit.Id && x.EntryId == existingWorklogWithoutCommits.Id
        );
    }
    
    [Fact]
    public async Task SetLinkedGitlabCommits_AddingNewCommitNotAlreadyInDb_NewCommitLinkedSuccessfully()
    {
        var existingWorklogWithoutCommits = await CreateExistingWorklogEntryInDb();
        await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(
            existingWorklogWithoutCommits.Id, 
            _user.Id, 
            [_commitNotYetInDb]
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogCommitJoins.Should().ContainSingle(x => 
            x.CommitId == _commitNotYetInDb.Id && x.EntryId == existingWorklogWithoutCommits.Id
        );
    }
    
    [Fact]
    public async Task SetLinkedGitlabCommits_ReplacingAlreadyLinkedCommitWithNewOne_OnlyOneLinkedCommit()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(existingLinkedCommits: [_firstCommit]);
        await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(existingWorklog.Id, _user.Id, [_commitNotYetInDb]);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogCommitJoins.Should().ContainSingle(x => 
            x.CommitId == _commitNotYetInDb.Id && x.EntryId == existingWorklog.Id
        );
    }
    
    [Fact]
    public async Task SetLinkedGitlabCommits_ReplacingAlreadyLinkedCommitWithEmptyList_NoLinkedCommits()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(existingLinkedCommits: [_firstCommit]);
        await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(existingWorklog.Id, _user.Id, []);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogCommitJoins.Where(x => x.EntryId == existingWorklog.Id).Should().BeEmpty();
    }
    
    [Fact]
    public async Task SetLinkedGitlabCommits_AddingASecondCommitToOneExisting_BothCommitsLinked()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(existingLinkedCommits: [_firstCommit]);
        await _worklogEntryService.SetLinkedGitlabCommitsOnWorklogAsync(existingWorklog.Id, _user.Id, [_firstCommit, _secondCommit]);
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogCommitJoins.Should().HaveCount(2);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _firstCommit.Id && x.EntryId == existingWorklog.Id);
        context.WorklogCommitJoins.Should().Contain(x => x.CommitId == _secondCommit.Id && x.EntryId == existingWorklog.Id);
    }

    [Fact]
    public async Task UpdateWorklogEntry_NoWorklogForGivenId_ExceptionThrown()
    {
        var action = async () => await _worklogEntryService.UpdateWorklogEntryAsync(
            10000000, FakeDataGenerator.CreateWorklogEntryForm(), _user.Id);
        await action.Should().ThrowAsync<ArgumentException>();
    }
    

   
    public static TheoryData<string, DateTime?> UpdateWorklogEntryData =>
        new ()
        {
            { "Some new description", null },
            { null, DateTime.Now.Date.AddHours(9) },
            { "Some new description and a new occurred", DateTime.Now.Date.AddHours(9) }
        };

    public static TheoryData<string> WorklogTagNames => 
        new () { "Feature", "Test", "TestManual", "Fix", "Chore", "Refactor", "Reengineer", "Spike", "Review", "Document" };


    [Theory]
    [MemberData(nameof(UpdateWorklogEntryData))]
    public async Task UpdateWorklogEntry_UpdateFields_ValuesUpdated(string newDescription, DateTime? newOccurred)
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb();
        await UpdateExistingWorklogEntry(existingWorklog, newDescription: newDescription, newOccurred: newOccurred);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var updatedWorklog = await context.WorklogEntries.FirstAsync(x => x.Id == existingWorklog.Id);

        updatedWorklog.Description.Should().Be(newDescription ?? existingWorklog.Description);
        updatedWorklog.Occurred.Should().Be(newOccurred ?? existingWorklog.Occurred);
    }
    
    [Theory]
    [MemberData(nameof(UpdateWorklogEntryData))]
    public async Task UpdateWorklogEntry_UpdateFields_ChangelogsCreated(string newDescription, DateTime? newOccurred)
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb();

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.WorklogEntryChangelogEntries.Should().BeEmpty();
        
        await UpdateExistingWorklogEntry(existingWorklog, newDescription: newDescription, newOccurred: newOccurred);

        if (newDescription is not null)
        {
            var changelog = context.WorklogEntryChangelogEntries.ToList()
                .First(x => x.FieldChangedName == nameof(existingWorklog.Description));
            changelog.Type.Should().Be(ChangeType.Update);
            ((string)changelog.FromValueObject).Should().Be(existingWorklog.Description);
            ((string)changelog.ToValueObject).Should().Be(newDescription);
        }
    
        if (newOccurred is not null)
        {
            var changelog = context.WorklogEntryChangelogEntries.ToList()
                .First(x => x.FieldChangedName == nameof(existingWorklog.Occurred));
            changelog.Type.Should().Be(ChangeType.Update);
            // We truncate down to minutes when saving, so allow up to 60 seconds deviation
            ((DateTime)changelog.FromValueObject).Should().BeCloseTo(existingWorklog.Occurred, TimeSpan.FromSeconds(60));
            ((DateTime)changelog.ToValueObject).Should().BeCloseTo(newOccurred.Value, TimeSpan.FromSeconds(60));
        }
    }
    
    [Fact]
    public async Task UpdatePairUser_NoWorklogForGivenId_ExceptionThrown()
    {
        var action = async () => await _worklogEntryService.UpdatePairUserAsync(
            10000000, _user.Id, _secondUser.Id);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPairUserOnWorklogEntry_NoExistingPairUser_ValueUpdated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb();
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, _secondUser.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var updated = await context.WorklogEntries.SingleAsync(x => x.Id == existingWorklog.Id);

        updated.PairUserId.Should().Be(_secondUser.Id);
    }
    
    [Fact]
    public async Task AddPairUserOnWorklogEntry_NoExistingPairUser_ChangelogCreated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb();
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, _secondUser.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var changelog = await context.WorklogEntryUserAssociationChangelogEntries.SingleAsync();
        
        changelog.Type.Should().Be(ChangeType.Create);
        changelog.PairUserChangedId.Should().Be(_secondUser.Id);
    }
    
    [Fact]
    public async Task RemovePairUserOnWorklogEntry_ExistingPairUser_ValueUpdated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, null);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var updated = await context.WorklogEntries.SingleAsync(x => x.Id == existingWorklog.Id);

        updated.PairUserId.Should().BeNull();
    }
    
    [Fact]
    public async Task RemovePairUserOnWorklogEntry_ExistingPairUser_ChangelogCreated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, null);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var changelog = await context.WorklogEntryUserAssociationChangelogEntries.SingleAsync();
        
        changelog.Type.Should().Be(ChangeType.Delete);
        changelog.PairUserChangedId.Should().Be(_secondUser.Id);
    }
    
    [Fact]
    public async Task ReplacePairUserOnWorklogEntry_ExistingPairUser_ValueUpdated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, _thirdUser.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var updated = await context.WorklogEntries.SingleAsync(x => x.Id == existingWorklog.Id);

        updated.PairUserId.Should().Be(_thirdUser.Id);
    }
    
    [Fact]
    public async Task ReplacePairUserOnWorklogEntry_ExistingPairUser_BothChangelogsCreated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, _thirdUser.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var removeChangelog = await context.WorklogEntryUserAssociationChangelogEntries
            .SingleAsync(x => x.Type == ChangeType.Delete);
        var addChangelog = await context.WorklogEntryUserAssociationChangelogEntries
            .SingleAsync(x => x.Type == ChangeType.Create);
        
        removeChangelog.Type.Should().Be(ChangeType.Delete);
        removeChangelog.PairUserChangedId.Should().Be(_secondUser.Id);
        
        addChangelog.Type.Should().Be(ChangeType.Create);
        addChangelog.PairUserChangedId.Should().Be(_thirdUser.Id);
    }
    
    [Fact]
    public async Task ReplacePairUserOnWorklogEntry_NewPairIsExistingPair_ValueNotChangedAndNoChangelogsCreated()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await _worklogEntryService.UpdatePairUserAsync(existingWorklog.Id, _user.Id, _secondUser.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        
        var updated = await context.WorklogEntries.SingleAsync(x => x.Id == existingWorklog.Id);
        updated.PairUserId.Should().Be(_secondUser.Id);

        context.WorklogEntryUserAssociationChangelogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkLogEntryById_IncludeUsers_UserAndPairUserAreReturned()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();

        var worklogEntryWithUsers = await _worklogEntryService.GetWorklogEntryByIdAsync(existingWorklog.Id, true);
        worklogEntryWithUsers.User.Should().NotBeNull();
        worklogEntryWithUsers.PairUser.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetWorkLogEntryById_NotIncludeUsers_UserAndPairUserAreNotReturned()
    {
        var existingWorklog = await CreateExistingWorklogEntryInDb(pairId: _secondUser.Id);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        
        var worklogEntryWithUsers = await _worklogEntryService.GetWorklogEntryByIdAsync(existingWorklog.Id, false);
        worklogEntryWithUsers.User.Should().BeNull();
        worklogEntryWithUsers.PairUser.Should().BeNull();
    }
    
    
    [Fact]
    public async Task GetIssuesForWorklogEntry_WorklogHasNoIssues_ReturnsEmptyList()
    {
        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(occurred: new DateTime(DateOnly.FromDateTime(DateTime.Today), TimeOnly.FromTimeSpan(new TimeSpan(0, 12, 0, 0))),"a long enough description to be accepted");

        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit ],
            worklogEntry: _worklogEntry,
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        );
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);

        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);
        issueTags.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(WorklogTagNames))]
    public async Task GetIssuesForWorklogEntry_WorklogHasNoLinkedCommit_ReturnsListWithMissingCommitTag(string tagName)
    {
        List<string> tagsThatNeedLinkedCommits = ["Feature", "Test", "Fix", "Chore", "Refactor", "Reengineer"];
      
        await using var context = await GetDbContextFactory().CreateDbContextAsync();

        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(description: "http");
        _worklogTag = FakeDataGenerator.CreateWorklogTag(name: tagName);
        await SaveEntries([ _worklogTag ]);
        await AddWorklogToDefaultTask(new List<GitlabCommit>(), _worklogEntry, FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag));
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

        if (tagsThatNeedLinkedCommits.Contains(tagName))
        {
            issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.MissingCommit.GetName());
        }
        else
        {
            issueTags.Should().NotContain(x => x.Name == WorklogIssue.MissingCommit.GetName());
        }
    }

    [Fact]
    public async Task GetIssueForWorklogEntry_TestManualWorklogHasNoUrl_ReturnsListWithMissingCommitTag()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
                        
        _worklogTag = FakeDataGenerator.CreateWorklogTag(name: "TestManual");
        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(description: "a description without a url");
        await SaveEntries([ _worklogTag ]);
        await AddWorklogToDefaultTask(new List<GitlabCommit>(), worklogEntry: _worklogEntry, taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag));
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);
        issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.MissingCommit.GetName());
    }
    
    [Theory]
    [InlineData("TestManual")]
    [InlineData("Spike")]
    public async Task GetIssueForWorklogEntry_WorklogWithGivenTagHasUrl_ReturnsListWithoutMissingCommitTag(string tagName)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
                        
        _worklogTag = FakeDataGenerator.CreateWorklogTag(name: tagName);
        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(description: "here is a link: https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await SaveEntries([ _worklogTag ]);
        await AddWorklogToDefaultTask(new List<GitlabCommit>(), worklogEntry: _worklogEntry, taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag));
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);
        issueTags.Should().NotContain(x => x.Name == WorklogIssue.MissingCommit.GetName());
    }

    [Fact]
    public async Task GetIssueForWorklogEntry_SpikeAndTestManualTagsOnWorklogWithNoUrl_ReturnsListWithMissingUrlTagOnlyOnce()
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
                        
        _worklogTag = FakeDataGenerator.CreateWorklogTag(name: "TestManual");
        _secondWorklogTag = FakeDataGenerator.CreateWorklogTag(name: "Spike");
        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(description: "a description without a url");
        await SaveEntries([ _worklogTag, _secondWorklogTag ]);
        await AddWorklogToDefaultTask(
            new List<GitlabCommit>(), 
            worklogEntry: _worklogEntry, 
            taggedWorkInstanceForms: [FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag), FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_secondWorklogTag)]);
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);
        issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.MissingCommit.GetName());
    }


    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    public async Task GetIssueForWorklogEntry_WorklogHasSpecifiedNumberOfTags_ReturnsListWithTooManyTagsIfNecessary(int numberOfTags, bool isTooManyTags)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var tags = new List<WorklogTag>();
        for (var i = 0; i < numberOfTags; i++)
        {
            tags.Add(FakeDataGenerator.CreateWorklogTag($"tag{i}"));
        }

        await SaveEntries(tags);

        var taggedWorkInstanceForms = tags.Select(tag => FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(tag)).ToArray();
        
        await AddWorklogToDefaultTask(taggedWorkInstanceForms: taggedWorkInstanceForms);
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

        if (isTooManyTags)
        {
            issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.TooManyTags.GetName());
        }
        else
        {
            issueTags.Should().NotContain(x => x.Name == WorklogIssue.TooManyTags.GetName());
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(1000, false)]
    public async Task GetIssueForWorklogEntry_DescriptionHasSpecifiedLength_ReturnsListWithShortDescriptionTagIfNecessary(
        int descriptionLengthInChars, bool isTooShort)
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
                        
        _worklogTag = FakeDataGenerator.CreateWorklogTag(name: "TestManual");
        _worklogEntry = FakeDataGenerator.CreateWorklogEntryForm(description: new string('a', descriptionLengthInChars));
        await SaveEntries([ _worklogTag ]);
        await AddWorklogToDefaultTask(new List<GitlabCommit>(), worklogEntry: _worklogEntry, taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag));
        
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);
    
        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

        if (isTooShort)
        {
            issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.ShortDescription.GetName());
        }
        else
        {
            issueTags.Should().NotContain(x => x.Name == WorklogIssue.ShortDescription.GetName());
        }
    }
    
    [Theory]
    [InlineData(0.01, false)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(3.01, true)]
    [InlineData(20, true)]
    public async Task GetIssuesForWorklogEntry_WorklogHasSpecifiedDuration_ReturnsListWithLongWorklogTagIfLongerThanThreeHours(double durationInHours, bool isTooLong)
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag, TimeSpan.FromHours(durationInHours)
        ));
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);

        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

        if (isTooLong)
        {
             issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.TooLong.GetName());    
        }
        else
        {
            issueTags.Should().NotContain(x => x.Name == WorklogIssue.TooLong.GetName());
        }
    }  
    
    [Theory]
    [InlineData(0.01, true)]
    [InlineData(0.49, true)]
    [InlineData(0.5, false)]
    [InlineData(20, false)]
    public async Task GetIssuesForWorklogEntry_FeatureWorklogHasSpecifiedDuration_ReturnsListWithShortWorklogTagIfLessThanThirtyMinutes(double durationInHours, bool isTooShort)
    {
        await AddWorklogToDefaultTask(
            linkedCommits: [ _firstCommit ], 
            taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag, TimeSpan.FromHours(durationInHours)
        ));
        
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);

        var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);
        
        if (isTooShort)
        {
            issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.ShortDuration.GetName());    
        }
        else
        {
            issueTags.Should().NotContain(x => x.Name == WorklogIssue.ShortDuration.GetName());
        }    
    }    
    
    [Theory]
    [MemberData(nameof(WorklogTagNames))]
    public async Task GetIssuesForWorklogEntry_NonFeatureWorklogHasDurationLessThanThirtyMins_ReturnsEmptyList(string tagName)
    {
        if (tagName != "Feature")
        {
            await using var context = await GetDbContextFactory().CreateDbContextAsync();
            
            _worklogTag = FakeDataGenerator.CreateWorklogTag(name: tagName);
            await SaveEntries([ _worklogTag ]);

            await AddWorklogToDefaultTask(
                linkedCommits: [ _firstCommit ], 
                taggedWorkInstanceForms: FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag, TimeSpan.FromMinutes(20)
            ));

            var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);

            var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

            issueTags.Should().NotContain(x => x.Name == WorklogIssue.ShortDuration.GetName());
        }
    }

    [Theory]
    [MemberData(nameof(WorklogTagNames))]
    public async Task GetIssueforWorklogEntry_WorklogHasFeatureAndOtherTags_ReturnsTooShortTagWhenFeatureLogIsLessThanThirtyMinutes(string tagName)
    {
        if (tagName != "Feature")
        {
            await using var context = await GetDbContextFactory().CreateDbContextAsync();
            
            _worklogTag = FakeDataGenerator.CreateWorklogTag(name: tagName);
            await SaveEntries([ _worklogTag ]);

            await AddWorklogToDefaultTask(
                linkedCommits: [ _firstCommit ], 
                taggedWorkInstanceForms: [ FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_worklogTag, TimeSpan.FromMinutes(20)), FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag, TimeSpan.FromMinutes(20))]
                );
            
            var existingWorklog = await context.WorklogEntries.Include(x => x.LinkedCommits).SingleAsync(x => x.Task == _userStoryTask);

            var issueTags = await _worklogEntryService.GetIssuesForWorklogEntryAsync(existingWorklog.Id);

            issueTags.Should().ContainSingle(x => x.Name == WorklogIssue.ShortDuration.GetName());
        }
    }
}