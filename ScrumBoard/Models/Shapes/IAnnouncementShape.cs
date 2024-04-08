using System;

namespace ScrumBoard.Models.Shapes
{
    public interface IAnnouncementShape
    {
        string Content { get; set; }
        DateTime? Start { get; set; }
        DateTime? End { get; set; }
        bool CanBeHidden { get; set; }
        bool ManuallyArchived { get; set; }
    }
}