
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class FormTemplateConfiguration : IEntityTypeConfiguration<FormTemplate>
    {
        public void Configure(EntityTypeBuilder<FormTemplate> builder)
        {
            builder
                .HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}