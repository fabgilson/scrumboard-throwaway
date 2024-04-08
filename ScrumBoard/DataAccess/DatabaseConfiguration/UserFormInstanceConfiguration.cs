using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class UserFormInstanceConfiguration: IEntityTypeConfiguration<UserFormInstance>
{
    public void Configure(EntityTypeBuilder<UserFormInstance> builder)
    {
        builder.Navigation(x => x.Assignment).AutoInclude();
        builder.Navigation(x => x.Assignee).AutoInclude();
        builder.Navigation(x => x.Pair).AutoInclude();
    }
}