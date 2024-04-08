using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Filters;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;

namespace ScrumBoard.Shared;

public partial class WorklogTable : BaseProjectScopedComponent
{

    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }

    [Inject]
    protected IScrumBoardStateStorageService StateStorageService { get; set; }
    
    [Inject]
    protected IWorklogEntryService WorklogEntryService { get; set; }

    [Inject]
    protected IUserStoryTaskRepository UserStoryTaskRepository { get; set; }
        
    [Inject]
    protected IUserStoryTaskTagRepository UserStoryTaskTagRepository { get; set; }
        
    [Inject]
    protected IWorklogTagRepository WorklogTagRepository { get; set; }
    
    /// <summary>
    /// Used to display work log issue tags when on the marking Summary page, and not when on the Work Log page.
    /// Also passed to the WorklogTableData component.
    /// </summary>
    [Parameter]
    public bool IsMarkingTable { get; set; }
    
    /// <summary>
    /// Used to filter work logs in the work log summary page by default, to only show work logs relevant to the current user.
    /// Only affects the table when IsMarkingTable is true.
    /// </summary>
    [Parameter]
    public User SelectedUser { get; set; }

    // Summary Section Data
    private int _numberStoriesWorked = 0;
        
    private TimeSpan _totalTime = TimeSpan.Zero;        

    private int _totalStoryPoints = 0;

    // Filter attributes
    private readonly WorklogEntryFilter _worklogEntryFilter = new();

    private List<User> _members = [];

    private StoryTaskDisplayPanel _storyTaskPanel;

    private List<TableColumnConfiguration> _columnConfiguration;
    
    private DateOnly EarliestDate => Sprint?.StartDate ?? DateOnly.FromDateTime(DateTime.Now);
    

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await SetupColumnConfiguration();
        await GenerateSummary();
        
        _members = Project.GetWorkingMembers().ToList();
    }

    private async Task SetupColumnConfiguration()
    {
        if(_columnConfiguration is not null) return;
        var storedConfig = await StateStorageService.GetTableColumnConfiguration() ?? [];
        
        // Add any columns that exist but weren't found in the stored configuration
        storedConfig.AddRange(
            Enum.GetValues<TableColumn>()
                .Except(storedConfig.Select(config => config.Column))
                .Select(column => new TableColumnConfiguration { Hidden = false, Column = column })
        );
        
        // If we are not in marking mode, remove any columns that should only be shown in marking mode
        storedConfig.RemoveAll(x => !IsMarkingTable && x.Column.IsForMarkingTableOnly());
        
        _columnConfiguration = storedConfig;
    }
        
    /// <summary>
    /// Handler for change in current project. Attempts to obtain the current sprint of the new project. 
    /// If the current sprint exists, generate the summary.
    /// </summary>
    private async Task HandleProjectChanged() {  
        await GenerateSummary();
        StateHasChanged();
    }

    /// <summary>
    /// Gets all worklog entries from either the current sprint or all sprints and sets attributes required for the summary element. 
    /// 
    /// Sets the total number of stories worked on,
    /// sets the total number of story points from the worked on stories,
    /// sets the total amount of hours contributed in worklogs. 
    /// </summary>
    private async Task GenerateSummary() {
        if (IsMarkingTable) _worklogEntryFilter.AssigneeFilter = new List<User> { SelectedUser };
        
        _numberStoriesWorked = 0;
        _totalTime = TimeSpan.Zero;
        _totalStoryPoints = 0;

        var worklogEntries = await WorklogEntryService.GetWorklogEntriesForProjectAsync(Project.Id, Sprint?.Id);
        var worklogEntriesArray = worklogEntries.ToArray();
        var storiesWorked = worklogEntriesArray.Select(e => e.Task.UserStory).Distinct().ToList();
        
        _numberStoriesWorked = storiesWorked.Count;
        _totalTime = worklogEntriesArray.Select(e => e.GetTotalTimeSpent()).Sum();
        _totalStoryPoints = storiesWorked.Select(s => s.Estimate).Sum();
    }

    /// <summary>
    /// Handler for TableEntryClicked event.
    /// </summary>
    /// <param name="ids">Tuple of (TaskId, WorklogId) for entry that has been clicked</param>
    private async Task ViewWorklogTask((long, long) ids) {
        var (selectedTaskId, selectedWorklogId) = ids;
        if (selectedTaskId != 0) {
            var selectedTask = await UserStoryTaskRepository.GetByIdAsync(selectedTaskId);
            var selectedEntry = await WorklogEntryService.GetWorklogEntryByIdAsync(selectedWorklogId);
            await _storyTaskPanel.SelectTaskAndWorklog(selectedTask, selectedEntry);
            StateHasChanged();
        }
    }

    /// <summary> 
    /// Updates the column configuration of the table (the order of the columns)
    /// Also stores the configuration in the browser's localStorage.
    /// </summary>
    /// <param name="configuration">A list of TableColumnConfiguration entities to update to</param>
    /// <returns>A task</returns>
    private async Task UpdateColumnConfiguration(List<TableColumnConfiguration> configuration)
    {
        _columnConfiguration = configuration;
        StateHasChanged();
        await StateStorageService.SetTableColumnConfiguration(configuration);
    }

    /// <summary> 
    /// Updates the column configuration of the table (whether the column should be hidden)
    /// Also stores the configuration in the browser's localStorage.
    /// </summary>
    /// <param name="column">A TableColumnConfiguration entity to update</param>
    /// <returns>A task</returns>
    private async Task ToggleColumnHidden(TableColumnConfiguration column)
    {
        column.Hidden = !column.Hidden;
        StateHasChanged();
        await StateStorageService.SetTableColumnConfiguration(_columnConfiguration);
    }
    
    private IReadOnlyList<TableColumn> GetColumns()
    {
        return(_columnConfiguration ?? [])
            .Where(config => !config.Hidden)
            .Select(config => config.Column)
            .ToList();
    }
}