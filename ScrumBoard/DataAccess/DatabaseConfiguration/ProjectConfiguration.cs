
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.Navigation(x => x.MemberAssociations).AutoInclude();
            builder.OwnsOne(x => x.GitlabCredentials, xg => 
            {
                xg.HasIndex(g => new { g.Id, g.GitlabURL }).IsUnique();
            });
        }
    }
}