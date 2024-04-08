using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Announcements;

namespace ScrumBoard.Shared.Announcements
{
    public partial class AnnouncementDisplay : ComponentBase
    {
        [Parameter]
        public Announcement Announcement { get; set; }    

        [Parameter]
        public EventCallback<Announcement> HideAnnouncementCallback { get; set; }

        private async Task Hide()
        {
            await HideAnnouncementCallback.InvokeAsync();
        }
    }
}