using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class MultiChoiceAnswerConfiguration : IEntityTypeConfiguration<MultiChoiceAnswer>
{
    public void Configure(EntityTypeBuilder<MultiChoiceAnswer> builder)
    {
        builder.Navigation(x => x.SelectedOptions)
            .AutoInclude();
    }
}