using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Shapes
{
    public interface IUserStoryShape {
        string Name { get; set; }
        string Description { get; set; }
        int Estimate { get; set; }
        Priority Priority { get; set; }
    }
}