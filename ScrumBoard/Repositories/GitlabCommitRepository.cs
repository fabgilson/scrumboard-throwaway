using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Gitlab;

namespace ScrumBoard.Repositories
{
    using TransformType = Func<IQueryable<GitlabCommit>, IQueryable<GitlabCommit>>;

    public interface IGitlabCommitRepository : IRepository<GitlabCommit>
    {
        Task<GitlabCommit> GetByIdAsync(string hash, params TransformType[] transforms);
        Task<List<GitlabCommit>> GetByWorklogAsync(WorklogEntry worklogEntry, params TransformType[] transforms);
        Task<bool> HasLinkAsync(GitlabCommit commit, User user);
        Task AddCommitsIfNeededAsync(IEnumerable<GitlabCommit> commits);
    }

    public static class CommitIncludes
    {
        /// <summary>
        /// Includes GitlabCommit.RelatedWorklogEntry
        /// </summary>
        public static readonly TransformType RelatedWorklogEntries =
            query => query.Include(gitlabCommit => gitlabCommit.RelatedWorklogEntries);
    }

    /// <summary>
    /// Repository for GitlabCommit
    /// </summary>
    public class GitlabCommitRepository : Repository<GitlabCommit>, IGitlabCommitRepository
    {
        public GitlabCommitRepository(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<GitlabCommitRepository> logger) : base(dbContextFactory, logger) { }

        /// <summary>
        /// Gets a gitlab commit by its hash, returns null if no gitlab commit with the hash exists
        /// </summary>
        /// <param name="hash">Gitlab commit hash to find</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
        /// <returns>Gitlab commit with the given hash if it exists, otherwise null</returns>
        public async Task<GitlabCommit> GetByIdAsync(string hash, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(commit => commit.Id == hash)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Finds all the commits curently linked with the given worklog entry
        /// </summary>
        /// <param name="worklogEntry">Worklog entry to find commits for</param>
        /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters, sorts</param>
        /// <returns>List of gitlab commits</returns>
        public async Task<List<GitlabCommit>> GetByWorklogAsync(WorklogEntry worklogEntry, params TransformType[] transforms)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await GetBaseQuery(context, transforms)
                .Where(commit => commit.RelatedWorklogEntries.Any(entry => entry.Id == worklogEntry.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Determines whether the given user has already linked up a worklog entry with the given commit
        /// </summary>
        /// <param name="commit">Commit to check</param>
        /// <param name="user">User to check hasn't logged commit</param>
        /// <returns>True if user has linked against commit, otherwise false</returns>
        public async Task<bool> HasLinkAsync(GitlabCommit commit, User user)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var databaseCommit = context.WorklogCommitJoins.Where(c => c.CommitId == commit.Id);
            return databaseCommit.Any(c => c.Entry.UserId == user.Id);
        }

        /// <summary>
        /// Adds a list of commits asynchronously and adds them to the database only if they're not already present
        /// </summary>
        /// <param name="commits">Commits to add if needed</param>
        public async Task AddCommitsIfNeededAsync(IEnumerable<GitlabCommit> commits)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            foreach (var commit in commits)
            {
                var databaseCommit = await context.GitlabCommits.FirstOrDefaultAsync(c => c.Id == commit.Id);
                if (databaseCommit == null)
                {
                    context.Add(new GitlabCommit()
                    {
                        Id = commit.Id,
                        WebUrl = commit.WebUrl,
                        Title = commit.Title,
                        Message = commit.Message,
                        AuthoredDate = commit.AuthoredDate,
                        AuthorEmail = commit.AuthorEmail,
                        AuthorName = commit.AuthorName,
                        RelatedWorklogEntries = commit.RelatedWorklogEntries
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}