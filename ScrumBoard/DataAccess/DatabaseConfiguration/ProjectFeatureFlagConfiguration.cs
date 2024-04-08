using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.FeatureFlags;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class ProjectFeatureFlagConfiguration : IEntityTypeConfiguration<ProjectFeatureFlag>
{
    public void Configure(EntityTypeBuilder<ProjectFeatureFlag> builder)
    {
        builder
            .Property(e => e.FeatureFlag)
            .HasConversion<int>();

        builder.HasKey(nameof(ProjectFeatureFlag.ProjectId), nameof(ProjectFeatureFlag.FeatureFlag));
    }
}