using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<Backlog>, IQueryable<Backlog>>;
    public interface IBacklogRepository : IRepository<Backlog>
    {
        Task UpdateBacklogAndStories(Backlog backlog, List<UserStory> stories);
    }

    /// <summary>
    /// Repository for Backlog
    /// </summary>
    public class BacklogRepository : Repository<Backlog>, IBacklogRepository
    {
        public BacklogRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<BacklogRepository> logger) : base(dbContextFactory, logger)
        {
        }        

        /// <summary>
        /// Updates that backlog and any stories associated with it
        /// </summary>
        /// <param name="backlog">Backlog to update, this should have a Stories property value of null</param>
        /// <param name="stories">Stories to replace current backlog stories</param>
        /// <exception cref="InvalidOperationException">If backlog.Stories is not null</exception>
        public async Task UpdateBacklogAndStories(Backlog backlog, List<UserStory> stories)
        {
            if (backlog.Stories != null)
                throw new InvalidOperationException("backlog.Stories should be null");
            
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            context.Update(backlog);
            await UpdateStories(backlog, stories);
                        
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates just the stories of a backlog
        /// </summary>
        /// <param name="backlog">Backlog to update</param>
        /// <param name="stories">Stories to replace current backlog stories</param>
        private async Task UpdateStories(Backlog backlog, List<UserStory> stories)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();                   
            var updatedStoried = stories.Select(story => story.CloneForPersisting()).ToList();         
            foreach (var story in updatedStoried) {
                story.StoryGroupId = backlog.Id;
            }          
            context.UpdateRange(updatedStoried);      
            await context.SaveChangesAsync();
        }
    }
}