using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface IUserStoryService
{
    /// <summary>
    /// Updates the stages of every story within the sprint using the provided stage mapping function
    /// </summary>
    /// <param name="actingUser">User to make changes as</param>
    /// <param name="sprint">Sprint to select stories from, must include stories</param>
    /// <param name="stageMapping">Mapping from current stage to new stage</param>
    Task UpdateStages(User actingUser, Sprint sprint, Func<Stage, Stage> stageMapping);
        
    /// <summary>
    /// Updates the stages for stories within an enumerable using the provided stage mapping function
    /// </summary>
    /// <param name="actingUser">User to make changes as</param>
    /// <param name="stories">Stories to update stages</param>
    /// <param name="stageMapping">Mapping from current stage to new stage</param>
    Task UpdateStages(User actingUser, IEnumerable<UserStory> stories, Func<Stage, Stage> stageMapping);
        
    /// <summary>
    /// Defers the a given story. If the story is in done or deferred it stays in the same stage, otherwise
    /// its stage is changed to deferred.
    /// </summary>
    /// <param name="actingUser">User to make changes as</param>
    /// <param name="story">Story to defer</param>
    Task DeferStory(User actingUser, UserStory story);

    Task<UserStory> GetByIdAsync(long storyId);

    /// <summary>
    /// Sets the review comments for some story, optionally scoped to an editing session. Changes made with the
    /// same <see cref="editingSessionGuid"/> will only ever generate (at most) a single changelog for each field.
    /// This allows for auto-saving fields (nice for live updating) without spamming a new changelog entry for
    /// each time auto-save is triggered. 
    /// </summary>
    /// <param name="storyId">ID of story for which review comments are being set</param>
    /// <param name="actingUserId">The ID of the user who is making the changes</param>
    /// <param name="reviewComments">Review comments to set</param>
    /// <param name="editingSessionGuid">GUID representing the current editing session, if any.</param>
    Task SetReviewCommentsForIdAsync(long storyId, long actingUserId, string reviewComments, Guid? editingSessionGuid=null);
    
    Task<IEnumerable<UserStory>> GetBySprintIdAsync(long sprintId);
    Task<IList<UserStory>> GetStoriesForSprintReviewAsync(long sprintId);
}

public class UserStoryService : IUserStoryService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
        
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserStoryChangelogRepository _userStoryChangelogRepository;

    private readonly IChangelogService _changelogService;
    
    private readonly IEntityLiveUpdateService _entityLiveUpdateService;

    public UserStoryService(IUserStoryRepository userStoryRepository,
        IUserStoryChangelogRepository userStoryChangelogRepository, 
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IChangelogService changelogService, 
        IEntityLiveUpdateService entityLiveUpdateService
    ) {
        _userStoryRepository = userStoryRepository;
        _userStoryChangelogRepository = userStoryChangelogRepository;
        _dbContextFactory = dbContextFactory;
        _changelogService = changelogService;
        _entityLiveUpdateService = entityLiveUpdateService;
    }
        
    /// <inheritdoc/>
    public async Task UpdateStages(User actingUser, Sprint sprint, Func<Stage, Stage> stageMapping)
    {
        await UpdateStages(actingUser, sprint.Stories, stageMapping);
    }

    /// <inheritdoc/>
    public async Task UpdateStages(User actingUser, IEnumerable<UserStory> stories, Func<Stage, Stage> stageMapping)
    {
        var updatedStories = new List<UserStory>();
        var storyChanges = new List<UserStoryChangelogEntry>();
        foreach (var story in stories)
        {
            var oldStage = story.Stage;
            var newStage = stageMapping(oldStage);
            if (newStage == oldStage) continue;
            story.Stage = newStage;
            updatedStories.Add(story);
            storyChanges.Add(new(actingUser.Id, story.Id, nameof(UserStory.Stage),
                Change<object>.Update(oldStage, newStage)));
        }

        var clonedStories = updatedStories.Select(story => story.CloneForPersisting()).ToList();

        await _userStoryRepository.UpdateAllAsync(clonedStories);
        await _userStoryChangelogRepository.AddAllAsync(storyChanges);

        foreach (var (story, clone) in updatedStories.Zip(clonedStories)) story.RowVersion = clone.RowVersion;
    }
        
    /// <inheritdoc/>
    public async Task DeferStory(User actingUser, UserStory story)
    {
        Stage StageMapping(Stage stage) => stage == Stage.Done ? stage : Stage.Deferred;
        IEnumerable<UserStory> enumerableStory = new[] { story };
        await UpdateStages(actingUser, enumerableStory, StageMapping);
    }

    public async Task<UserStory> GetByIdAsync(long storyId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.UserStories
            .Include(x => x.AcceptanceCriterias)
            .FirstOrDefaultAsync(x => x.Id == storyId);
    }

    public async Task SetReviewCommentsForIdAsync(long storyId, long actingUserId, string reviewComments, Guid? editingSessionGuid=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var story = await context.UserStories.FirstOrDefaultAsync(x => x.Id == storyId);
        if (story is null) throw new ArgumentException("No story found with given ID");
        
        var changelog = new UserStoryChangelogEntry(
            actingUserId, 
            storyId, 
            nameof(UserStory.ReviewComments), 
            Change<object>.Update(story.ReviewComments, reviewComments),
            editingSessionGuid
        );
        
        story.ReviewComments = reviewComments;
        context.Update(story);
        await context.SaveChangesAsync();
        await _changelogService.SaveChangelogsAsync(
            new[] { changelog }, 
            existingChangelogs => existingChangelogs.Where(x => x.UserStoryChangedId == storyId)
        );
        
        await _entityLiveUpdateService.BroadcastNewValueForEntityToProjectAsync(storyId, story.ProjectId, story, actingUserId);
    }

    public async Task<IEnumerable<UserStory>> GetBySprintIdAsync(long sprintId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.UserStories
            .Where(x => x.StoryGroupId == sprintId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }
    
    public async Task<IList<UserStory>> GetStoriesForSprintReviewAsync(long sprintId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.UserStories
            .Where(x => x.StoryGroupId == sprintId)
            .Where(x => x.Stage != Stage.Deferred)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }
}