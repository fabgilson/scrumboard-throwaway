namespace ScrumBoard.Models.Entities.Relationships
{
    public class UserStoryTaskTagJoin
    {
        public UserStoryTaskTagJoin() {}

        public UserStoryTaskTagJoin(UserStoryTask task, UserStoryTaskTag tag)
        {
            TaskId = task.Id;
            TagId = tag.Id;
        }

        public long TagId { get; set; }
        public UserStoryTaskTag Tag { get; set; }

        public long TaskId { get; set; }
        public UserStoryTask Task { get; set; }
    }
}