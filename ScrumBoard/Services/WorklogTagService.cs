using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Services;

public interface IWorklogTagService
{
    Task<IEnumerable<WorklogTag>> GetAllAsync();
}

public class WorklogTagService : IWorklogTagService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public WorklogTagService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IEnumerable<WorklogTag>> GetAllAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.WorklogTags.ToListAsync();
    }
}