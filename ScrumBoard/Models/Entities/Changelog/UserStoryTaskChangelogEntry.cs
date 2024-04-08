using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Utils;
using ScrumBoard.Models.Messages;
using ScrumBoard.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Changelog
{    
    public class UserStoryTaskChangelogEntry : ChangelogEntry
    {   
        public long UserStoryTaskChangedId { get; set; }

        [ForeignKey(nameof(UserStoryTaskChangedId))]        
        public UserStoryTask UserStoryTaskChanged { get; set; }

        public override Type EntityType => typeof(UserStoryTask);

        // EF Core needs an empty constructor
        public UserStoryTaskChangelogEntry(){}

        public UserStoryTaskChangelogEntry(User creator, UserStoryTask userStoryTaskChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
            UserStoryTaskChangedId = userStoryTaskChanged.Id;
        }

        private List<IMessageToken> GenerateUpdateMessage() {   
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} changed task {FieldChangedName}"),
                new DifferenceToken(FromValueObject, ToValueObject),
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (Type)
            {
                case ChangeType.Update:
                    return GenerateUpdateMessage();
                default:
                    throw new NotSupportedException();                
            }
        }
    }
}
