using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.UsageData;

namespace ScrumBoard.Services.UsageData
{
    public interface IUsageDataService
    {
        public Task AddUsageEventAsync(BaseUsageDataEvent usageEvent);
        public void AddUsageEvent(BaseUsageDataEvent usageEvent);
    }

    /// <summary>
    /// This is a transient class for saving usage-data events in the background
    /// </summary>
    public class UsageDataService : IUsageDataService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UsageDataService> _logger;

        public UsageDataService(IServiceScopeFactory serviceScopeFactory, ILogger<UsageDataService> logger) 
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new usage event and waits for it to complete
        /// </summary>
        /// <param name="usageEvent">The usage-event data to be stored persistently</param>
        public async Task AddUsageEventAsync(BaseUsageDataEvent usageEvent)
        {
            // Catch all exceptions to prevent normal application operation breaking if something goes wrong here
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var usageDataContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UsageDataDbContext>>();
                await using var context = await usageDataContextFactory.CreateDbContextAsync();
                await context.UsageDataEvents.AddAsync(usageEvent);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, $"Failed to add usage event type `{usageEvent.GetType().ToString()}` for user with id: {usageEvent.UserId}");
            }
        }

        /// <summary>
        /// A 'fire and forget' method to persist a usage event in the appropriate database.
        /// To avoid blocking the calling thread (and slowing down our application for normal use), 
        /// this method runs within its own scope to avoid issues with dbContexts and services being
        /// disposed prematurely. 
        /// </summary>
        /// <param name="usageEvent">Event to add</param>
        public void AddUsageEvent(BaseUsageDataEvent usageEvent)
        {
            Task.Run(() => AddUsageEventAsync(usageEvent));
        }
    }
}