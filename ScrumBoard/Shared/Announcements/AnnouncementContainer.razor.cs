using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.Announcements
{
    public partial class AnnouncementContainer : ComponentBase
    {
        [CascadingParameter(Name="Self")]
        public User Self { get; set; }
        
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Inject]
        protected IAnnouncementService AnnouncementService { get; set; }
        
        private EventCallback ForceRefreshCallback => new(this, (Func<Task>)RefreshAnnouncements);
        
        private ICollection<Announcement> _announcements = new List<Announcement>();

        protected override async Task OnParametersSetAsync()
        {
            await RefreshAnnouncements();
        }

        public async Task RefreshAnnouncements()
        {
            _announcements = await AnnouncementService.GetActiveAnnouncementsForUserAsync(Self.Id);
        }

        private async Task HideAnnouncement(long announcementId)
        {
            await AnnouncementService.HideAnnouncementForUserAsync(announcementId, Self.Id);
            await RefreshAnnouncements();
            StateHasChanged();
        }
    }
}