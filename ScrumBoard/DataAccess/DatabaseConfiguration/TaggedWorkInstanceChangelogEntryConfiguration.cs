using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class TaggedWorkInstanceChangelogEntryConfiguration : IEntityTypeConfiguration<TaggedWorkInstanceChangelogEntry>
{
    public void Configure(EntityTypeBuilder<TaggedWorkInstanceChangelogEntry> builder)
    {
        builder
            .HasOne(x => x.TaggedWorkInstance)
            .WithMany()
            .HasForeignKey(x => x.TaggedWorkInstanceId) 
            .IsRequired(false) 
            .OnDelete(DeleteBehavior.SetNull); 
        
        builder.Navigation(x => x.TaggedWorkInstance).AutoInclude();
        builder.Navigation(x => x.WorklogTag).AutoInclude();
    }
}