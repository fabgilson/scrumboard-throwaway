using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Forms;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit.Models.Forms
{
    public class UserStoryFormTest
    {

        private readonly User _actingUser = new User() {
            Id = 5,
            FirstName = "Tim",
            LastName = "Tam",
        };

        private static readonly string _name = "test name";
        private static readonly string _description = "test description";
        private static readonly Priority _priority = Priority.Low;
        private static readonly int _estimate = 40;

        private UserStory _story = new() {
            Name = _name,
            Description = _description,
            Estimate = _estimate,
            Priority = _priority,
            AcceptanceCriterias = new List<AcceptanceCriteria>(),
        };
        private UserStoryForm _model = new() {
            Name = _name,
            Description = _description,
            EstimateOptional = _estimate,
            Priority = _priority,
            AcceptanceCriterias = new(),
        };

        public UserStoryFormTest() {
        }

        private void CheckStoryValues() {
            _story.Name.Should().Be(_name);
            _story.Description.Should().Be(_description);
            _story.Priority.Should().Be(_priority);
            _story.Estimate.Should().Be(_estimate);
        }

        [Fact]
        public void ApplyChanges_AllPropertiesSet_ChangesAppliedAndRecorded() {
            _story = new UserStory()
            {
                AcceptanceCriterias = new List<AcceptanceCriteria>(),
            };

            var changes = _model.ApplyChanges(_actingUser, _story);
            changes.Should().HaveCount(4);
            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_NoPropertiesChanged_NoChangesAppliedOrRecorded() {
            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().BeEmpty();

            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_NameChanged_NameChangeAppliedAndRecorded() {
            var initialName = "random name";
            _story.Name = initialName;

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Update);
            change.FieldChanged.Should().Be(nameof(UserStory.Name));
            change.FromValueObject.Should().Be(initialName);
            change.ToValueObject.Should().Be(_name);

            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_DescriptionChanged_DescriptionChangeAppliedAndRecorded() {
            var initialDescription = "random description";
            _story.Description = initialDescription;

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Update);
            change.FieldChanged.Should().Be(nameof(UserStory.Description));
            change.FromValueObject.Should().Be(initialDescription);
            change.ToValueObject.Should().Be(_description);

            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_PriorityChanged_PriorityChangeAppliedAndRecorded() {
            var initialPriority = Priority.High;
            _story.Priority = initialPriority;

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Update);
            change.FieldChanged.Should().Be(nameof(UserStory.Priority));
            change.FromValueObject.Should().Be(Priority.High);
            change.ToValueObject.Should().Be(Priority.Low);

            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_EstimateChanged_EstimateChangeAppliedAndRecorded() {
            var initialEstimate = 1;
            _story.Estimate = initialEstimate;

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Update);
            change.FieldChanged.Should().Be(nameof(UserStory.Estimate));
            change.FromValueObject.Should().Be(initialEstimate);
            change.ToValueObject.Should().Be(40);

            CheckStoryValues();
        }

        [Fact]
        public void ApplyChanges_AddAcceptanceCriteria_AcceptanceCriteriaAddedAndRecorded() {
            var acceptanceCriteria = "does the thing";

            _model.AcceptanceCriterias.Add(new() {
                Content = acceptanceCriteria
            });

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Create);
            change.FieldChanged.Should().Be("AC1");
            change.FromValueObject.Should().BeNull();
            change.ToValueObject.Should().Be(acceptanceCriteria);

            CheckStoryValues();

            _story.AcceptanceCriterias.Should().BeEquivalentTo(new List<AcceptanceCriteria>() {
                new AcceptanceCriteria() { InStoryId = 1, Content = acceptanceCriteria, UserStory = _story }
            });
        }

        [Fact]
        public void ApplyChanges_RemoveAcceptanceCriteria_AcceptanceCriteriaRemovedAndRecorded() {
            var acceptanceCriteria = "to be removed";
            _story.AcceptanceCriterias.Add(new() {
                InStoryId = 4,
                Id = 4,
                UserStory = _story,
                Content = acceptanceCriteria,
            });

            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Delete);
            change.FieldChanged.Should().Be("AC4");
            change.FromValueObject.Should().Be(acceptanceCriteria);
            change.ToValueObject.Should().BeNull();

            CheckStoryValues();

            _story.AcceptanceCriterias.Should().BeEmpty();
        }

        [Fact]
        public void ApplyChanges_UpdateAcceptanceCriteria_AcceptanceCriteriaUpdatedAndRecorded() {
            var initialAcceptanceCriteria = "initial ac content";
            var newAcceptanceCriteria = "new ac content";

            var acceptanceCriteriaId = 4;
            _story.AcceptanceCriterias.Add(new() {
                InStoryId = 72,
                Id = acceptanceCriteriaId,
                UserStory = _story,
                Content = initialAcceptanceCriteria,
            });
            _model.AcceptanceCriterias.Add(new() {
                Id = acceptanceCriteriaId,
                Content = newAcceptanceCriteria,
            });


            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(1);
            var change = changes.First();

            change.CreatorId.Should().Be(_actingUser.Id);
            change.UserStoryChangedId.Should().Be(_story.Id);
            change.Type.Should().Be(ChangeType.Update);
            change.FieldChanged.Should().Be("AC72");
            change.FromValueObject.Should().Be(initialAcceptanceCriteria);
            change.ToValueObject.Should().Be(newAcceptanceCriteria);

            CheckStoryValues();

            _story.AcceptanceCriterias.Should().BeEquivalentTo(new List<AcceptanceCriteria>() {
                new AcceptanceCriteria() {
                    InStoryId = 1,
                    Id = acceptanceCriteriaId, 
                    UserStory = _story,
                    Content = newAcceptanceCriteria, 
                }
            });
        }

        [Fact]
        public void ApplyChanges_AddAndRemoveAcceptanceCriteria_AcceptanceCriteriaUpdatedAnd2ChangesRecorded() {
            var removedAcceptanceCriteria = "removed ac content";
            var newAcceptanceCriteria = "new ac content";

            _story.AcceptanceCriterias.Add(new() {
                Id = 4,
                InStoryId = 11,
                UserStory = _story,
                Content = removedAcceptanceCriteria,
            });
            _model.AcceptanceCriterias.Add(new() {
                Content = newAcceptanceCriteria,
            });


            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(2);
            
            var removeChange = changes[0];
            removeChange.CreatorId.Should().Be(_actingUser.Id);
            removeChange.UserStoryChangedId.Should().Be(_story.Id);
            removeChange.Type.Should().Be(ChangeType.Delete);
            removeChange.FieldChanged.Should().Be("AC11");
            removeChange.FromValueObject.Should().Be(removedAcceptanceCriteria);
            removeChange.ToValueObject.Should().BeNull();

            var addChange = changes[1];
            addChange.CreatorId.Should().Be(_actingUser.Id);
            addChange.UserStoryChangedId.Should().Be(_story.Id);
            addChange.Type.Should().Be(ChangeType.Create);
            addChange.FieldChanged.Should().Be("AC1");
            addChange.FromValueObject.Should().BeNull();
            addChange.ToValueObject.Should().Be(newAcceptanceCriteria);

            CheckStoryValues();

            _story.AcceptanceCriterias.Should().BeEquivalentTo(new List<AcceptanceCriteria>() {
                new AcceptanceCriteria() {
                    InStoryId = 1,
                    UserStory = _story,
                    Content = newAcceptanceCriteria, 
                }
            });
        }

        [Fact]
        public void ApplyChanges_AddMultipleACs_AcceptanceCriteriaAddedAndChangesLabelledCorrectly() {
            var acceptanceCriteria = new List<string>(){
                "first",
                "second",
                "third",
            };
            _model.AcceptanceCriterias.AddRange(acceptanceCriteria
                .Select(content => new AcceptanceCriteriaForm() { Content = content })
            );


            var changes = _model.ApplyChanges(_actingUser, _story);

            changes.Should().HaveCount(acceptanceCriteria.Count());
            
            foreach (var (content, index) in acceptanceCriteria.Select((content, index) => (content, index))) {
                var change = changes[index];
                change.CreatorId.Should().Be(_actingUser.Id);
                change.UserStoryChangedId.Should().Be(_story.Id);
                change.Type.Should().Be(ChangeType.Create);
                change.FieldChanged.Should().Be($"AC{index + 1}");
                change.FromValueObject.Should().BeNull();
                change.ToValueObject.Should().Be(content);
            }

            CheckStoryValues();

            var inStoryId = 1;
            _story.AcceptanceCriterias.Should().BeEquivalentTo(acceptanceCriteria
                .Select(content => new AcceptanceCriteria() { InStoryId = inStoryId++, UserStory = _story, Content = content})
                .ToList()
            );
        }
    }
}
