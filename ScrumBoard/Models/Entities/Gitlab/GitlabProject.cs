namespace ScrumBoard.Models.Gitlab
{
    public class ProjectAccess {
        public int AccessLevel { get; set; }
        public int NotificationLevel { get; set; }
    }
    public class ProjectPermissions {
        public ProjectAccess ProjectAccess { get; set; }
    }
    public class GitlabProject
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public ProjectPermissions Permissions { get; set; }
    }
}