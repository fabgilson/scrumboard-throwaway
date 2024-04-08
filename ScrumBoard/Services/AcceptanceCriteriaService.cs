using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface IAcceptanceCriteriaService
{
    /// <summary>
    /// Sets the review related fields of some AcceptanceCriteria, optionally passing in a session GUID.
    /// </summary>
    /// <param name="acceptanceCriteriaId">ID of acceptance criteria for which to set review fields.</param>
    /// <param name="actingUserId">ID of user who is making the changes.</param>
    /// <param name="newStatus">New status to apply to the acceptance criterion.</param>
    /// <param name="newReviewComments">New review comments to apply to the acceptance criterion.</param>
    /// <param name="editingSessionGuid">Optional, if given will ensure that only a single set of changelogs is generated for this GUID.</param>
    Task SetReviewFieldsByIdAsync(long acceptanceCriteriaId, long actingUserId, AcceptanceCriteriaStatus newStatus, string newReviewComments, Guid? editingSessionGuid=null);
}

public class AcceptanceCriteriaService : IAcceptanceCriteriaService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    private readonly IEntityLiveUpdateService _entityLiveUpdateService;
    private readonly IChangelogService _changelogService;

    public AcceptanceCriteriaService(
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IEntityLiveUpdateService entityLiveUpdateService, 
        IChangelogService changelogService
    ) {
        _dbContextFactory = dbContextFactory;
        _entityLiveUpdateService = entityLiveUpdateService;
        _changelogService = changelogService;
    }

    public async Task SetReviewFieldsByIdAsync(long acceptanceCriteriaId, long actingUserId, AcceptanceCriteriaStatus newStatus, string newReviewComments, Guid? editingSessionGuid=null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var acceptanceCriteria = await context.AcceptanceCriterias
            .Include(acceptanceCriteria => acceptanceCriteria.UserStory)
            .FirstOrDefaultAsync(x => x.Id == acceptanceCriteriaId);

        if (acceptanceCriteria is null) throw new ArgumentException("No acceptance criteria found with given ID");
        
        var fieldChangelogs = ChangelogGenerator.GenerateChangesForObject(acceptanceCriteria,
            ("Status", x => x.Status, newStatus),
            ("ReviewComments", x => x.ReviewComments, newReviewComments)
        );
        var changelogs = fieldChangelogs
            .Select(x => new AcceptanceCriteriaChangelogEntry(actingUserId, acceptanceCriteria, x.FieldName, x.Change, editingSessionGuid));
        
        await _changelogService.SaveChangelogsAsync(
            changelogs, 
            existingChangelogs => existingChangelogs
                .Where(x => x.AcceptanceCriteriaChangedId == acceptanceCriteriaId)
                .Where(x => x.UserStoryChangedId == acceptanceCriteria.UserStoryId)
        );
        
        acceptanceCriteria.Status = newStatus;
        acceptanceCriteria.ReviewComments = newReviewComments;
        
        context.Update(acceptanceCriteria);
        await context.SaveChangesAsync();
            
        await _entityLiveUpdateService.BroadcastNewValueForEntityToProjectAsync(
            acceptanceCriteriaId,
            acceptanceCriteria.UserStory.ProjectId, 
            acceptanceCriteria, 
            actingUserId
        );
    }
}