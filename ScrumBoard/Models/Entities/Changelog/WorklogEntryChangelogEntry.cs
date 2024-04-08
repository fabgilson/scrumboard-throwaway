using System;
using System.Collections.Generic;
using ScrumBoard.Extensions;
using ScrumBoard.Utils;
using ScrumBoard.Models.Messages;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class WorklogEntryChangelogEntry : ChangelogEntry
    {   
        
        public long WorklogEntryChangedId { get; set; }

        [ForeignKey(nameof(WorklogEntryChangedId))]
        public WorklogEntry WorklogEntryChanged { get; set; }

        public override Type EntityType => typeof(WorklogEntry);

        // EF Core needs an empty constructor
        public WorklogEntryChangelogEntry(){}

        public WorklogEntryChangelogEntry(User creator, WorklogEntry worklogEntryChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
            WorklogEntryChangedId = worklogEntryChanged.Id;
        }
        
        public WorklogEntryChangelogEntry(long creatorId, WorklogEntry worklogEntryChanged, string fieldChanged, Change<object> change) : base(creatorId, fieldChanged, change) {
            WorklogEntryChangedId = worklogEntryChanged.Id;
        }
        
        public WorklogEntryChangelogEntry(long creatorId, long worklogEntryChangedId, string fieldChanged, Change<object> change) : base(creatorId, fieldChanged, change) {
            WorklogEntryChangedId = worklogEntryChangedId;
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} created the worklog"),
                new ValueToken(WorklogEntryChanged.Description),
            };
        }

        private List<IMessageToken> GenerateUpdateMessage() {           
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} changed the worklog {FieldChangedName}"),
                new DifferenceToken(FromValueObject, ToValueObject),
            };
        }

        private List<IMessageToken> GenerateDeleteMessage() {        
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} deleted the worklog"),
                new ValueToken(WorklogEntryChanged.Description),
            };    
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (this.Type)
            {
                case ChangeType.Create:
                    return GenerateCreateMessage();
                case ChangeType.Update:
                    return GenerateUpdateMessage();
                case ChangeType.Delete:
                    return GenerateDeleteMessage();
                default:
                    throw new NotSupportedException();               
            }
        }
    }
}
