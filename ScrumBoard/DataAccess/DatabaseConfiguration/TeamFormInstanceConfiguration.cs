using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class TeamFormInstanceConfiguration : IEntityTypeConfiguration<TeamFormInstance>
{
    public void Configure(EntityTypeBuilder<TeamFormInstance> builder)
    {
        builder.Navigation(x => x.Assignment).AutoInclude();
        builder.Navigation(x => x.LinkedProject).AutoInclude();
        builder.Navigation(x => x.Project).AutoInclude();
    }
}