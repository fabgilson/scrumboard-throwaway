using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Report;
using ScrumBoard.Shared.Widgets;

namespace ScrumBoard.Pages
{
    public partial class Report : BaseProjectScopedComponent
    {
        private IEnumerable<Sprint> AvailableSprints => Project?.Sprints?.Where(sprint => sprint.TimeStarted.HasValue) ?? new List<Sprint>();

        private SprintSelection _sprintSelection;
        
        [Parameter]
        public int? ReportTypeParam { get; set; }

        private User _selectedUser;

        private MyStatistics _myStats;
        private MyReflectionCheckIns _myWeeklMyReflectionCheckIns;
        private MarkingStats _markingStats;
        
        [Inject]
        protected IConfigurationService ConfigurationService { get; set; }
        
        /// <summary>
        /// If we have been given a valid report type parameter, use that, otherwise default to:
        /// MyStatistics (for developers), ProjectStatistics (for leaders), or worklog (everyone else)
        /// </summary>
        private ReportType ReportType 
        {
            get
            {
                if (ReportTypeParam is not null && Enum.IsDefined(typeof(ReportType), ReportTypeParam))
                {
                    return (ReportType) ReportTypeParam.Value;
                }
                return RoleInCurrentProject switch
                {
                    ProjectRole.Developer => ReportType.MyStatistics,
                    ProjectRole.Leader => ReportType.ProjectStatistics,
                    _ => ReportType.WorkLog
                };
            }
        }

        private async Task HandleUserChanged(User user)
        {
            _selectedUser = user;
            switch (ReportType)
            {
                case ReportType.MyStatistics:
                    await _myStats.ChangeUser(user);
                    break;
                case ReportType.MyWeeklyReflections:
                    await _myWeeklMyReflectionCheckIns.ChangeUser(user);
                    break;
                case ReportType.MarkingStats:
                    await _markingStats.ChangeUser(user);
                    break;
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            RefreshSprint();
            if (ReportTypeParam is null) NavigationManager.NavigateTo(PageRoutes.ToProjectReport(ProjectState.ProjectId, ReportType), false);
        }

        private void ReportChanged(ReportType reportType)
        {
            NavigationManager.NavigateTo(PageRoutes.ToProjectReport(ProjectState.ProjectId, reportType));
        }

        private void RefreshSprint() 
        {
            var currentSprint = Project.GetCurrentSprint();
            if (currentSprint?.TimeStarted is not null) 
            {
                _sprintSelection = new SprintSelection { isWholeProject = false, sprint = currentSprint };
            } else {
                _sprintSelection = new SprintSelection { 
                    isWholeProject = false, 
                    sprint = AvailableSprints.MaxBy(sprint => sprint.EndDate) 
                };                         
            }
            
            StateHasChanged();
        }
    }
}