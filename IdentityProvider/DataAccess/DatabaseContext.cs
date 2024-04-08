using System.Reflection;
using IdentityProvider.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.DataAccess
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Entity<User>()
                .HasKey(x => x.Id);
            
            builder.Entity<User>()
                .HasIndex(x => x.UserName).IsUnique();
            
            builder.Entity<User>()
                .HasIndex(x => x.NormalizedUserName).IsUnique();
        }
    }
}