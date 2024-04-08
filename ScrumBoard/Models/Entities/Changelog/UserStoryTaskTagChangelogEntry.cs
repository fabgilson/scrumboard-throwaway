using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Utils;
using ScrumBoard.Models.Messages;
using ScrumBoard.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.Changelog
{    
    public class UserStoryTaskTagChangelogEntry : UserStoryTaskChangelogEntry
    {   
        public long UserStoryTaskTagChangedId { get; set; }

        [ForeignKey(nameof(UserStoryTaskTagChangedId))]        
        public UserStoryTaskTag UserStoryTaskTagChanged { get; set; }
        
        public override Type FieldType => throw new NotSupportedException();
        
        public override string FieldChangedName => throw new NotSupportedException();


        // EF Core needs an empty constructor
        public UserStoryTaskTagChangelogEntry(){}

        public static UserStoryTaskTagChangelogEntry Add(User creator, UserStoryTask taskChanged, UserStoryTaskTag tagAdded)
        {
            return new UserStoryTaskTagChangelogEntry(creator, taskChanged, tagAdded, Change<object>.Create(null));
        }
        
        public static UserStoryTaskTagChangelogEntry Remove(User creator, UserStoryTask taskChanged, UserStoryTaskTag tagRemoved)
        {
            return new UserStoryTaskTagChangelogEntry(creator, taskChanged, tagRemoved, Change<object>.Delete(null));
        }
        
        private UserStoryTaskTagChangelogEntry(User creator, UserStoryTask userStoryTaskChanged, UserStoryTaskTag userStoryTaskTagChanged, Change<object> change) : base(creator, userStoryTaskChanged, null, change) {
            UserStoryTaskTagChangedId = userStoryTaskTagChanged.Id;
        }
        

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} added tag"),
                new ValueToken(UserStoryTaskTagChanged),
            };
        }
        
        private List<IMessageToken> GenerateDeleteMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} removed tag"),
                new ValueToken(UserStoryTaskTagChanged),
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
