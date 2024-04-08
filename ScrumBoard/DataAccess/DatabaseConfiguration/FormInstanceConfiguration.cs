using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class FormInstanceConfiguration : IEntityTypeConfiguration<FormInstance>
{
    public void Configure(EntityTypeBuilder<FormInstance> builder)
    {
        builder.Navigation(x => x.Assignment)
            .AutoInclude();
    }
}