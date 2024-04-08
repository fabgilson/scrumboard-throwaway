using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities.Announcements;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<Announcement>, IQueryable<Announcement>>;

    public interface IAnnouncementRepository : IRepository<Announcement>
    {
        Task<PaginatedList<Announcement>> GetPaginatedUpcomingAnnouncementsAsync(int pageNumber, int pageSize);
        Task<PaginatedList<Announcement>> GetPaginatedActiveAnnouncementsAsync(int pageNumber, int pageSize);
        Task<ICollection<Announcement>> GetActiveAnnouncementsForUserAsync(long userId);
        Task AddAnnouncementHideAsync(AnnouncementHide announcementHide);
        Task<PaginatedList<Announcement>> GetPaginatedExpiredOrManuallyArchivedAnnouncementsAsync(int pageNumber, int pageSize);
    }

    public static class AnnouncementTransforms
    {
        public static TransformType IncludeCreator() => query => query.Include(a => a.Creator);
        public static TransformType IncludeEditor() => query => query.Include(a => a.LastEditor);
        
        public static TransformType IsNotHiddenByUser(long userId) => query 
            => query.Where(
                    // Can't be hidden; or,
                    announcement => !announcement.CanBeHidden 
                    // Is hideable but not hidden by user
                    || (announcement.CanBeHidden && announcement.Hides.All(h => h.UserId != userId))
                );

        public static TransformType IsActive() => query
            => query.Where(announcement => 
                // Is after start date, or no start date is given; and,
                (!announcement.Start.HasValue || announcement.Start < DateTime.Now)
                // Is before end date, or no end date is given; and,
                && (!announcement.End.HasValue || announcement.End > DateTime.Now)
                // Is not manually archived
                && !announcement.ManuallyArchived
            );
        
        public static TransformType IsExpiredOrManuallyArchived() => query
            => query.Where(announcement => 
                // End date is in the past; or,
                (announcement.End.HasValue && announcement.End < DateTime.Now)
                // Is manually archived
                || announcement.ManuallyArchived
            );
        
        public static TransformType IsYetToOccur() => query
            => query.Where(announcement => 
                // Start date is in the future; and,
                (announcement.Start.HasValue && announcement.Start > DateTime.Now)
                // Is not manually archived
                && !announcement.ManuallyArchived
            );
    }

    public class AnnouncementRepository : Repository<Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<AnnouncementRepository> logger) 
            : base(dbContextFactory, logger) { }

        /// <summary>
        /// Adds an announcement hide join between a user and an announcement.
        /// </summary>
        /// <param name="announcementHide">AnnouncementHide to add</param>
        public async Task AddAnnouncementHideAsync(AnnouncementHide announcementHide)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.AnnouncementHides.AddAsync(announcementHide);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Get a paginated list of all announcements that have been manually archived, or have their end
        /// date in the past.
        /// </summary>
        /// <param name="pageNumber">Index of page to get results for, first page number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>Paginated list of expired or archived announcements, ordered most recently created first</returns>
        public async Task<PaginatedList<Announcement>> GetPaginatedExpiredOrManuallyArchivedAnnouncementsAsync(int pageNumber, int pageSize)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await PaginatedList<Announcement>.CreateAsync(
                GetBaseQuery(
                    context, 
                    new[] { AnnouncementTransforms.IsExpiredOrManuallyArchived(), AnnouncementTransforms.IncludeEditor(), AnnouncementTransforms.IncludeCreator() }
                ).OrderByDescending(a => a.Created),
                pageNumber,
                pageSize
            );
        }


        /// <summary>
        /// Retrieves all announcements that should be shown to a user, most recent announcements first.
        /// Announcements are retrieved only if they are currently active and have not been hidden by the user.
        /// </summary>
        /// <param name="userId">ID of user to fetch currently active announcements for</param>
        /// <returns>Announcements to be shown to user, ordered recent -> old</returns>
        public async Task<ICollection<Announcement>> GetActiveAnnouncementsForUserAsync(long userId)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, new[] { 
                    AnnouncementTransforms.IsNotHiddenByUser(userId), 
                    AnnouncementTransforms.IsActive() 
                })
                .OrderByDescending(a => a.Created)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves paginated announcements that are yet to become active, ordered by soonest to occur first,
        /// and furthest away last.
        /// </summary>
        /// <param name="pageNumber">Index of page to get results for, first page number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>Paginated list of upcoming announcements, soonest first</returns>
        public async Task<PaginatedList<Announcement>> GetPaginatedUpcomingAnnouncementsAsync(int pageNumber, int pageSize)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await PaginatedList<Announcement>.CreateAsync(
                GetBaseQuery(
                    context, 
                    new[] { AnnouncementTransforms.IsYetToOccur(), AnnouncementTransforms.IncludeEditor(), AnnouncementTransforms.IncludeCreator() }
                ).OrderBy(a => a.Start),
                pageNumber,
                pageSize
            );
        }

        /// <summary>
        /// Retrieves paginated announcements that are currently active, most recently created announcements first.
        /// </summary>
        /// <param name="pageNumber">Index of page to get results for, first page number=1</param>
        /// <param name="pageSize">Number of results to return per page</param>
        /// <returns>Announcements that are currently active, ordered recent -> old</returns>
        public async Task<PaginatedList<Announcement>> GetPaginatedActiveAnnouncementsAsync(int pageNumber, int pageSize)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await PaginatedList<Announcement>.CreateAsync(
                GetBaseQuery(
                    context, 
                    new[] { AnnouncementTransforms.IsActive(), AnnouncementTransforms.IncludeEditor(), AnnouncementTransforms.IncludeCreator() }
                ).OrderByDescending(a => a.Created),
                pageNumber,
                pageSize
            );
        }
    }
}