using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Changelog;
using System.Threading.Tasks;
using ScrumBoard.Models.Messages;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages
{
    public partial class ProjectChangelog : BaseProjectScopedComponent
    {
        [Inject]
        protected IProjectChangelogRepository ProjectChangelogRepository { get; set; }

        private List<IMessage> _changelogEntries = new();
        private bool _isInitialized;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await GetProjectChangelog();
            _isInitialized = true;
        }

        private async Task GetProjectChangelog() {
            IEnumerable<ChangelogEntry> changelog = await ProjectChangelogRepository.GetOrderedProjectChangelogsByProjectAsync(Project, 
                ProjectChangelogIncludes.Creator,
                ProjectChangelogIncludes.RelatedUser
            ); 
            _changelogEntries = changelog.Cast<IMessage>().ToList(); 
            _changelogEntries.Add(new CreatedMessage(Project.Created, Project.Creator, "project"));              
        }
    }
}