using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.ProjectFeatureFlags;

public partial class ProjectFeatureFlagRequiredComponent : ComponentBase
{
    [Parameter, EditorRequired]
    public FeatureFlagDefinition RequiredFeatureFlag { get; set; }
    
    [CascadingParameter(Name = "ProjectState")]
    public ProjectState ProjectState { get; set; }
    
    [Inject]
    public IProjectRepository ProjectRepository { get; set; }
    
    [Inject]
    public IProjectFeatureFlagService ProjectFeatureFlagService { get; set; }
    
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private bool _isEnabled;
    
    [Parameter]
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value ) return;
            _isEnabled = value;
            IsEnabledChanged.InvokeAsync(value);
        }
    }
    
    [Parameter]
    public EventCallback<bool> IsEnabledChanged { get; set; }
    
    protected override async Task OnParametersSetAsync()
    {
        if (ProjectState?.Project is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ProjectFeatureFlagRequiredComponent)} requires a current project to be set " +
                $"and accessible via {nameof(ProjectStateContainer)}"
            );
        }
        IsEnabled = await ProjectFeatureFlagService.ProjectHasFeatureFlagAsync(ProjectState.Project, RequiredFeatureFlag);
    }
}