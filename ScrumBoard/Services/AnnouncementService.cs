using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Repositories;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services
{
    public interface IAnnouncementService
    {
        Task AddNewAnnouncementAsync(Announcement announcement);
        Task UpdateExistingAnnouncementAsync(Announcement announcement);
        Task HideAnnouncementForUserAsync(long announcementId, long userId);
        Task<ICollection<Announcement>> GetActiveAnnouncementsForUserAsync(long userId);
        Task<PaginatedList<Announcement>> GetActiveAnnouncementsAsync(int pageNumber, int pageSize);
        Task<PaginatedList<Announcement>> GetExpiredOrArchivedAnnouncementsAsync(int pageNumber, int pageSize);
        Task<PaginatedList<Announcement>> GetUpcomingAnnouncementsAsync(int pageNumber, int pageSize);
    }

    public class AnnouncementService : IAnnouncementService
    {
        private readonly IAnnouncementRepository _announcementRepository;

        public AnnouncementService(IAnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        /// <summary>
        /// Adds a new announcement to the system.
        /// </summary>
        /// <param name="announcement">Announcement to add.</param>
        public async Task AddNewAnnouncementAsync(Announcement announcement)
        {
            announcement.Created = DateTime.Now;
            await _announcementRepository.AddAsync(announcement);
        }

        /// <summary>
        /// Gets all announcements that should be shown to the user with given ID.
        /// </summary>
        /// <param name="userId">ID of user to fetch announcements for</param>
        /// <returns>Collection of active announcements for user</returns>
        public async Task<ICollection<Announcement>> GetActiveAnnouncementsForUserAsync(long userId)
        {
            return await _announcementRepository.GetActiveAnnouncementsForUserAsync(userId);
        }

        /// <summary>
        /// Gets paginated list of  announcements that are currently active. I.e they are not manually
        /// archived, are beyond their start date (or have no start date), and are before their end date
        /// (or have no end date). Ordered by recently created -> older
        /// </summary>
        /// <param name="pageNumber">Index of page to get, first page has number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>All currently active announcements</returns>
        public async Task<PaginatedList<Announcement>> GetActiveAnnouncementsAsync(int pageNumber, int pageSize)
        {
            return await _announcementRepository.GetPaginatedActiveAnnouncementsAsync(pageNumber, pageSize);
        }

        /// <summary>
        /// Gets all announcements that have been manually archived, or are expired (i.e their end
        /// date is in the past). Results are paginated as there could be potentially many, and are
        /// ordered from most recently created -> oldest.
        /// </summary>
        /// <param name="pageNumber">Index of page to get, first page has number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>Paginated list of expired or archived announcements, most recently created first</returns>
        public async Task<PaginatedList<Announcement>> GetExpiredOrArchivedAnnouncementsAsync(int pageNumber, int pageSize)
        {
            return await _announcementRepository.GetPaginatedExpiredOrManuallyArchivedAnnouncementsAsync(pageNumber, pageSize);
        }

        /// <summary>
        /// Gets all announcements that are yet to occur. I.e their start date is in the future
        /// and they have not been manually archived.
        /// </summary>
        /// <param name="pageNumber">Index of page to get, first page has number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>Paginated list of upcoming announcements, ordered with soonest to occur first</returns>
        public async Task<PaginatedList<Announcement>> GetUpcomingAnnouncementsAsync(int pageNumber, int pageSize)
        {
            return await _announcementRepository.GetPaginatedUpcomingAnnouncementsAsync(pageNumber, pageSize);
        }

        /// <summary>
        /// Hides an announcement for a user.
        /// </summary>
        /// <param name="announcementId">ID of announcement to hide</param>
        /// <param name="userId">ID of user who is hiding the announcement</param>
        public async Task HideAnnouncementForUserAsync(long announcementId, long userId)
        {
            await _announcementRepository.AddAnnouncementHideAsync(new AnnouncementHide() {
                Created = DateTime.Now,
                AnnouncementId = announcementId,
                UserId = userId
            });
        }

        /// <summary>
        /// Updates an already existing announcement and sets the LastEdited timestamp.
        /// </summary>
        /// <param name="announcement">Existing announcement to update</param>
        public async Task UpdateExistingAnnouncementAsync(Announcement announcement)
        {
            announcement.LastEdited = DateTime.Now;
            await _announcementRepository.UpdateAsync(announcement);
        }
    }
}