using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class WorklogEntryUserAssociationChangelogEntry : WorklogEntryChangelogEntry
    {
        public long? PairUserChangedId { get; set; }
        
        [ForeignKey(nameof(PairUserChangedId))]
        public User PairUserChanged { get; set; } 
        
        public override string FieldChangedName => throw new NotSupportedException();
        
        // EF Core needs an empty constructor
        public WorklogEntryUserAssociationChangelogEntry(){}

        public WorklogEntryUserAssociationChangelogEntry(User creator, WorklogEntry worklogEntryChanged, User pairUserChanged, string fieldChanged, Change<object> change) : base(creator, worklogEntryChanged, fieldChanged, change)
        {
            PairUserChangedId = pairUserChanged.Id;
        }
        
        public WorklogEntryUserAssociationChangelogEntry(long creatorId, long worklogEntryChangedId, long? pairUserChangedId, string fieldChanged, Change<object> change) 
            : base(creatorId, worklogEntryChangedId, fieldChanged, change)
        {
            PairUserChangedId = pairUserChangedId;
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} added partner"),
                new ValueToken(PairUserChanged),
            };
        }

        private List<IMessageToken> GenerateDeleteMessage() {       
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} removed partner"),
                new ValueToken(PairUserChanged),
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (this.Type)
            {
                case ChangeType.Create:
                    return GenerateCreateMessage();
                case ChangeType.Delete:
                    return GenerateDeleteMessage();  
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
