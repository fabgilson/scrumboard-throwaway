using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class OverheadEntrySessionChangelogEntry : OverheadEntryChangelogEntry
    {   

        [ForeignKey(nameof(OldSessionId))]
        public OverheadSession OldSession { get; set; }
        
        public long OldSessionId { get; set; }
        
        [ForeignKey(nameof(NewSessionId))]
        public OverheadSession NewSession { get; set; }
        
        public long NewSessionId { get; set; }

        public override Type EntityType => typeof(OverheadEntry);

        public override string FieldChangedName => throw new NotSupportedException();

        // EF Core needs an empty constructor
        public OverheadEntrySessionChangelogEntry(){}

        public OverheadEntrySessionChangelogEntry(User creator, OverheadEntry overheadEntryChanged, OverheadSession oldSession, OverheadSession newSession) : 
            base(creator, overheadEntryChanged, null, Change<object>.Update(null, null))
        {
            OldSessionId = oldSession.Id;
            NewSessionId = newSession.Id;
        }

        private List<IMessageToken> GenerateUpdateMessage() {            
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} changed the formal event session"),
                new ValueToken(OldSession),
                new ArrowToken(),
                new ValueToken(NewSession),
            };
        }
        
        public override List<IMessageToken> GenerateMessage() {
            switch (Type)
            {
                case ChangeType.Update:
                    return GenerateUpdateMessage();    
                default:
                    throw new InvalidOperationException();             
            }
        }
        
    }
}
