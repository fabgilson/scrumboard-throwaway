using System;

namespace ScrumBoard.Models.Shapes
{
    public interface ISprintShape {
        string Name { get; set; }
        DateOnly StartDate { get; set; }
        DateOnly EndDate { get; set; }
    }
}