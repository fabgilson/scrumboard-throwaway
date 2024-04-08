using System;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Announcements
{
    public partial class AnnouncementManagementDisplay
    {
        [CascadingParameter(Name = "ForceAnnouncementRefresh")]
        public EventCallback ForceAnnouncementRefresh { get; set; }
        
        [Parameter]
        public Announcement Announcement { get; set; }
        
        [Parameter]
        public EventCallback ArchiveAnnouncementCallback { get; set; }

        private bool _isEditing = false;
        
        private const DurationFormatOptions _durationFormatOptions = 
            DurationFormatOptions.UseDaysAsLargestUnit 
            | DurationFormatOptions.FormatForLongString
            | DurationFormatOptions.IgnoreSecondsInOutput;

        private string TimeAgoCreatedString =>
            DurationUtils.DurationStringFrom(DateTime.Now - Announcement.Created, _durationFormatOptions);

        private string TimeAgoEditedString =>
            Announcement.LastEdited.HasValue
                ? DurationUtils.DurationStringFrom(DateTime.Now - Announcement.LastEdited.Value, _durationFormatOptions)
                : "";
    }
}

