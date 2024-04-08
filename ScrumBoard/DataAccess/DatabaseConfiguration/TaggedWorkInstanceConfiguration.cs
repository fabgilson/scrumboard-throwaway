using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class TaggedWorkInstanceConfiguration : IEntityTypeConfiguration<TaggedWorkInstance>
{
    public void Configure(EntityTypeBuilder<TaggedWorkInstance> builder)
    {
        builder.HasOne(x => x.WorklogTag).WithMany().HasForeignKey(x => x.WorklogTagId);
        builder.HasOne(x => x.WorklogEntry).WithMany().HasForeignKey(x => x.WorklogEntryId);
        
        builder.Navigation(workInstance => workInstance.WorklogEntry).AutoInclude();
        builder.Navigation(workInstance => workInstance.WorklogTag).AutoInclude();
    }
}