using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;

namespace ScrumBoard.Shared
{
    public partial class SprintChangelog
    {
        private ElementReference _root;

        [Parameter]
        public Sprint Sprint { get; set; }

        [Inject]
        protected ISprintChangelogRepository SprintChangelogRepository { get; set; }

        [Inject]
        protected IJsInteropService JSInteropService { get; set; }

        private List<IMessage> _changelog;

        /// <summary> 
        /// Calls the scroll to method on the JSInteropService to scroll to the root element reference.
        /// </summary>
        /// <returns>A task</returns>
        private async Task OnExpanded() {
            await JSInteropService.ScrollTo(_root);
        }

        protected override async Task OnParametersSetAsync()
        {
            base.OnParametersSet();
            if (_changelog != null)
            {
                await GenerateChangelog();
            }
        }

        /// <summary> 
        /// Checks if the changelog has already been loaded once, if not loads the sprint changelog.
        /// </summary>
        /// <returns>A task</returns>
        private async Task GenerateChangelogIfNeeded()
        {
            if (_changelog != null) return;
            await GenerateChangelog();
        }

        /// <summary> 
        /// Loads the sprint changelog from the database.
        /// </summary>
        /// <returns>A task</returns>
        private async Task GenerateChangelog()
        {
            var entries = await SprintChangelogRepository.GetBySprintAsync(Sprint, SprintChangelogIncludes.Creator, SprintChangelogIncludes.UserStory);
            _changelog = entries.Cast<IMessage>().ToList();   
            _changelog.Add(new CreatedMessage(Sprint.Created, Sprint.Creator, "sprint"));
        }
    }
}