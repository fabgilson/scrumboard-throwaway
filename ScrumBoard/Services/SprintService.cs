using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface ISprintService
{
    Task<bool> UpdateStage(User actingUser, Sprint sprint, SprintStage newStage);
    
    /// <summary>
    /// Get a sprint by some given ID, if no sprint with given ID exists, returns null.
    /// </summary>
    /// <param name="sprintId">ID of sprint to get</param>
    /// <returns>Sprint with given ID if one exists, null otherwise</returns>
    Task<Sprint> GetByIdAsync(long sprintId);
}

public class SprintService : ISprintService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    
    private readonly ISprintChangelogRepository _sprintChangelogRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ILogger<SprintService> _logger;

    private readonly IEntityLiveUpdateService _entityLiveUpdateService;
    
    public SprintService(
        ISprintChangelogRepository sprintChangelogRepository, 
        ISprintRepository sprintRepository, 
        ILogger<SprintService> logger, 
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IEntityLiveUpdateService entityLiveUpdateService
    ) {
        _sprintChangelogRepository = sprintChangelogRepository;
        _sprintRepository = sprintRepository;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _entityLiveUpdateService = entityLiveUpdateService;
    }

    /// <summary>
    /// Updates sprint stage without modifying user stories or task stage
    /// </summary>
    /// <param name="actingUser">User to apply sprint stage change as</param>
    /// <param name="sprint">Sprint to update</param>
    /// <param name="newStage">New sprint stage</param>
    /// /// <returns>False if the update failed because of a concurrency exception, true otherwise.</returns>
    public async Task<bool> UpdateStage(User actingUser, Sprint sprint, SprintStage newStage)
    {
        var clonedSprint = sprint.CloneForPersisting();
        clonedSprint.Stage = newStage;

        List<SprintChangelogEntry> changes = new();
        changes.Add(new(actingUser, sprint, nameof(Sprint.Stage), Change<object>.Update(sprint.Stage, newStage)));
        
        try {
            await _sprintRepository.UpdateAsync(clonedSprint);    
            await _sprintChangelogRepository.AddAllAsync(changes);
            await _entityLiveUpdateService.BroadcastNewValueForEntityToProjectAsync(sprint.Id, clonedSprint.SprintProjectId, clonedSprint, actingUser.Id);
        } catch (DbUpdateConcurrencyException ex) {
            _logger.LogInformation("Update failed for sprint (name={SprintName}). Concurrency exception occurred: {ExMessage}", sprint.Name, ex.Message);
            return false;        
        }
        sprint.Stage = newStage;
        sprint.RowVersion = clonedSprint.RowVersion;
        sprint.EndDate = clonedSprint.EndDate;
        return true;
    }

    public async Task<Sprint> GetByIdAsync(long sprintId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Sprints.FirstOrDefaultAsync(x => x.Id == sprintId);
    }
}