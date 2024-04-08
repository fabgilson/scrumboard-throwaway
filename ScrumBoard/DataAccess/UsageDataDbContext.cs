using Microsoft.EntityFrameworkCore;
using ScrumBoard.Models.Entities.UsageData;

namespace ScrumBoard.DataAccess
{
    public class UsageDataDbContext : DbContext
    {
        public UsageDataDbContext(DbContextOptions<UsageDataDbContext> options) : base(options) { }
        public DbSet<BaseUsageDataEvent> UsageDataEvents { get; set; } = null!;
        public DbSet<AuthenticationUsageEvent> AuthenticationUsageEvents { get; set; } = null!;
        public DbSet<ViewLoadedUsageEvent> ViewLoadedUsageEvents { get; set; } = null!;
        public DbSet<ProjectViewLoadedUsageEvent> ProjectViewLoadedUsageEvents { get; set; } = null!;
        public DbSet<StudentGuideViewLoadedUsageEvent> StudentGuideViewLoadedUsageEvents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthenticationUsageEvent>()
                .Property(e => e.AuthenticationEventType)
                .HasConversion<int>();

            modelBuilder.Entity<ViewLoadedUsageEvent>()
                .Property(e => e.LoadedViewUsageEventType)
                .HasConversion<int>();

            modelBuilder.Entity<ProjectViewLoadedUsageEvent>()
                .Property(e => e.LoadedViewUsageEventType)
                .HasConversion<int>();

            modelBuilder.Entity<StudentGuideViewLoadedUsageEvent>()
                .Property(e => e.LoadedViewUsageEventType)
                .HasConversion<int>();
        }
    }
}