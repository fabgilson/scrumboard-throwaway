using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Filters;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Forms;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services;

public interface IWorklogEntryService
{
    /// <summary>
    /// Create a new worklog entry and save it to database. Any <see cref="TaggedWorkInstance"/>s or <see cref="GitlabCommit"/>s
    /// included in the form will be ignored, and should be supplied to the dedicated parameters separately.
    /// </summary>
    /// <param name="newWorklogEntryForm">New base values for worklog entry</param>
    /// <param name="creatorId">ID of the creating user</param>
    /// <param name="taskId">ID of the task to which the worklog belongs</param>
    /// <param name="pairId">ID of a pair user for the worklog, may be null for no pair</param>
    /// <param name="taggedWorkInstanceForms">
    /// Tagged work instances to be added to the worklog. Any worklog tag may be used no more than once or an  
    /// </param>
    /// <param name="linkedCommits"></param>
    Task CreateWorklogEntryAsync(
        WorklogEntryForm newWorklogEntryForm, 
        long creatorId, 
        long taskId, 
        IEnumerable<TaggedWorkInstanceForm> taggedWorkInstanceForms,
        long? pairId = null,
        ICollection<GitlabCommit> linkedCommits = null
    );

    /// <summary>
    /// Update the basic fields of a worklog, will not modify any navigation properties.
    /// </summary>
    /// <param name="existingWorklogId"></param>
    /// <param name="newWorklogEntryValue"></param>
    /// <param name="updaterId"></param>
    Task UpdateWorklogEntryAsync(long existingWorklogId, WorklogEntryForm newWorklogEntryValue, long updaterId);

    /// <summary>
    /// Updates the (possibly null) pair user for some worklog.
    /// </summary>
    /// <param name="existingWorklogId"></param>
    /// <param name="actingUserId"></param>
    /// <param name="pairUserId"></param>
    Task UpdatePairUserAsync(long existingWorklogId, long actingUserId, long? pairUserId);

    /// <summary>
    /// Gets the most recent worklog entries (ordered most recent first) for some project, optionally filtered to a
    /// specific story group.
    /// </summary>
    /// <param name="projectId">ID of project for which to get most recent worklogs</param>
    /// <param name="maxNumberOfResults">Maximum number of worklogs to return</param>
    /// <param name="storyGroupId">Optional, if given will limit worklogs to just this story group</param>
    /// <returns>Most recent worklogs for some project, optionally filtered to a single story group, most recent first</returns>
    Task<List<WorklogEntry>> GetMostRecentWorklogForProjectAsync(long projectId, int maxNumberOfResults, long? storyGroupId=null);
    
    /// <summary>
    /// Gets a WorklogEntry by ID and optionally includes both related users
    /// </summary>
    /// <param name="worklogEntryId">ID of the WorklogEntry to retreive</param>
    /// <param name="includeUsers">When true both User and PairUser will be retrieved from the database</param>
    /// <returns>A single WorklogEntry</returns>
    Task<WorklogEntry> GetWorklogEntryByIdAsync(long worklogEntryId, bool includeUsers=false);
    
    /// <summary>
    /// Given some already existing commits, links them to the given worklog entry. Removes any existing linked commits
    /// not included in this list.
    /// </summary>
    /// <param name="worklogId"></param>
    /// <param name="creatorId"></param>
    /// <param name="linkedCommits"></param>
    Task SetLinkedGitlabCommitsOnWorklogAsync(long worklogId, long creatorId, ICollection<GitlabCommit> linkedCommits);

    /// <summary>
    /// Returns all worklog entries for some project, optionally filtered by a single sprint, optionally filtered
    /// by a given user.
    /// </summary>
    /// <param name="projectId">ID of project for which to get worklog entries</param>
    /// <param name="sprintId">Optional, if given will only fetch worklog entries in this sprint</param>
    /// <param name="userId">Optional, if given will only fetch worklog entries for this user</param>
    /// <returns>All worklog entries from a project, optionally limited to a single sprint</returns>
    Task<IEnumerable<WorklogEntry>> GetWorklogEntriesForProjectAsync(long projectId, long? sprintId=null, long? userId=null);

    Task<ICollection<WorklogEntry>> GetWorklogEntriesForStoryAsync(long storyId);

    Task<ICollection<WorklogEntry>> GetWorklogEntriesForTaskAsync(long taskId);

    Task<IEnumerable<WorklogEntry>> GetByProjectFilteredAsync(long projectId, WorklogEntryFilter filter, long? storyGroupId = null);
    
    /// <summary>
    /// Gets paginated worklog entries for some project given some filtering and ordering options. Optionally,
    /// may be limited to a single story group within the project.
    /// </summary>
    /// <param name="projectId">ID of the project for which to get paginated worklog entries</param>
    /// <param name="filter">Filtering options to apply</param>
    /// <param name="orderByColumn">Column by which to order results</param>
    /// <param name="orderByDescending">Whether column should be sorted in descending order</param>
    /// <param name="pageNumber">Page number of results to return, first page = 1</param>
    /// <param name="pageSize">Number of results to return per page</param>
    /// <param name="storyGroupId">Optional, if given will limit results to just within this story group</param>
    /// <returns>
    /// Paginated list of worklog entries for some project, and optionally some story group, with filtering and ordering
    /// configuration applied
    /// </returns>
    Task<PaginatedList<WorklogEntry>> GetByProjectFilteredAndPaginatedAsync(
        long projectId,
        WorklogEntryFilter filter,
        TableColumn orderByColumn,
        bool orderByDescending,
        int pageNumber,
        int pageSize,
        long? storyGroupId=null
    );

    /// <summary>
    /// Updates (overrides) a worklogs tagged work instances. Any work instances not found in the provided collection
    /// <see cref="taggedWorkInstanceForms"/> that already exist on the entity will be deleted. Any new taggedWorkInstances
    /// for a <see cref="WorklogTag"/> that already exists on the worklog will overwrite the existing ones.
    /// </summary>
    /// <param name="worklogEntryId">ID of worklog entry for which to update tagged work instances</param>
    /// <param name="actingUserId">ID of user making the changes to a worklog's tagged work instances</param>
    /// <param name="taggedWorkInstanceForms">The new set of tagged work instances for this worklog</param>
    Task SetTaggedWorkInstancesAsync(long worklogEntryId, long actingUserId, ICollection<TaggedWorkInstanceForm> taggedWorkInstanceForms);

    /// <summary>
    /// Asynchronously gets a list of issue tags representing issues with the work log with the given ID.
    /// </summary>
    /// <param name="worklogEntryId">ID of the work log entry to get issues for</param>
    /// <returns>A list of issue tags representing issues with the work log with the given ID</returns>
    Task<IEnumerable<IssueTag>> GetIssuesForWorklogEntryAsync(long worklogEntryId);
}

public class WorklogEntryService : IWorklogEntryService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IClock _clock;
    private const int MinDescriptionCharacterCount = 20;
    private const int MaxTagsPerWorklog = 3;
    private const int MaxWorklogDurationInHours = 3;
    private const double MinFeatureDurationInHoursPerWorklog = 0.5;
    private readonly TimeSpan _earliestWorkStartTime = new (0, 7, 0, 0);
    private readonly TimeSpan _latestWorkEndTime = new (0, 20, 0, 0);
    private readonly List<string> _tagsThatNeedLinkedCommits = ["Feature", "Test", "Fix", "Chore", "Refactor", "Reengineer"];
    private readonly List<string> _tagsThatNeedLinkedUrls = ["TestManual", "Spike"];

    public WorklogEntryService(IDbContextFactory<DatabaseContext> dbContextFactory, IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task CreateWorklogEntryAsync(
        WorklogEntryForm newWorklogEntryForm, 
        long creatorId, 
        long taskId, 
        IEnumerable<TaggedWorkInstanceForm> taggedWorkInstanceForms,
        long? pairId = null,
        ICollection<GitlabCommit> linkedCommits = null
    ) {
        var taggedWorkInstanceFormsArray = taggedWorkInstanceForms?.ToArray() ?? Array.Empty<TaggedWorkInstanceForm>();
        if (!taggedWorkInstanceFormsArray.Any())
        {
            throw new InvalidOperationException("At least one taggedWorkInstanceForm must be provided");
        }
        
        var worklogEntry = new WorklogEntry
        {
            UserId = creatorId,
            PairUserId = pairId,
            TaskId = taskId,
            Created = _clock.Now,
            Description = newWorklogEntryForm.Description,
            Occurred = newWorklogEntryForm.Occurred,
        };
        
        await using (var context = await _dbContextFactory.CreateDbContextAsync())
        {
            await context.WorklogEntries.AddAsync(worklogEntry);
            await context.SaveChangesAsync();
        }
        await AddChangelogsForWorklogEntryCreation(worklogEntry, creatorId);
        await SetTaggedWorkInstancesAsync(worklogEntry.Id, creatorId, taggedWorkInstanceFormsArray.ToList());
        
        if(linkedCommits is not { Count: > 0 }) return;
        await SetLinkedGitlabCommitsOnWorklogAsync(worklogEntry.Id, creatorId, linkedCommits);
    }

    private async Task AddChangelogsForWorklogEntryCreation(WorklogEntry worklogEntry, long actingUserId)
    {
        var changelog = new WorklogEntryChangelogEntry
        {
            Created = _clock.Now,
            CreatorId = actingUserId,
            Type = ChangeType.Create,
            WorklogEntryChangedId = worklogEntry.Id
        };
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.WorklogEntryChangelogEntries.AddAsync(changelog);
    }
    
    public async Task UpdateWorklogEntryAsync(long existingWorklogId, WorklogEntryForm newWorklogEntryValue, long updaterId)
    {
        var existingWorklog = await GetWorklogEntryByIdAsync(existingWorklogId);
        if (existingWorklog is null)
        {
            throw new ArgumentException("Existing worklog with provided ID was not found in the database.");
        }
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Update just the non-navigation fields that are allowed to be modified after worklog creation
        var changeLogs = ChangelogGenerator.GenerateChangesBetweenObjects(
            existingWorklog, 
            newWorklogEntryValue,
            ("Description", existing => existing.Description, incoming => incoming.Description),
            ("Occurred", existing => existing.Occurred, incoming => incoming.Occurred)
        ).ToList();
        var worklogChangeLogs = changeLogs.Select(x => 
            new WorklogEntryChangelogEntry(updaterId, existingWorklog, x.FieldName, x.Change));

        existingWorklog.Description = newWorklogEntryValue.Description;
        existingWorklog.Occurred = newWorklogEntryValue.Occurred;

        context.Update(existingWorklog);
        await context.WorklogEntryChangelogEntries.AddRangeAsync(worklogChangeLogs);
        await context.SaveChangesAsync();
    }

    public async Task UpdatePairUserAsync(long existingWorklogId, long actingUserId, long? pairUserId)
    {
        var existingWorklog = await GetWorklogEntryByIdAsync(existingWorklogId);
        if (existingWorklog is null)
        {
            throw new ArgumentException("Existing worklog with provided ID was not found in the database.");
        }
        if (existingWorklog.PairUserId == pairUserId) return;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var changes = new List<WorklogEntryUserAssociationChangelogEntry>();
        // Handle the case where the pairUserId is being set to null (removal)
        if (pairUserId is null && existingWorklog.PairUserId != null)
        {
            changes.Add(new WorklogEntryUserAssociationChangelogEntry(actingUserId, existingWorklogId, existingWorklog.PairUserId, nameof(existingWorklog.PairUser), Change<object>.Delete(existingWorklog.PairUser)));
        }
        // Handle the case where the pairUserId is being added or changed
        else if (pairUserId != null)
        {
            if (existingWorklog.PairUserId != null)
            {
                changes.Add(new WorklogEntryUserAssociationChangelogEntry(actingUserId, existingWorklogId, existingWorklog.PairUserId, nameof(existingWorklog.PairUser), Change<object>.Delete(existingWorklog.PairUser)));
            }
            changes.Add(new WorklogEntryUserAssociationChangelogEntry(actingUserId, existingWorklogId, pairUserId, nameof(existingWorklog.PairUser), Change<object>.Create(pairUserId)));
        }

        existingWorklog.PairUserId = pairUserId;
        context.Update(existingWorklog);
        
        await context.WorklogEntryUserAssociationChangelogEntries.AddRangeAsync(changes);
        await context.SaveChangesAsync();
    }

    public async Task<List<WorklogEntry>> GetMostRecentWorklogForProjectAsync(long projectId, int maxNumberOfResults, long? storyGroupId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => storyGroupId == null || x.Task.UserStory.StoryGroupId == storyGroupId)
            .Include(x => x.User)
            .OrderByDescending(entry => entry.Occurred)
            .Take(maxNumberOfResults)
            .ToListAsync();
    }


    public async Task<WorklogEntry> GetWorklogEntryByIdAsync(long worklogEntryId, bool includeUsers=false)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var dbSet = context.WorklogEntries;
        if (includeUsers)
        {
            return await dbSet
                .Include(w => w.User)
                .Include(w => w.PairUser)
                .Include(w => w.Task)
                .FirstOrDefaultAsync(x => x.Id == worklogEntryId);
        }
        return await dbSet.FirstOrDefaultAsync(x => x.Id == worklogEntryId);
    }
    
    public async Task SetLinkedGitlabCommitsOnWorklogAsync(long worklogId, long creatorId, ICollection<GitlabCommit> linkedCommits)
    {
        if (await GetWorklogEntryByIdAsync(worklogId) is null)
        {
            throw new ArgumentException("Existing worklog with provided ID was not found in the database.");
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var existingCommitJoins = await context.WorklogCommitJoins
            .Where(x => x.EntryId == worklogId)
            .Include(x => x.Commit)
            .ToListAsync();
        
        // Get incoming commits that are not yet linked to the worklog
        var newCommitsToLink = linkedCommits
            .Where(incoming => !existingCommitJoins.Any(x => x.CommitId == incoming.Id && x.EntryId == worklogId))
            .ToList();
        
        // Get already linked commits that aren't included in the incoming commits and should therefore be removed
        var oldCommitsToUnlink = existingCommitJoins
            .Where(existing => !linkedCommits.Select(x => x.Id).Contains(existing.CommitId) && existing.EntryId == worklogId)
            .ToList();

        context.WorklogCommitJoins.RemoveRange(oldCommitsToUnlink);

        await EnsureCommitsExistInDbAsync(newCommitsToLink);
        await context.WorklogCommitJoins.AddRangeAsync(newCommitsToLink.Select(x => new WorklogCommitJoin
        {
            CommitId = x.Id,
            EntryId = worklogId
        }));
        
        await context.SaveChangesAsync();
        await AddChangelogsForLinkedCommits(newCommitsToLink, oldCommitsToUnlink.Select(x => x.Commit), worklogId, creatorId);
    }

    /// <summary>
    /// Ensure that some given collection of commits are all present in the database. Any that are not already present are added.
    /// </summary>
    /// <param name="commits">Commits to ensure are in the database.</param>
    private async Task EnsureCommitsExistInDbAsync(IEnumerable<GitlabCommit> commits)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var commit in commits)
        {
            if(await context.GitlabCommits.AnyAsync(x => x.Id == commit.Id)) continue;
            await context.GitlabCommits.AddAsync(commit);
        }

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WorklogEntry>> GetWorklogEntriesForProjectAsync(long projectId, long? sprintId=null, long? userId=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => sprintId == null || x.Task.UserStory.StoryGroupId == sprintId)
            .Where(x => userId == null || x.UserId == userId)
            .Include(x => x.Task).ThenInclude(t => t.UserStory)
            .ToListAsync();
    }

    public async Task<ICollection<WorklogEntry>> GetWorklogEntriesForStoryAsync(long storyId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogEntries
            .Where(x => x.Task.UserStoryId == storyId)
            .Include(x => x.Task)
            .Include(x => x.User)
            .Include(x => x.LinkedCommits)
            .Include(x => x.PairUser)
            .ToListAsync();
    }

    public async Task<ICollection<WorklogEntry>> GetWorklogEntriesForTaskAsync(long taskId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogEntries
            .Where(x => x.TaskId == taskId)
            .Include(x => x.Task)
            .Include(x => x.User)
            .Include(x => x.LinkedCommits)
            .Include(x => x.PairUser)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorklogEntry>> GetByProjectFilteredAsync(
        long projectId, 
        WorklogEntryFilter filter, 
        long? storyGroupId=null
    ) {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => storyGroupId == null || x.Task.UserStory.StoryGroupId == storyGroupId)
            .Where(filter.Predicate)
            .Include(x => x.User)
            .Include(x => x.Task).ThenInclude(x => x.UserStory)
            .ToListAsync();
    }
    
    public async Task<PaginatedList<WorklogEntry>> GetByProjectFilteredAndPaginatedAsync(
        long projectId, 
        WorklogEntryFilter filter, 
        TableColumn orderByColumn,
        bool orderByDescending,
        int pageNumber, 
        int pageSize, 
        long? storyGroupId=null
    ) {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var afterDb = await context.WorklogEntries
            .Where(x => x.Task.UserStory.ProjectId == projectId)
            .Where(x => storyGroupId == null || x.Task.UserStory.StoryGroupId == storyGroupId)
            .Where(filter.Predicate)
            .Include(x => x.User)
            .Include(x => x.Task).ThenInclude(x => x.UserStory)
            .Include(x => x.LinkedCommits)
            .Include(x => x.PairUser)
            .ToListAsync();

        // Some ordering queries (such as those that must sum TimeSpans) can not be translated, so we must pull
        // results into memory here first, forcing the DB query to execute. Hopefully one day there will be a way
        // to translate queries that sum TimeSpans into SQL with our stack, but for now we must do this. 
        var queryable = afterDb
            .AsQueryable()
            .OrderWorklogEntries(orderByColumn, orderByDescending);
        
        return await PaginatedList<WorklogEntry>.CreateAsync(queryable, pageNumber, pageSize);
    }

    public async Task SetTaggedWorkInstancesAsync(long worklogEntryId, long actingUserId, ICollection<TaggedWorkInstanceForm> taggedWorkInstanceForms)
    {
        if (taggedWorkInstanceForms.Select(x => x.WorklogTagId).Distinct().Count() != taggedWorkInstanceForms.Count)
        {
            throw new ArgumentException("No more than one TaggedWorkInstance may be supplied for any WorklogTag");
        }
        if (await GetWorklogEntryByIdAsync(worklogEntryId) is null)
        {
            throw new ArgumentException("Existing worklog with provided ID was not found in the database");
        }
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var existingTaggedWorkInstances = await context.TaggedWorkInstances
            .Where(x => x.WorklogEntryId == worklogEntryId)
            .ToListAsync();
        
        // Recreate tagged work instances to ensure no navigation entities are included
        var newTaggedWorkInstances = taggedWorkInstanceForms.Select(x => new TaggedWorkInstance
        {
            WorklogEntryId = worklogEntryId,
            WorklogTagId = x.WorklogTagId,
            Duration = x.Duration
        }).ToList();

        context.RemoveRange(existingTaggedWorkInstances);
        await context.AddRangeAsync(newTaggedWorkInstances);
        await context.SaveChangesAsync();

        await AddChangelogsForTaggedWorkInstances(newTaggedWorkInstances, existingTaggedWorkInstances, worklogEntryId, actingUserId);
    }

    public async Task<IEnumerable<IssueTag>> GetIssuesForWorklogEntryAsync(long worklogEntryId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var worklogEntry = await context.WorklogEntries.Include(x => x.LinkedCommits).FirstAsync(x => x.Id == worklogEntryId);
        
        var issueTags = new List<IssueTag>();
        var worklogTags = worklogEntry.TaggedWorkInstances.Select(x => x.WorklogTag.Name).ToList();
        
        if (worklogTags.Intersect(_tagsThatNeedLinkedCommits).Any() && !worklogEntry.LinkedCommits.Any()
            || (worklogTags.Intersect(_tagsThatNeedLinkedUrls).Any() && !worklogEntry.Description.Contains("http")))
            issueTags.Add(new IssueTag { Name = WorklogIssue.MissingCommit.GetName() });

        if (worklogEntry.Description.Length < MinDescriptionCharacterCount)
            issueTags.Add(new IssueTag { Name = WorklogIssue.ShortDescription.GetName()});

        if (worklogEntry.GetTotalTimeSpent() > TimeSpan.FromHours(MaxWorklogDurationInHours))
            issueTags.Add(new IssueTag { Name = WorklogIssue.TooLong.GetName()});

        var timeOnFeatureTag = worklogEntry.TaggedWorkInstances
            .Where(x => x.WorklogTag.Name == "Feature")
            .Select(x => x.Duration)
            .Sum();

        if (timeOnFeatureTag > TimeSpan.Zero && timeOnFeatureTag < TimeSpan.FromHours(MinFeatureDurationInHoursPerWorklog))
            issueTags.Add(new IssueTag { Name = WorklogIssue.ShortDuration.GetName()});

        if (worklogEntry.TaggedWorkInstances.Count > MaxTagsPerWorklog)
            issueTags.Add(new IssueTag { Name = WorklogIssue.TooManyTags.GetName()});

        var entryTime = worklogEntry.Occurred.TimeOfDay;
        if (entryTime < _earliestWorkStartTime || entryTime.Add(worklogEntry.GetTotalTimeSpent()) > _latestWorkEndTime)
        {
            issueTags.Add(new IssueTag { Name = WorklogIssue.OutsideWorkHours.GetName()});
        }

        return issueTags;
    }
    
    private async Task AddChangelogsForTaggedWorkInstances(
        ICollection<TaggedWorkInstance> incomingTaggedWorkInstances, 
        ICollection<TaggedWorkInstance> existingTaggedWorkInstances, 
        long worklogEntryId, 
        long actingUserId
    ) {
        var newIncomingTaggedWorkInstances = incomingTaggedWorkInstances
            .Where(incoming => existingTaggedWorkInstances.All(x => x.WorklogTagId != incoming.WorklogTagId))
            .ToList();

        var overridingIncomingWorkInstances = incomingTaggedWorkInstances
            .Where(incoming => existingTaggedWorkInstances.Any(x => x.WorklogTagId == incoming.WorklogTagId));

        var oldTaggedWorkInstancesToRemove = existingTaggedWorkInstances
            .Where(existing => incomingTaggedWorkInstances.All(incoming => incoming.WorklogTagId != existing.WorklogTagId));

        var changelogs = new List<TaggedWorkInstanceChangelogEntry>();

        // Add changelogs for new tagged work instances not overriding any existing ones
        changelogs.AddRange(newIncomingTaggedWorkInstances.Select(x => 
            TaggedWorkInstanceChangelogEntry.Add(actingUserId, worklogEntryId, x.WorklogTagId, x)    
        ));
        
        // Add changelogs for potential changes to existing changelogs
        foreach (var incoming in overridingIncomingWorkInstances)
        {
            var existing = existingTaggedWorkInstances.First(x => x.WorklogTagId == incoming.WorklogTagId);
            if(existing.Duration == incoming.Duration) continue;
            changelogs.Add(TaggedWorkInstanceChangelogEntry.Update(actingUserId, worklogEntryId, existing.WorklogTagId, incoming, existing));
        }
        
        // Add changelogs for removed tagged work instances
        changelogs.AddRange(oldTaggedWorkInstancesToRemove.Select(x => 
            TaggedWorkInstanceChangelogEntry.Remove(actingUserId, worklogEntryId, x.WorklogTagId, x)    
        ));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddRangeAsync(changelogs);
        await context.SaveChangesAsync();
    }

    private async Task AddChangelogsForLinkedCommits(IEnumerable<GitlabCommit> newlyLinkedCommits, IEnumerable<GitlabCommit> unlinkedCommits, long worklogEntryId, long actingUserId)
    {
        var addedCommitChangelogs = newlyLinkedCommits.Select(x => 
            WorklogEntryCommitChangelogEntry.Add(actingUserId, worklogEntryId, x));
        var removedCommitChangelogs = unlinkedCommits.Select(x => 
            WorklogEntryCommitChangelogEntry.Remove(actingUserId, worklogEntryId, x));
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        await context.AddRangeAsync(addedCommitChangelogs);
        await context.AddRangeAsync(removedCommitChangelogs);
        await context.SaveChangesAsync();
    }
}