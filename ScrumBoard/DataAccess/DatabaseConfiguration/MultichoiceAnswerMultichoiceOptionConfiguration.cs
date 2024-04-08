using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class MultichoiceAnswerMultichoiceOptionConfiguration : IEntityTypeConfiguration<MultichoiceAnswerMultichoiceOption>
{
    public void Configure(EntityTypeBuilder<MultichoiceAnswerMultichoiceOption> builder)
    {
        builder
            .HasOne(x => x.MultichoiceAnswer)
            .WithMany(x => x.SelectedOptions)
            .HasForeignKey(x => x.MultichoiceAnswerId)
            .IsRequired();
            
        builder
            .HasOne(x => x.MultichoiceOption)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.MultichoiceOptionId);

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MultichoiceAnswerId, x.MultichoiceOptionId })
            .IsUnique();
    }
}
