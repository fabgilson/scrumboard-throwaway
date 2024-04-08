
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class ProjectUserMembershipConfiguration : IEntityTypeConfiguration<ProjectUserMembership>
    {
        public void Configure(EntityTypeBuilder<ProjectUserMembership> builder)
        {
            builder
                .HasOne(x => x.User)
                .WithMany(x => x.ProjectAssociations)
                .HasForeignKey(x => x.UserId);
            
            builder
                .HasOne(x => x.Project)
                .WithMany(x => x.MemberAssociations)
                .HasForeignKey(x => x.ProjectId);

            builder.Navigation(x => x.User).AutoInclude();

            builder
                .HasKey(x => new {x.ProjectId, x.UserId});
        }
    }
}