using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using System.Threading.Tasks;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Extensions;
using ScrumBoard.Filters;
using ScrumBoard.Models;
using ScrumBoard.Repositories;
using ScrumBoard.Shared;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Pages
{
    public partial class Overhead : BaseProjectScopedComponent
    {
        /// <summary>
        /// Number of overhead entries to show per page
        /// </summary>
        private readonly int _pageSize = 25;

        [Inject]
        public IOverheadEntryRepository OverheadEntryRepository { get; set; }
        
        [Inject]
        public IOverheadSessionRepository OverheadSessionRepository { get; set; }

        [Inject]
        public IScrumBoardStateStorageService StateStorageService { get; set; }

        private int _currentPage = 1;

        private readonly OverheadEntryFilter _overheadEntryFilter = new();

        private Sprint _sprint;
        
        /// <summary>
        /// Summary only sprint that overhead time can be logged against
        /// </summary>
        private Sprint _loggableSprint;
        
        /// <summary>
        /// Current page of overhead entries
        /// </summary>
        private PaginatedList<OverheadEntry> _overheadEntries;

        /// <summary>
        /// Total time logged for all overhead entries that have been returned from the search
        /// </summary>
        private TimeSpan? _totalTime;

        /// <summary>
        /// Total time logged for overhead entries on the current page
        /// </summary>
        private TimeSpan TimeOnPage => _overheadEntries.Sum(entry => entry.Duration);
        
        private OverheadEntry _editingOverheadEntry;
        private ICollection<User> _members;

        private void AddOverhead()
        {
            _editingOverheadEntry = new OverheadEntry
            {
                Sprint = _sprint,
            };
        }

        private void StartEditingOverhead(OverheadEntry entry)
        {
            _editingOverheadEntry = entry;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if(RoleInCurrentProject is ProjectRole.Reviewer) NavigationManager.NavigateTo("", true);
            _members = Project.GetWorkingMembers().ToList();
            _overheadEntryFilter.OnUpdate += () => _ = UpdateOverhead();
            await RefreshSprint();
        }

        /// <summary>
        /// Queries the database for overhead entries and updates the total time
        /// </summary>
        private async Task UpdateOverhead()
        {
            var transforms = new[]
            {
                OverheadEntryIncludes.Session,
                OverheadEntryIncludes.User,
                query => query.Where(_overheadEntryFilter.Predicate),
                query => query.OrderByDescending(entry => entry.Occurred),
                _sprint != null ? OverheadEntryTransforms.FilterBySprint(_sprint) : OverheadEntryTransforms.FilterByProject(Project),
            };

            _overheadEntries = await OverheadEntryRepository.GetAllPaginatedAsync(_currentPage, _pageSize, transforms);
            if (!_overheadEntries.Any())
            {
                if (_overheadEntries.TotalPages == 0)
                {
                    _currentPage = 1;
                }
                else
                {
                    _currentPage = _overheadEntries.TotalPages;
                    _overheadEntries = await OverheadEntryRepository.GetAllPaginatedAsync(_currentPage, _pageSize, transforms);
                }
            }
            _totalTime = _overheadEntries.TotalCount == 0 ? 
                TimeSpan.Zero : 
                await OverheadEntryRepository.GetTotalTimeLogged(transforms);
            
            StateHasChanged();
        }
        
        private async Task OnOverheadClosed()
        {
            _editingOverheadEntry = null;
            await UpdateOverhead();
        }

        /// <summary>
        /// Selects and updates the current sprint being displayed, if null provided then whole project will be viewed instead
        /// If a overhead entry is currently being edited, then this function will not do anything
        /// </summary>
        /// <param name="sprint">Sprint to select, null for whole project</param>
        private async Task SelectSprint(Sprint sprint)
        {
            if (_editingOverheadEntry != null) return;
            _sprint = sprint;
            _currentPage = 1;
            await UpdateOverhead();
        }

        /// <summary>
        /// Changes the page and loads the overhead entries for that page
        /// </summary>
        /// <param name="newPage">Page number (1 indexed)</param>
        private async Task ChangePage(int newPage)
        {
            _currentPage = newPage;
            await UpdateOverhead();
        }
        
        private async Task RefreshSprint() {
            _editingOverheadEntry = null;
            
            // Find the most recent sprint that is started and not closed
            _loggableSprint = Project.Sprints.FirstOrDefault(sprint => sprint.TimeStarted.HasValue && sprint.Stage != SprintStage.Closed);
            // If no sprints match, then select the most recent created sprint
            _loggableSprint ??= Project.Sprints.FirstOrDefault(sprint => sprint.Stage == SprintStage.Created);

            if (_loggableSprint == null)
            {
                await SelectSprint(Project.Sprints.FirstOrDefault());
            }
            else
            {
                await SelectSprint(_loggableSprint);
            }
        }
        
        // Wrapper for StateHasChanged so it can be overridden by integration test
        protected virtual void NotifyStateChange()
        {
            StateHasChanged();
        }
    }
}