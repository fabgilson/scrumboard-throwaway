
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class MultichoiceQuestionFormTemplateBlockConfiguration : IEntityTypeConfiguration<MultiChoiceQuestion>
    {
        public void Configure(EntityTypeBuilder<MultiChoiceQuestion> builder)
        {
            builder
                .Navigation(e => e.Options)
                .AutoInclude();
        }
    }
}