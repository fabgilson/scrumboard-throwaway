using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Announcements
{
    public class AnnouncementHide
    {
        [Key]
        public long Id { get; set; }

        public DateTime Created { get; set; }

        [Required]
        [ForeignKey(nameof(AnnouncementId))]
        public Announcement Announcement { get; set; }

        public long AnnouncementId { get; set; }

        [Required]
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public long UserId { get; set; }
    }
}