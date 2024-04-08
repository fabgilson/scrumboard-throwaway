using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Forms;

namespace ScrumBoard.Repositories;

using TransformType = Func<IQueryable<Assignment>, IQueryable<Assignment>>;

public static class AssignmentIncludes
{
    /// <summary>
    /// Includes Instances, Projects, and Users
    /// </summary>
    public static readonly TransformType InstancesProjectsAndMembers = query =>
        query.Include(a => a.Instances)
            .ThenInclude(i => i.Project)
            .ThenInclude(p => p.MemberAssociations)
            .ThenInclude(m => m.User);
}

public interface IAssignmentRepository : IRepository<Assignment>
{
    
}

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<AssignmentRepository> logger) : base(dbContextFactory, logger)
    {
        
    }
}