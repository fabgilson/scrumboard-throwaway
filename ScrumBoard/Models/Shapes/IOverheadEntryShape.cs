using System;

namespace ScrumBoard.Models.Shapes
{
    public interface IOverheadEntryShape
    {
        string Description { get; set; }
        TimeSpan Duration { get; set; }
        DateTime Occurred { get; set; }
    }
}
