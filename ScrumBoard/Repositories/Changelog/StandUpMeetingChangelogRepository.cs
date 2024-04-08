using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Utils;

namespace ScrumBoard.Repositories.Changelog;

using TransformType = Func<IQueryable<StandUpMeetingChangelogEntry>, IQueryable<StandUpMeetingChangelogEntry>>;
public interface IStandUpMeetingChangelogRepository : IRepository<StandUpMeetingChangelogEntry>
{
    Task<List<StandUpMeetingChangelogEntry>> GetByStandUpMeetingAsync(StandUpMeeting standUpMeeting, params TransformType[] transforms);
}

public static class StandUpMeetingChangelogEntryIncludes
{
    /// <summary>
    /// Includes StandUpMeetingChangelogEntry.Creator
    /// </summary>
    public static readonly TransformType Creator = query => query.Include(changelog => changelog.Creator);
}

public class StandUpMeetingChangelogRepository : Repository<StandUpMeetingChangelogEntry>, IStandUpMeetingChangelogRepository
{
    public StandUpMeetingChangelogRepository(IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<StandUpMeetingChangelogRepository> logger) : base(dbContextFactory, logger) { }
    
    /// <summary>
    /// Finds all the changelog entries for the given stand-up meeting ordered by most recently occurred first
    /// </summary>
    /// <param name="standUpMeeting">Stand-up meeting to find changes for</param>
    /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
    /// <returns>List of changes for the given stand-up meeting</returns>
    public async Task<List<StandUpMeetingChangelogEntry>> GetByStandUpMeetingAsync(StandUpMeeting standUpMeeting, params TransformType[] transforms)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var changelogs = await GetBaseQuery(context, transforms)
            .Where(entry => entry.StandUpMeetingChangedId == standUpMeeting.Id)
            .OrderByDescending(entry => entry.Created)
            .ToListAsync();      
        changelogs.Insert(0, new StandUpMeetingChangelogEntry(standUpMeeting.Creator, standUpMeeting, "", Change<object>.Create(standUpMeeting)));
        return changelogs;
    }
}