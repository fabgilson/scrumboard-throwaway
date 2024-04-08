using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Services;

/// <summary>
/// A service that allows for managing of some site-wide per-user flags. For example, when loading the upcoming stand-up
/// page for the first time with check-ins enabled, users will be prompted with a short tutorial of what the page entails.
/// To avoid forcing users to see this tutorial multiple times, we can set a flag for that user indicating that they have
/// completed that tutorial and so do not need to see it again.
/// </summary>
public interface IUserFlagService
{
    /// <summary>
    /// Determine whether or not if a flag with given type has been set (and is true) for some user.
    /// </summary>
    /// <param name="userId">Id of user for whom to check the existence of a (positive) flag</param>
    /// <param name="flagType">Type of flag being checked</param>
    /// <returns>True if a flag of the given type exists in the database AND that flag's value is TRUE, false otherwise</returns>
    public Task<bool> IsFlagSetForUserAsync(long userId, SinglePerUserFlagType flagType);

    /// <summary>
    /// Determine whether a flag exists for a user which has been updated (i.e. edited since its initial value)
    /// </summary>
    /// <param name="userId">Id of user for whom to check the existence of an updated flag</param>
    /// <param name="flagType">Type of flag being checked</param>
    /// <returns></returns>
    public Task<bool> HasFlagBeenUpdated(long userId, SinglePerUserFlagType flagType);

    /// <summary>
    /// Sets the flag value for a given user and a given flag type. If no flag of the corresponding type exists for the
    /// user in the database then a new one is created with the given value. If a flag of the corresponding type exists
    /// for the user in the database, then its value is set to the value specified.
    /// </summary>
    /// <param name="userId">Id of user for whom a flag is being created</param>
    /// <param name="flagType">Type of the flag being created for user</param>
    /// <param name="isSet">Value of the flag (whether or not it is 'set'), defaults to true</param>
    /// <returns></returns>
    public Task SetFlagForUserAsync(long userId, SinglePerUserFlagType flagType, bool isSet = true);
}

public class UserFlagService : IUserFlagService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public UserFlagService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsFlagSetForUserAsync(long userId, SinglePerUserFlagType flagType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.SinglePerUserFlags.AnyAsync(f => f.UserId == userId && f.FlagType == flagType && f.IsSet);
    }

    public async Task<bool> HasFlagBeenUpdated(long userId, SinglePerUserFlagType flagType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.SinglePerUserFlags.AnyAsync(f => f.UserId == userId && f.FlagType == flagType && f.LastUpdated != null);
    }

    public async Task SetFlagForUserAsync(long userId, SinglePerUserFlagType flagType, bool isSet = true)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var existingFlag = await context.SinglePerUserFlags.FirstOrDefaultAsync(f => f.UserId == userId && f.FlagType == flagType);
        if (existingFlag is not null)
        {
            existingFlag.IsSet = isSet;
            existingFlag.LastUpdated = DateTime.Now;
        }
        else
        {
            await context.SinglePerUserFlags.AddAsync(
                new SinglePerUserFlag { UserId = userId, FlagType = flagType, Created = DateTime.Now, IsSet = isSet }
            );
        }
        await context.SaveChangesAsync();
    }
}