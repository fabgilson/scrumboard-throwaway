using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Widgets;

public struct SprintSelection
{
    public bool isWholeProject;
    public Sprint sprint;
}

public partial class SprintSelector : ComponentBase
{
    private string ProjectSelectorCss => SprintSelection.isWholeProject 
        ? "btn border dropdown-toggle d-block btn-primary" 
        : "btn border dropdown-toggle d-block";

    [Parameter]
    public SprintSelection SprintSelection { get; set; }
    
    [Parameter]
    public EventCallback<SprintSelection> SprintSelectionChanged { get; set; }
    
    [Parameter]
    public EventCallback AfterSelectionChangedCallback { get; set; }

    [Parameter, EditorRequired]
    public IEnumerable<Sprint> AvailableSprints { get; set; }

    [Parameter]
    public Func<bool> WholeProjectOptionIsDisabledDelegate { get; set; }

    [Parameter] 
    public bool ShowWholeProjectSelection { get; set; } = true;

    private async Task UpdateSelection(bool isWholeProject, Sprint sprint)
    {
        SprintSelection = new SprintSelection { isWholeProject = isWholeProject, sprint = sprint };
        await SprintSelectionChanged.InvokeAsync(SprintSelection);
        await AfterSelectionChangedCallback.InvokeAsync();
    }
}