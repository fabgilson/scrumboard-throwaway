using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.DataAccess.DatabaseConfiguration;

public class StandUpMeetingAttendanceConfiguration: IEntityTypeConfiguration<StandUpMeetingAttendance>
{
    public void Configure(EntityTypeBuilder<StandUpMeetingAttendance> builder)
    {
        builder
            .HasKey(b => new { b.UserId, b.StandUpMeetingId });
        
        builder
            .HasOne(a => a.User)
            .WithMany(u => u.StandUpMeetingAttendances)
            .HasForeignKey(a => a.UserId);
        
        builder
            .HasOne(a => a.StandUpMeeting)
            .WithMany(s => s.ExpectedAttendances)
            .HasForeignKey(s => s.StandUpMeetingId);

        builder.Navigation(a => a.User)
            .AutoInclude();
        builder.Navigation(a => a.StandUpMeeting)
            .AutoInclude();
    }
}