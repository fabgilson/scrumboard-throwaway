
using System;

namespace ScrumBoard.Models.Entities
{
    public enum ProjectRole {
        Guest,
        Reviewer,
        Developer,
        Leader,
    }

    public static class ProjectRoleExtensions
    {
        /// <summary>
        /// Determines whether a user in a project of this role can modify any project fields.
        /// E.g. Create sprint, edit task, be assigned
        /// </summary>
        /// <returns> true if User with this role can edit the project or false if they cannot </returns>
        public static bool CanEdit(this ProjectRole role)
        {
            return role switch {
                ProjectRole.Guest => false,
                ProjectRole.Reviewer => false,
                ProjectRole.Developer => true,
                ProjectRole.Leader => true,
                _ => throw new ArgumentException($"Invalid enum value {role} for role", nameof(role)),
            };
        }
    }
     
    public interface ICloneable<T>
    {
        T Clone();
    }
    public class ProjectUserMembership: ICloneable<ProjectUserMembership>
    {
        public long ProjectId { get; set; }
        public long UserId { get; set; }
        public virtual User User { get; set; }
        public virtual Project Project { get; set; }
        public ProjectRole Role { get; set; }

        public ProjectUserMembership Clone()
        {
            return new ProjectUserMembership() { ProjectId = ProjectId, UserId = UserId, User = User, Project = Project, Role = Role };
        }

        protected bool Equals(ProjectUserMembership other)
        {
            return ProjectId == other.ProjectId && UserId == other.UserId && Role == other.Role;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProjectUserMembership) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProjectId, UserId, (int) Role);
        }
    }
}
