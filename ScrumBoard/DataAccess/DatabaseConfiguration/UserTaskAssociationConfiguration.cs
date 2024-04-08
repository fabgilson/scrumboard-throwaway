
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.DataAccess.DatabaseConfiguration
{
    public class UserTaskAssociationConfiguration : IEntityTypeConfiguration<UserTaskAssociation>
    {
        public void Configure(EntityTypeBuilder<UserTaskAssociation> builder)
        {
            builder
                .HasOne(x => x.User)
                .WithMany(x => x.TaskAssociations)
                .HasForeignKey(x => x.UserId);
            
            builder
                .HasOne(x => x.Task)
                .WithMany(x => x.UserAssociations)
                .HasForeignKey(x => x.TaskId);

            builder
                .HasKey(x => new {x.UserId, x.TaskId});
        }
    }
}