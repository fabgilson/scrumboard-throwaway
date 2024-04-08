using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class StandUpMeetingConfiguration : IEntityTypeConfiguration<StandUpMeeting>
{
    public void Configure(EntityTypeBuilder<StandUpMeeting> builder)
    {
        builder
            .HasOne(meeting => meeting.Creator)
            .WithMany()
            .HasForeignKey(meeting => meeting.CreatorId);
        
        builder
            .Navigation(meeting => meeting.Creator)
            .AutoInclude();
        
        builder
            .HasOne(meeting => meeting.StartedBy)
            .WithMany()
            .HasForeignKey(meeting => meeting.StartedById)
            .IsRequired(false);
        
        builder
            .Navigation(meeting => meeting.StartedBy)
            .AutoInclude();
        
        builder
            .Navigation(meeting => meeting.ExpectedAttendances)
            .AutoInclude();
        
        builder
            .Navigation(meeting => meeting.Sprint)
            .AutoInclude();
    }
}