using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared;

public class ProjectState
{
    public long ProjectId { get; init; }
    public bool IsReadOnly { get; init; }
    public ProjectRole ProjectRole { get; init; }
    public Project Project { get; init; }
}
    
public partial class ProjectStateContainer : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// The maximum time a project state can exist before being refreshed, regardless of change in project id
    /// </summary>
    private static TimeSpan _maximumProjectStateLifetime = TimeSpan.FromSeconds(5);
    private DateTime? _lastUpdated;
    
    [Inject]
    protected IClock Clock { get; set; }
    
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
        
    [Inject]
    protected IProjectRepository ProjectRepository { get; set; }
    
    [Inject]
    private IEntityLiveUpdateConnectionBuilder LiveUpdateConnectionBuilder { get; set; }
        
    [CascadingParameter(Name = "Self")]
    public User User { get; set; }
        
    [Parameter]
    public long? ProjectId { get; set; }
        
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private HubConnection _liveUpdateHubConnection;
    private EntityUpdateHubConnectionWrapper _liveUpdateHubConnectionWrapper;

    private ProjectState _projectState;

    private long? _previousProjectId;
    private bool _isAllowedToViewProject;

    protected override async Task OnParametersSetAsync()
    {
        // Not on a project scoped page, don't attempt to load a project
        if (ProjectId is null) return;
        if (ProjectId != _previousProjectId)
        {
            _isAllowedToViewProject = false;
            _previousProjectId = ProjectId;
        }
        
        if (_liveUpdateHubConnection == null)
        {
            _liveUpdateHubConnection = await LiveUpdateConnectionBuilder.CreateHubConnectionForProjectAsync(ProjectId.Value);
            await _liveUpdateHubConnection.StartAsync();
            _liveUpdateHubConnectionWrapper = new EntityUpdateHubConnectionWrapper(_liveUpdateHubConnection);
        }
        
        // If the project ID hasn't changed and the current value hasn't expired yet, don't refresh the project
        if(_projectState?.Project is not null 
            && _projectState.ProjectId == ProjectId
            && _lastUpdated is not null 
            && Clock.Now <= _lastUpdated.Value.Add(_maximumProjectStateLifetime)
        ) return;
        
        // Project needs to be refreshed, so we keep track of when it was last updated
        _lastUpdated = Clock.Now;
        
        var project = await ProjectRepository.GetByIdAsync(ProjectId.Value,
            ProjectIncludes.Member,
            ProjectIncludes.Sprints,
            ProjectIncludes.Backlog
        );
        var role = User.GetProjectRole(project);
        if (role is null)
        {
            _isAllowedToViewProject = false;
            NavigationManager.NavigateTo(PageRoutes.ToRoot(), true);
            return;
        }

        _isAllowedToViewProject = true;
        _projectState = new ProjectState
        {
            ProjectId = ProjectId.Value,
            IsReadOnly = role is ProjectRole.Reviewer or ProjectRole.Guest,
            ProjectRole = role!.Value,
            Project = project
        };
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_liveUpdateHubConnection != null)
        {
            await _liveUpdateHubConnection.DisposeAsync();
            _liveUpdateHubConnectionWrapper = null;
        }
        GC.SuppressFinalize(this);
    }
}