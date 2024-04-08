using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;

namespace ScrumBoard.Shared;

public class BaseProjectScopedComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// The currently logged in user entity
    /// </summary>
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }
        
    /// <summary>
    /// Some basic information about the current project state, if we are currently within a project scope.
    /// If we are not within a project scope (i.e URL doesn't begin with /project/{id}) then this is null.
    /// </summary>
    [CascadingParameter(Name = "ProjectState")]
    public ProjectState ProjectState { get; set; }
    
    /// <summary>
    /// Receives
    /// </summary>
    [CascadingParameter(Name = "LiveUpdateHubConnectionWrapper")]
    public EntityUpdateHubConnectionWrapper LiveUpdateHubConnectionWrapper { private get; set; }
    private readonly List<IDisposable> _handlers = [];
    
    /// <summary>
    /// We need to have somewhere for the ProjectId value to be written from parameter route,
    /// but we'll effectively hide this for inheriting classes to force them to use the more
    /// complete ProjectState property.
    /// </summary>
    [Parameter]
    public long ProjectId { private get; init; }
    
    [Inject]
    protected IProjectRepository ProjectRepository { get; set; }
    
    [Inject]
    protected ILogger<BaseProjectScopedComponent> Logger { get; set; }
    
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    
    [Inject]
    protected IEntityLiveUpdateService EntityLiveUpdateService { get; set; }

    /// <summary>
    /// The current scope project, to refresh its value, call <see cref="RefreshProjectAndEnforcePermissionsAsync"/>
    /// </summary>
    protected Project Project { get; private set; }
    protected ProjectRole? RoleInCurrentProject => ProjectState?.ProjectRole;

    private bool _lastBroadcastWasForEditingStarted;

    /// <summary>
    /// Refreshes the underlying project for class to use, and enforces permissions to make sure that user is allowed
    /// to access this page.
    /// </summary>
    private async Task RefreshProjectAndEnforcePermissionsAsync()
    {
        if (ProjectState is null)
        {
            throw new InvalidOperationException(
                "Cascading ProjectState not found. Ensure that this method is only called after parameters have been set," +
                "and that the user is within a project's scope (i.e URL is /project/{id}/...)."
            );
        }

        if(ProjectState.Project is null) Logger.LogWarning("Project state has been lost, re-fetching...");

        await RefreshProject();

        if (Project is null)
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access non-existing project with ID={ProjectStateProjectId}", Self.Id, ProjectState.ProjectId);
            NavigationManager.NavigateTo("", true);
            return;
        }

        if (!Self.CanView(Project))
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access a project (ID={ProjectStateProjectId}) without sufficient permissions", Self.Id, ProjectState.ProjectId);
            NavigationManager.NavigateTo("", true);
        }            
    }

    protected async Task RefreshProject(bool forceRefresh=false)
    {
        Project = (forceRefresh ? null : ProjectState.Project) ?? await ProjectRepository.GetByIdAsync(
            ProjectState.ProjectId, 
            ProjectIncludes.Member,
            ProjectIncludes.Sprints, 
            ProjectIncludes.Backlog
        );
    }
    
    protected override async Task OnParametersSetAsync()
    {
        // If we've already loaded the project, and we are still looking at the same project, don't refresh again
        if(Project is not null && ProjectState is not null && Project.Id == ProjectState.ProjectId) return;
        await RefreshProjectAndEnforcePermissionsAsync();
    }

    /// <summary>
    /// Registers a handler for live entity updates, allowing the calling class to define
    /// an asynchronous update handling mechanism for a specific entity type and ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for updates.</param>
    /// <param name="onUpdateHandler">The asynchronous function to handle the update.</param>
    protected void RegisterNewLiveEntityUpdateHandler<TEntity>(long entityId, Func<TEntity, long, Task> onUpdateHandler) where TEntity : IId
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnUpdateReceivedForEntityWithId(entityId, (TEntity entity, long l) =>
        {
            _lastBroadcastWasForEditingStarted = false;
            return onUpdateHandler(entity, l);
        }, GetType()));
    }
    
    /// <summary>
    /// Registers a handler for live entity updates, allowing the calling class to define
    /// a synchronous update handling mechanism for a specific entity type and ID.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for updates.</param>
    /// <param name="onUpdateHandler">The action to handle the update.</param>
    protected void RegisterNewLiveEntityUpdateHandler<TEntity>(long entityId, Action<TEntity, long> onUpdateHandler) where TEntity : IId
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnUpdateReceivedForEntityWithId(entityId, (TEntity entity, long userId) => 
        {
            _lastBroadcastWasForEditingStarted = false;
            onUpdateHandler(entity, userId);
            return Task.CompletedTask;
        }, GetType()));
    }

    /// <summary>
    /// Broadcasts a notification indicating that an update has begun on a specific entity. Unless the <see cref="forceBroadcast"/>
    /// parameter is given, this method will only send a broadcast if that was not the last message it sent. I.e. it can be safely bound
    /// to an @oninput handler without spamming broadcasts on every keystroke.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity being updated.</param>
    /// <param name="forceBroadcast">If true, will force a broadcast even there was one recently</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task BroadcastUpdateBegun<TEntity>(long entityId, bool forceBroadcast=false) where TEntity : IId
    {
        if(_lastBroadcastWasForEditingStarted && !forceBroadcast) return;
        _lastBroadcastWasForEditingStarted = true;
        await EntityLiveUpdateService.BroadcastUpdateStartedOnEntityToProject<TEntity>(entityId, Project.Id, Self.Id);
    }
    
    /// <summary>
    /// Registers a listener for when some entity has had its state changed. This method does not in any way receive
    /// the new state, but notifies clients that they may wish to refresh the entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity that has changed.</typeparam>
    /// <param name="entityId">The ID of the entity for which to listen for changes.</param>
    /// <param name="onEntityChangedHandler">The asynchronous function to perform when entity change notification is received.</param>
    protected void RegisterListenerForEntityChanged<TEntity>(long entityId, Func<Task> onEntityChangedHandler)
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnEntityWithIdHasChanged<TEntity>(entityId, onEntityChangedHandler, GetType()));
    }
    
    /// <summary>
    /// Registers a listener for when some entity has had its state changed. This method does not in any way receive
    /// the new state, but notifies clients that they may wish to refresh the entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity that has changed.</typeparam>
    /// <param name="entityId">The ID of the entity for which to listen for changes.</param>
    /// <param name="onEntityChangedHandler">The synchronous function to perform when entity change notification is received.</param>
    protected void RegisterListenerForEntityChanged<TEntity>(long entityId, Action onEntityChangedHandler)
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnEntityWithIdHasChanged<TEntity>(entityId, () =>
        {
            onEntityChangedHandler();
            return Task.CompletedTask;
        }, GetType()));
    }
    
    /// <summary>
    /// Registers a listener for when an update begins on an entity, allowing the calling class
    /// to define an asynchronous handling mechanism.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for update commencement.</param>
    /// <param name="onUpdateBegunByUserHandler">The asynchronous function to handle the beginning of the update.</param>
    protected void RegisterListenerForUpdateBegun<TEntity>(long entityId, Func<long, Task> onUpdateBegunByUserHandler)
    {
        LiveUpdateHubConnectionWrapper.OnUpdateBegunForEntityByUser<TEntity>(entityId, onUpdateBegunByUserHandler, GetType());
    }
    
    /// <summary>
    /// Registers a listener for when an update begins on an entity, allowing the calling class
    /// to define a synchronous handling mechanism.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for update commencement.</param>
    /// <param name="onUpdateBegunByUserHandler">The action to handle the beginning of the update.</param>
    protected void RegisterListenerForUpdateBegun<TEntity>(long entityId, Action<long> onUpdateBegunByUserHandler)
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnUpdateBegunForEntityByUser<TEntity>(entityId, userId => 
        {
            onUpdateBegunByUserHandler(userId);
            return Task.CompletedTask;
        }, GetType()));
    }
    
    /// <summary>
    /// Broadcasts a notification indicating that an update has ended on a specific entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity whose update has ended.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task BroadcastUpdateEnded<TEntity>(long entityId) where TEntity : IId
    {
        _lastBroadcastWasForEditingStarted = false;
        await EntityLiveUpdateService.BroadcastUpdateEndedOnEntityToProject<TEntity>(entityId, Project.Id, Self.Id);
    }
    
    /// <summary>
    /// Registers a listener for when an update ends on an entity, allowing the calling class
    /// to define an asynchronous handling mechanism.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for update completion.</param>
    /// <param name="onUpdateEndedByUserHandler">The asynchronous function to handle the end of the update.</param>
    protected void RegisterListenerForUpdateEnded<TEntity>(long entityId, Func<long, Task> onUpdateEndedByUserHandler)
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnUpdateEndedForEntityByUser<TEntity>(entityId, onUpdateEndedByUserHandler, GetType()));
    }
    
    /// <summary>
    /// Registers a listener for when an update ends on an entity, allowing the calling class
    /// to define a synchronous handling mechanism.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entityId">The ID of the entity to listen for update completion.</param>
    /// <param name="onUpdateEndedByUserHandler">The action to handle the end of the update.</param>
    protected void RegisterListenerForUpdateEnded<TEntity>(long entityId, Action<long> onUpdateEndedByUserHandler)
    {
        _handlers.Add(LiveUpdateHubConnectionWrapper.OnUpdateEndedForEntityByUser<TEntity>(entityId, userId => 
        {
            onUpdateEndedByUserHandler(userId);
            return Task.CompletedTask;
        }, GetType()));
    }

    public void Dispose()
    {
        if(_handlers.Count == 0) return;
        foreach (var handler in _handlers)
        {
            handler?.Dispose();
        }
        _handlers.Clear();
        GC.SuppressFinalize(this);
    }
}