using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityProvider.DataAccess;
using IdentityProvider.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Services.Internal
{
    public class LensUserStore : IUserPasswordStore<User>, IUserEmailStore<User>, IQueryableUserStore<User>
    {
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

        public LensUserStore(IDbContextFactory<DatabaseContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = dbContext.Attach(user);
            if(entity.State is not EntityState.Added) return IdentityResult.Failed(
                new IdentityError {
                    Description = "User conflicts with an existing entity in DB context, are you certain it hasn't already been added?"
                });
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = dbContext.Attach(user);
            if(entity.State is EntityState.Added or EntityState.Detached) return IdentityResult.Failed(
                new IdentityError {
                Description = "User could not be attached to the DB context, are you certain it already exists in the database?"
            });
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        
        public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {        
            var userIdLong = Convert.ToInt64(userId);
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Users.FindAsync(new object[] { userIdLong }, cancellationToken);
        }

        public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Users.FirstOrDefaultAsync(
                x => x.NormalizedUserName == normalizedUserName, 
                cancellationToken
            );
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public async Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return dbContext.Users.Attach(user).Entity.PasswordHash;
        }

        public async Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            // We assume that any UC LDAP accounts do have a password
            return user.IdentitySource is IdentitySource.Ldap 
                || await GetPasswordHashAsync(user, cancellationToken) is not null;
        }

        public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Users.FirstOrDefaultAsync(
                x => x.NormalizedEmail == normalizedEmail, 
                cancellationToken
            );
        }

        public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public IQueryable<User> Users => _dbContextFactory.CreateDbContext().Users;
    }
}