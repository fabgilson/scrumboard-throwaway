
namespace ScrumBoard.Models.Entities
{    
    public enum TaskRole 
    {
        Assigned, 
        Reviewer
    }

    public class UserTaskAssociation
    {
        public long TaskId { get; set; }
        public long UserId { get; set; }
        public TaskRole Role { get; set; }
        public User User { get; set; }
        public UserStoryTask Task { get; set; }
    }
}
