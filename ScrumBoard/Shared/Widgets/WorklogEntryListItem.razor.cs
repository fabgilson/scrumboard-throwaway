using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Services;
using System.Linq;
using ScrumBoard.Repositories;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Models.Entities.UsageData;

namespace ScrumBoard.Shared.Widgets
{
    public partial class WorklogEntryListItem
    {
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }    
        
        [CascadingParameter(Name = "ProjectState")]
        public ProjectState ProjectState { get; set; }
        
        [Parameter]
        public WorklogEntry Entry { get; set; }  

        [Parameter]
        public bool IsEditing { get; set; } 

        [Parameter]
        public EventCallback<WorklogEntry> EditWorklog { get; set; }

        [Parameter]
        public bool StartExpanded { get; set; } = false;

        [Parameter]
        public bool ShowTaskName { get; set; } = false;

        [CascadingParameter(Name="BoundaryElementId")]
        public string Boundary { get; set; } = "clippingParents";

        [Inject]
        protected IWorklogEntryChangelogRepository WorklogEntryChangelogRepository { get; set; }

        private List<User> _users = new();

        private List<User> _pairUsers = new();
        
        private bool IsExpanded { get; set; }
        
        private bool _showChangelog = false;

        private bool _isReadOnly;

        private List<WorklogEntryChangelogEntry> _changelogEntries = new();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        private async Task LoadChangelog()
        {
            _changelogEntries = await WorklogEntryChangelogRepository.GetByWorklogEntryAsync(Entry, WorklogEntryChangelogIncludes.Display);
        }

        protected override async Task OnParametersSetAsync()
        {
            _isReadOnly = ProjectState.IsReadOnly || (ProjectState.ProjectRole != ProjectRole.Leader && Entry.UserId != Self.Id);
            _users = new() { Entry.User };
            _pairUsers.Clear();
            if (Entry.PairUser != null) {
                _pairUsers.Add(Entry.PairUser);
            }       
            if (_showChangelog) {
                await LoadChangelog();
            }
            IsExpanded = StartExpanded;
        }

        private async Task ToggleChangelog() {
            _showChangelog = !_showChangelog;
            if (_showChangelog) {
                await LoadChangelog();
            }
        }
    }
}