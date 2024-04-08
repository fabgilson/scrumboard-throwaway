using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Filters;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared
{
    public partial class WorklogTableData : BaseProjectScopedComponent
    {
        [Inject]
        protected IWorklogEntryService WorklogEntryService { get; set; }
        
        [CascadingParameter(Name = "Sprint")]
        public Sprint CurrentSprint { get; set; }

        [Parameter]
        public WorklogEntryFilter WorklogEntryFilter { get; set; }

        [Parameter]
        public EventCallback<(long, long)> TableEntryClicked { get; set; } 

        [Parameter]
        public IReadOnlyList<TableColumn> Columns { get; set; }
        
        /// <summary>
        /// Used to display all work logs when not being used for marking (on the Marking Summary page), and only work
        /// logs with issue tags when being used for marking.
        /// </summary>
        [Parameter]
        public bool IsMarkingTable { get; set; }

        private bool _descending = false;

        private bool _isLoading = true;

        private int _totalPages;

        /// <summary>
        /// First page = 1
        /// </summary>
        private int _currentPageNumber = 1;

        private int _pageSize = 10;

        private TableColumn _orderbyColumn = TableColumn.StoryName;

        private List<WorklogTableEntry> _worklogTableEntries = new();

        public string PersonalTime = "";

        public string FilteredUserTime = "";

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await RefreshTable();
        }

        /// <summary>
        /// Retrieves a paginated list of worklog entries based on the current page and orderBy values.
        /// The results are then used to generate a final list of worklog table entries to display.
        /// </summary>       
        /// <returns>An async Task</returns>
        public async Task RefreshTable() { 
            _isLoading = true;
            await GenerateTableEntries();            
            await GetSummaryTimeSpent();           
            _isLoading = false;
        }

        /// <summary> 
        /// Gets the worklog entries for the current page with the current filters from the database.
        /// </summary>      
        /// <returns>A task containing a PaginatedList of worklog entries</returns>
        private async Task<PaginatedList<WorklogEntry>> QueryWorklogEntries()
        {
            return await WorklogEntryService.GetByProjectFilteredAndPaginatedAsync(
                Project.Id,
                WorklogEntryFilter,
                _orderbyColumn,
                _descending,
                _currentPageNumber,
                _pageSize,
                CurrentSprint?.Id
            );
        }
        
        /// <summary>
        /// Retrieves a paginated list of worklog entries based on the current page and orderBy values.
        /// The results are then used to generate a final list of worklog table entries to display.
        /// </summary>       
        /// <returns>An async Task</returns>
        private async Task GenerateTableEntries() {     
            var worklogEntries = await QueryWorklogEntries();
            if (!worklogEntries.Any() && worklogEntries.HasPreviousPage)
            {
                _currentPageNumber = 1;
                worklogEntries = await QueryWorklogEntries();
            }
            _totalPages = worklogEntries.TotalPages;

            // Mapping from taskId to total time spent
            var totalTimeSpentMapping = new Dictionary<long, TimeSpan>();

            List<WorklogTableEntry> newTableEntries = new();
            foreach (WorklogEntry entry in worklogEntries) {
                List<User> assignees = new() { entry.User };
                if (entry.PairUser != null) {
                    assignees.Add(entry.PairUser);
                }    

                if (!totalTimeSpentMapping.TryGetValue(entry.Task.Id, out var totalTimeSpent))
                {
                    var allWorkInstancesOnTask = await WorklogEntryService.GetWorklogEntriesForTaskAsync(entry.TaskId);
                    totalTimeSpent = allWorkInstancesOnTask.Sum(x => x.GetTotalTimeSpent());
                    totalTimeSpentMapping.Add(entry.Task.Id, totalTimeSpent);
                }                
                
                TimeSpan timeRemaining = (entry.Task.Estimate - totalTimeSpent) > TimeSpan.Zero ? (entry.Task.Estimate - totalTimeSpent) : TimeSpan.Zero;

                var worklogIssues = await WorklogEntryService.GetIssuesForWorklogEntryAsync(entry.Id);
                WorklogTableEntry tableEntry = new() {
                    WorklogId = entry.Id,
                    StoryId = entry.Task.UserStory.Id,
                    TaskId = entry.Task.Id,
                    StoryName = entry.Task.UserStory.Name,
                    TaskName = entry.Task.Name,
                    OriginalEstimate = entry.Task.OriginalEstimate,
                    CurrentEstimate = entry.Task.Estimate,
                    TimeSpent = entry.GetTotalTimeSpent(),
                    TimeRemaining = timeRemaining,
                    TotalTimeSpent = totalTimeSpent,
                    Assignees = assignees,
                    TaskTags = entry.Task.Tags.ToList(), 
                    WorklogTags = entry.GetWorkedTags().ToList(),
                    IssueTags = worklogIssues.ToList(),
                    Occurred = entry.Occurred,
                    Created = entry.Created,
                    Description = entry.Description,
                };
                
                if (!IsMarkingTable || tableEntry.IssueTags.Any()) newTableEntries.Add(tableEntry);
            }            

            _worklogTableEntries = newTableEntries;
        }      

        /// <summary> 
        /// Changed the order of the table based on the given column and refreshes the table.
        /// </summary>
        /// <param name="column">The new column to order by</param>
        /// <param name="descending">A boolean, true if should be in descending order.</param>
        /// <returns>A task</returns>
        private async Task OrderWorklog(TableColumn column, bool descending) {
            _orderbyColumn = column;
            _descending = descending;
            await RefreshTable();

        }

        /// <summary> 
        /// Changed the page of the table based on the given page number and refreshes the table.
        /// </summary>       
        /// <param name="newPage">The new page number to change to</param>
        /// <returns>A task</returns>
        private async Task ChangePage(int newPage) {
            if (newPage > 0 && newPage <= _totalPages) {
                _currentPageNumber = newPage;
                await RefreshTable();
            }            
        }   

        /// <summary> 
        /// Calls the TableEntryClicked EventCallback with the given worklog table entry.
        /// </summary>       
        /// <param name="tableEntry">The WorklogTableEntry that was clicked.</param>
        /// <returns>A task</returns>
        private async Task HandleRowClicked(WorklogTableEntry tableEntry) {
            await TableEntryClicked.InvokeAsync((tableEntry.TaskId, tableEntry.WorklogId));
        }    

        /// <summary>
        /// Retrieves the total and personal time logged for the currently filtered worklog entries.
        /// </summary>    
        /// <returns>A task</returns>
        protected async Task GetSummaryTimeSpent()
        {
            var allFilteredWorklogs = (await WorklogEntryService.GetByProjectFilteredAsync(
                Project.Id,
                WorklogEntryFilter,
                CurrentSprint?.Id
            )).ToList();
            
            var filteredUserTime = allFilteredWorklogs
                .Where(x => x.UserId == Self.Id)
                .Sum(x => x.GetTotalTimeSpent());
            
            var personalTime = allFilteredWorklogs
                .Sum(x => x.GetTotalTimeSpent());

            FilteredUserTime = DurationUtils.DurationStringFrom(filteredUserTime);                  
            PersonalTime = DurationUtils.DurationStringFrom(personalTime);
        }
    }
}