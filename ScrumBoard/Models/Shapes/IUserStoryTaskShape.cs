using System;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Shapes
{
    public interface IUserStoryTaskShape {
        string Name { get; set; }
        string Description { get; set; }
        TimeSpan Estimate { get; set; }
        Priority Priority { get; set; }
        Complexity Complexity { get; set; }
        Stage Stage { get; set; }
    }
}