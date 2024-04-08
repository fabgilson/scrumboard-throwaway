using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class SprintChangelogEntry : ChangelogEntry
    {   

        public long SprintChangedId { get; set; }

        [ForeignKey(nameof(SprintChangedId))]
        public Sprint SprintChanged { get; set; }

        public override Type EntityType => typeof(Sprint);

        // EF Core needs an empty constructor
        public SprintChangelogEntry(){}

        public SprintChangelogEntry(User creator, Sprint sprintChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
            this.SprintChangedId = sprintChanged.Id;
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} created the sprint"),
                new ValueToken(SprintChanged.Name),
            };
        }

        private List<IMessageToken> GenerateUpdateMessage() {         
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} changed the sprint {FieldChangedName}"),
                new DifferenceToken(FromValueObject, ToValueObject),
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (this.Type)
            {
                case ChangeType.Create:
                    return GenerateCreateMessage();
                case ChangeType.Update:
                    return GenerateUpdateMessage();
                default:
                    throw new NotSupportedException();                    
            }
        }
    }
}
