using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities.Announcements
{
    public class Announcement : IAnnouncementShape
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        public long CreatorId { get; set; }

        public DateTime Created { get; set; }

        [Required]
        [ForeignKey(nameof(LastEditorId))]
        public User LastEditor { get; set; }

        public long LastEditorId { get; set; }

        public DateTime? LastEdited { get; set; }

        /// <summary>
        /// String content of announcement message. Markdown supported.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// When to start showing the announcement. Null values will show immediately.
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// When to stop showing the announcement. Null values will continue to show until
        /// the announcement is marked as 'Archived'.
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Whether users are able to hide the announcement. If this is set to true
        /// then a user will be able to 'close' it via the UI. If this is set to false, the 
        /// message will always be shown.
        /// </summary>
        public bool CanBeHidden { get; set; } = true;

        /// <summary>
        /// Whether the announcement has been marked as archived. Announcements marked 
        /// as archived (i.e this is set to true) will not be shown.
        /// </summary>
        public bool ManuallyArchived { get; set; }

        /// <summary>
        /// Instances of this announcement being 'hidden' by a user.
        /// </summary>
        public ICollection<AnnouncementHide> Hides { get; set; }
    }
}