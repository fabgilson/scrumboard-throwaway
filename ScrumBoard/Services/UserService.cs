using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<long> userIds);
}

public class UserService : IUserService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public UserService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<long> userIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users
            .Where(x => userIds.Contains(x.Id))
            .ToListAsync();
    }
}