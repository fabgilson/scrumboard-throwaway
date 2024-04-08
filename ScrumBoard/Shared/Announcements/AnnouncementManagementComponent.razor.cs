using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Services;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared.Announcements
{
    public partial class AnnouncementManagementComponent
    {
        private const int PageSize = 5;
        
        [CascadingParameter(Name = "ForceAnnouncementRefresh")]
        public EventCallback ForceAnnouncementRefresh { get; set; }
        
        [Inject]
        private IAnnouncementService AnnouncementService { get; set; }
        
        [Inject]
        private NavigationManager NavigationManager { get; set; }
        
        private EditAnnouncementForm CreateAnnouncementComponent { get; set; }
        
        private PaginatedList<Announcement> _upcomingAnnouncements = PaginatedList<Announcement>.Empty(PageSize);
        private bool _upcomingIsLoading = true;

        private PaginatedList<Announcement> _currentAnnouncements = PaginatedList<Announcement>.Empty(PageSize);
        private bool _currentIsLoading = true;

        private PaginatedList<Announcement> _expiredOrArchivedAnnouncements = PaginatedList<Announcement>.Empty(PageSize);
        private bool _expiredOrArchivedIsLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await RefreshAllLists();
        }

        private async Task RefreshHeaderAndLists()
        {
            await ForceAnnouncementRefresh.InvokeAsync();
            await RefreshAllLists();
        }

        private async Task RefreshAllLists()
        {
            var upcoming = RefreshUpcomingAnnouncements(_upcomingAnnouncements.PageNumber);
            var current = RefreshCurrentAnnouncements(_currentAnnouncements.PageNumber);
            var expiredOrArchived = RefreshExpiredOrArchivedAnnouncements(_expiredOrArchivedAnnouncements.PageNumber);

            await Task.WhenAll(upcoming, current, expiredOrArchived);
        }

        private async Task RefreshUpcomingAnnouncements(int pageNumber)
        {
            _upcomingIsLoading = true;
            _upcomingAnnouncements = await AnnouncementService.GetUpcomingAnnouncementsAsync(pageNumber, PageSize);
            _upcomingIsLoading = false;
            StateHasChanged();
        }
        
        private async Task RefreshCurrentAnnouncements(int pageNumber)
        {
            _currentIsLoading = true;
            _currentAnnouncements = await AnnouncementService.GetActiveAnnouncementsAsync(pageNumber, PageSize);
            _currentIsLoading = false;
            StateHasChanged();
        }
        
        private async Task RefreshExpiredOrArchivedAnnouncements(int pageNumber)
        {
            _expiredOrArchivedIsLoading = true;
            _expiredOrArchivedAnnouncements = await AnnouncementService.GetExpiredOrArchivedAnnouncementsAsync(pageNumber, PageSize);
            _expiredOrArchivedIsLoading = false;
            StateHasChanged();
        }

        private async Task ArchiveAnnouncement(Announcement announcement)
        {
            announcement.ManuallyArchived = true;
            await AnnouncementService.UpdateExistingAnnouncementAsync(announcement);
            await RefreshAllLists();
            await ForceAnnouncementRefresh.InvokeAsync();
        }
    }
}