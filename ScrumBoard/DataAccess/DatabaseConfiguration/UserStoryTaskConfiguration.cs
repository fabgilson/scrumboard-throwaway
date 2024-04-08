
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class UserStoryTaskConfiguration : IEntityTypeConfiguration<UserStoryTask>
    {
        public void Configure(EntityTypeBuilder<UserStoryTask> builder)
        {
            builder
                .Navigation(e => e.Tags)
                .AutoInclude();

            builder
                .HasMany(e => e.Tags)
                .WithMany(e => e.Tasks)
                .UsingEntity<UserStoryTaskTagJoin>(
                    j => j
                        .HasOne(join => join.Tag)
                        .WithMany()
                        .HasForeignKey(join => join.TagId),
                    j => j
                        .HasOne(join => join.Task)
                        .WithMany()
                        .HasForeignKey(join => join.TaskId),
                    j => j
                        .HasKey(join => new { join.TagId, join.TaskId })
                );
        }
    }
}