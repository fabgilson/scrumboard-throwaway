using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class OverheadEntryChangelogEntry : ChangelogEntry
    {   

        public long OverheadEntryChangedId { get; set; }

        [ForeignKey(nameof(OverheadEntryChangedId))]        
        public OverheadEntry OverheadEntryChanged { get; set; }

        public override Type EntityType => typeof(OverheadEntry);

        // EF Core needs an empty constructor
        public OverheadEntryChangelogEntry(){}

        public OverheadEntryChangelogEntry(User creator, OverheadEntry overheadEntryChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
            OverheadEntryChangedId = overheadEntryChanged.Id;
        }

        private List<IMessageToken> GenerateUpdateMessage() {            
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} changed the formal event {FieldChangedName}"),
                new DifferenceToken(FromValueObject, ToValueObject),
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
