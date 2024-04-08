using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit.Models.Forms;

public class UserStoryTaskFormTest : IDisposable
{
    private readonly User _actingUser = new User() {
        Id = 5,
        FirstName = "Tim",
        LastName = "Tam",
    };

    private static readonly User _assignee = new User() {
        Id = 6, 
        FirstName = "John", 
        LastName = "Smith"
    };

    private static readonly User _removedAssignee = new User() {
        Id = 8, 
        FirstName = "Removed", 
        LastName = "User"
    };

    private static readonly User _reviewer = new User() {
        Id = 7, 
        FirstName = "Jimmy", 
        LastName = "Jones"
    };

    private static readonly User _removedReviewer = new User() {
        Id = 9, 
        FirstName = "Removed", 
        LastName = "Reviewer"
    };
        
    private static readonly UserStoryTaskTag _break = new() {Id = 100, Name = "Break"};
    private static readonly UserStoryTaskTag _fix = new() {Id = 101, Name = "Fix"};
    private static readonly UserStoryTaskTag _nonStory = new() {Id = 102, Name = "Non Story"};

    private static readonly string _name = "test name";
    private static readonly string _description = "test description";
    private static readonly Priority _priority = Priority.Low;
    private static readonly TimeSpan _estimate = TimeSpan.FromSeconds(40);
    private static readonly List<UserStoryTaskTag> _tags = new List<UserStoryTaskTag>() { _break };

    private static UserStoryTask _task = new() {
        Id = 13,
        Name = _name, 
        Description = _description, 
        Estimate = _estimate, 
        Priority = _priority,
        Tags = _tags.ToList(),
    };

    private UserStoryTaskForm _model = new() {
        Name = _name, 
        Description = _description, 
        Estimate = _estimate, 
        Priority = _priority, 
        Tags = _tags.ToList(),
        Assignees = new List<User> { _assignee },
        Reviewers = new List<User> { _reviewer }
    };

    private void CheckTaskValues() {
        _task.Name.Should().Be(_name);
        _task.Description.Should().Be(_description);
        _task.Priority.Should().Be(_priority);
        _task.Estimate.Should().Be(_estimate);
        _task.Tags.Should().Equal(_tags);
    }

    public UserStoryTaskFormTest() {
        _assignee.AssignTask(_task);
        _reviewer.ReviewTask(_task);
    }

    public void Dispose() {
        _task.UserAssociations.Clear();
        _task.UserAssociations.Clear();
        _removedAssignee.TaskAssociations.Clear();
        _removedReviewer.TaskAssociations.Clear();
    }

    [Fact]
    public void ApplyChanges_AllPropertiesSet_ChangesAppliedAndRecorded() {
        _task = new UserStoryTask() { Tags = new List<UserStoryTaskTag>()};
            
        IEnumerable<UserStoryTaskChangelogEntry> changes = _model.ApplyChanges(_actingUser, _task);
        changes.Should().HaveCount(7);
        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_NoPropertiesChanged_NoChangesAppliedOrRecorded() {
        IEnumerable<UserStoryTaskChangelogEntry> changes = _model.ApplyChanges(_actingUser, _task);

        changes.Should().BeEmpty();

        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_NameChanged_NameChangeAppliedAndRecorded() {
        var initialName = "random name";
        _task.Name = initialName;

        var changes = _model.ApplyChanges(_actingUser, _task);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(UserStoryTask.Name));
        change.FromValueObject.Should().Be(initialName);
        change.ToValueObject.Should().Be(_name);

        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_DescriptionChanged_DescriptionChangeAppliedAndRecorded() {
        var initialDescription = "random description";
        _task.Description = initialDescription;

        var changes = _model.ApplyChanges(_actingUser, _task);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(UserStoryTask.Description));
        change.FromValueObject.Should().Be(initialDescription);
        change.ToValueObject.Should().Be(_description);

        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_PriorityChanged_PriorityChangeAppliedAndRecorded() {
        var initialPriority = Priority.High;
        _task.Priority = initialPriority;
        var changes = _model.ApplyChanges(_actingUser, _task);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(UserStoryTask.Priority));
        change.FromValueObject.Should().Be(Priority.High);
        change.ToValueObject.Should().Be(Priority.Low);

        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_EstimateChanged_EstimateChangeAppliedAndRecorded() {
        var initialEstimate = TimeSpan.FromSeconds(1);
        _task.Estimate = initialEstimate;

        var changes = _model.ApplyChanges(_actingUser, _task);

        changes.Should().HaveCount(1);
        var change = changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Update);
        change.FieldChanged.Should().Be(nameof(UserStoryTask.Estimate));
        change.FromValueObject.Should().Be(initialEstimate);
        change.ToValueObject.Should().Be(_estimate);

        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_TagRemoved_TagsChangeAppliedAndRecorded()
    {
        var initialTag = new List<UserStoryTaskTag>() {_break, _fix};
        _task.Tags = initialTag;
        var changes = _model.ApplyChanges(_actingUser, _task);
        
        changes.Should().HaveCount(1);
            
        var change = changes.OfType<UserStoryTaskTagChangelogEntry>().Single();
        
        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Delete);
        change.UserStoryTaskTagChangedId.Should().Be(_fix.Id);
        
        CheckTaskValues();
    }
        
    [Fact]
    public void ApplyChanges_TagAdded_TagsChangeAppliedAndRecorded()
    {
        var initialTag = new List<UserStoryTaskTag>() { };
        _task.Tags = initialTag;
        var changes = _model.ApplyChanges(_actingUser, _task);
        
        changes.Should().HaveCount(1);
            
        var change = changes.OfType<UserStoryTaskTagChangelogEntry>().Single();
        
        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);
        change.Type.Should().Be(ChangeType.Create);
        change.UserStoryTaskTagChangedId.Should().Be(_break.Id);
        
        CheckTaskValues();
    }

    [Fact]
    public void ApplyChanges_AssigneeAdded_AssigneeAddAppliedAndRecorded() {
        _task.UserAssociations.Remove(_task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Assigned) && a.User.Equals(_assignee)).First());

        var changes = _model.ApplyChanges(_actingUser, _task);

        _task.GetAssignedUsers().Should().Contain(_assignee);

        changes.Should().HaveCount(1);
        UserTaskAssociationChangelogEntry change = (UserTaskAssociationChangelogEntry) changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);

        change.Type.Should().Be(ChangeType.Create);
        change.FieldChanged.Should().Be("Assignee");
        change.UserChangedId.Should().Be(_assignee.Id);
    }

    [Fact]
    public void ApplyChanges_AssigneeRemoved_AssigneeAddAppliedAndRecorded() {    
        var initialAssociation = _removedAssignee.AssignTask(_task);
        _task.UserAssociations.Should().Contain(initialAssociation);

        var changes = _model.ApplyChanges(_actingUser, _task);

        _task.UserAssociations.Should().NotContain(initialAssociation);

        changes.Should().HaveCount(1);
        UserTaskAssociationChangelogEntry change = (UserTaskAssociationChangelogEntry) changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);

        change.Type.Should().Be(ChangeType.Delete);
        change.FieldChanged.Should().Be("Assignee");
        change.UserChangedId.Should().Be(_removedAssignee.Id);
    }

    [Fact]
    public void ApplyChanges_ReviewerAdded_ReviewerAddAppliedAndRecorded() {
        _task.UserAssociations.Remove(_task.UserAssociations.Where(a => a.Role.Equals(TaskRole.Reviewer) && a.User.Equals(_reviewer)).First());

        var changes = _model.ApplyChanges(_actingUser, _task);

        _task.GetReviewingUsers().Should().Contain(_reviewer);

        changes.Should().HaveCount(1);
        UserTaskAssociationChangelogEntry change = (UserTaskAssociationChangelogEntry) changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);

        change.Type.Should().Be(ChangeType.Create);
        change.FieldChanged.Should().Be("Reviewer");
        change.UserChangedId.Should().Be(_reviewer.Id);
    }

    [Fact]
    public void ApplyChanges_ReviewerRemoved_ReviewerAddAppliedAndRecorded() {
        _removedReviewer.ReviewTask(_task);
        _task.GetReviewingUsers().Should().Contain(_removedReviewer);

        var changes = _model.ApplyChanges(_actingUser, _task);

        _task.GetReviewingUsers().Should().NotContain(_removedReviewer);

        changes.Should().HaveCount(1);
        UserTaskAssociationChangelogEntry change = (UserTaskAssociationChangelogEntry) changes.First();

        change.CreatorId.Should().Be(_actingUser.Id);
        change.UserStoryTaskChangedId.Should().Be(_task.Id);

        change.Type.Should().Be(ChangeType.Delete);
        change.FieldChanged.Should().Be("Reviewer");
        change.UserChangedId.Should().Be(_removedReviewer.Id);
    }
}