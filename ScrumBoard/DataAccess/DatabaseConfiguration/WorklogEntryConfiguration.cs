
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class WorklogEntryConfiguration : IEntityTypeConfiguration<WorklogEntry>
    {
        public void Configure(EntityTypeBuilder<WorklogEntry> builder)
        {    
            builder
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);     

            builder
                .HasOne(x => x.Task)
                .WithMany(x => x.Worklog)
                .HasForeignKey(x => x.TaskId);  

            builder
                .HasOne(x => x.PairUser)
                .WithMany()
                .HasForeignKey(x => x.PairUserId)
                .IsRequired(false);
            
            builder
                .HasMany(e => e.TaggedWorkInstances)
                .WithOne(x => x.WorklogEntry)
                .HasForeignKey(x => x.WorklogEntryId);

            builder
                .Navigation(e => e.TaggedWorkInstances)
                .AutoInclude();

            builder
                .HasMany(e => e.LinkedCommits)
                .WithMany(e => e.RelatedWorklogEntries)
                .UsingEntity<WorklogCommitJoin>(
                    j => j
                        .HasOne(join => join.Commit)
                        .WithMany()
                        .HasForeignKey(join => join.CommitId),
                    j => j
                        .HasOne(join => join.Entry)
                        .WithMany()
                        .HasForeignKey(join => join.EntryId),
                    j => j
                        .HasKey(join => new { join.CommitId, join.EntryId })
                );
        }
    }
}