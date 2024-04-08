using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class UserStandUpMeetingCalendarLinkConfiguration : IEntityTypeConfiguration<UserStandUpCalendarLink>
{
    public void Configure(EntityTypeBuilder<UserStandUpCalendarLink> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ProjectId });
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
