using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{    
    public class UserTaskAssociationChangelogEntry : UserStoryTaskChangelogEntry
    {
        public long UserChangedId { get; set; }

        [ForeignKey(nameof(UserChangedId))]
        public User UserChanged { get; set; }  

        public override Type FieldType => typeof(TaskRole);       
        
        public override string FieldChangedName => throw new NotSupportedException();

        // EF Core needs an empty constructor
        public UserTaskAssociationChangelogEntry(){}

        public UserTaskAssociationChangelogEntry(User creator, UserStoryTask userStoryTaskChanged, User userChanged, string fieldChanged, Change<TaskRole> change) : base(creator, userStoryTaskChanged, fieldChanged, change.Cast<object>())
        {
            UserChangedId = userChanged.Id;
        } 

        private List<IMessageToken> GenerateDeleteMessage() {        
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} removed {FieldChanged}"),
                new ValueToken(UserChanged),
            };
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} added {FieldChanged}"),
                new ValueToken(UserChanged),
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
