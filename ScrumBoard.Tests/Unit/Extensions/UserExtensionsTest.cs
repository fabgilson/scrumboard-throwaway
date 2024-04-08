using System.Linq;
using FluentAssertions;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using Xunit;

namespace ScrumBoard.Tests.Unit.Extensions
{
    public class UserExtensionsTest
    {
        private User _user = new() { Id = 1, FirstName = "John", LastName = "Smith" };
        private Project _project = new() { Id = 7};
        private UserStoryTask _task = new() { Id = 101 };
    
        [Fact]
        public void CanView_UserNotAssociatedWithProject_FalseReturned()
        {
            _user.CanView(_project).Should().BeFalse();
        }

        [Fact]
        public void CanView_UserAssociatedWithProject_TrueReturned()
        {
            _user.ProjectAssociations.Add(new ProjectUserMembership() { Project = _project });

            _user.CanView(_project).Should().BeTrue();
        }

        [Fact]
        public void AssignUserToTask_UserAndTaskExists_AssociationCreated() {
            _user.AssignTask(_task);
            _user.TaskAssociations.Where(assoc => assoc.TaskId == _task.Id && assoc.Role.Equals(TaskRole.Assigned)).Should().ContainSingle();
        }

        [Fact]
        public void AssignUserToReviewTask_UserAndTaskExists_AssociationCreated() {
            _user.ReviewTask(_task);
            _user.TaskAssociations.Where(assoc => assoc.TaskId == _task.Id && assoc.Role.Equals(TaskRole.Reviewer)).Should().ContainSingle();
        }

        [Fact]
        public void RemoveUserFromTask_AssociationExists_AssociationRemoved() {
            UserTaskAssociation assignment = new UserTaskAssociation() { TaskId = _task.Id, Task = _task, UserId = _user.Id, User = _user, Role = TaskRole.Assigned };
            _user.TaskAssociations.Add(assignment);
            _user.TaskAssociations.Should().ContainSingle();
            _user.RemoveTaskAssignment(_task);
            _user.TaskAssociations.Should().BeEmpty();
        }

        [Fact]
        public void RemoveUserFromReviewingTask_AssociationExists_AssociationRemoved() {
            UserTaskAssociation assignment = new UserTaskAssociation() { TaskId = _task.Id, Task = _task, UserId = _user.Id, User = _user, Role = TaskRole.Reviewer };
            _user.TaskAssociations.Add(assignment);
            _user.TaskAssociations.Should().ContainSingle();
            _user.RemoveTaskReview(_task);
            _user.TaskAssociations.Should().BeEmpty();
        }

        [Fact]
        public void GetUserFullName_UserHasFullName_FullNameReturned() {
            _user.GetFullName().Should().Be($"{_user.FirstName} {_user.LastName}");
        }
    }
}
