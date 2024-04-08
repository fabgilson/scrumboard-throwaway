using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.Services;

public interface IChangelogService
{
    /// <summary>
    /// Saves given changelogs in the database. If changelogs have a value set for <see cref="ChangelogEntry.EditingSessionGuid"/>
    /// then this method checks whether there are existing changelogs that should be updated before creating new changelogs.
    ///
    /// The provided selector function is used to find existing changelogs for incoming values, and will be different
    /// for each type of changelog. Typically this function will filter based on the foreign keys used on a changelog.
    /// </summary>
    /// <param name="changelogEntries">Incoming changelog entries to save.</param>
    /// <param name="existingChangelogSelector">
    /// Function to find any existing changelog entries by foreign key values. Note: the save method already compares
    /// EditingSessionGuid, FieldChanged, and change Type, so this function should only filter by foreign key values.
    /// </param>
    /// <typeparam name="TChangelog">Type of changelogs being saved</typeparam>
    Task SaveChangelogsAsync<TChangelog>(
        IEnumerable<TChangelog> changelogEntries, 
        Func<IQueryable<TChangelog>, IQueryable<TChangelog>> existingChangelogSelector
    ) where TChangelog : ChangelogEntry;
}

public class ChangelogService : IChangelogService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public ChangelogService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SaveChangelogsAsync<TChangelog>(
        IEnumerable<TChangelog> changelogEntries, 
        Func<IQueryable<TChangelog>, IQueryable<TChangelog>> existingChangelogSelector
    ) where TChangelog : ChangelogEntry
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // For each incoming changelog entry, if the editingSessionGuid is specified, check if there is an
        // existing changelog to be replaced, rather than a new changelog entry to be created.
        foreach (var changelog in changelogEntries)
        {
            var existingChangelogInEditingSession = await existingChangelogSelector(context.Set<TChangelog>())
                .Where(x => changelog.EditingSessionGuid != null)
                .Where(x => x.EditingSessionGuid == changelog.EditingSessionGuid)
                .Where(x => x.FieldChanged == changelog.FieldChanged)
                .Where(x => x.Type == changelog.Type)
                .FirstOrDefaultAsync();
            
            if (existingChangelogInEditingSession is not null)
            {
                // If the value has been changed back to what it was at the start of this session, remove the changelog entry, otherwise update it
                if (existingChangelogInEditingSession.FromValue == changelog.ToValue)
                {
                    context.Remove(existingChangelogInEditingSession);
                }
                else
                {
                    existingChangelogInEditingSession.ToValueObject = changelog.ToValueObject;
                    context.Update(existingChangelogInEditingSession);
                }
            }
            else
            {
                await context.AddAsync(changelog);
            }
        }

        await context.SaveChangesAsync();
    }
}