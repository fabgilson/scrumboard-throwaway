using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{    
    public class ProjectUserMembershipChangelogEntry : ProjectChangelogEntry
    {     
        public long RelatedUserId { get; set; }
        
        [ForeignKey(nameof(RelatedUserId))]
        public User RelatedUser { get; set; }

        public override Type EntityType => typeof(Project);

        public override Type FieldType => typeof(ProjectRole);
        
        public override string FieldChangedName => throw new NotSupportedException();

        // EF Core needs an empty constructor
        public ProjectUserMembershipChangelogEntry(){}

        public ProjectUserMembershipChangelogEntry(User creator, Project projectChanged, User relatedUser, Change<ProjectRole> change) : base(creator, projectChanged, null, change.Cast<object>())
        {
            RelatedUserId = relatedUser.Id;
        }        

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>()
            {
                new TextToken($"{Creator.FirstName} {Creator.LastName} added a new member"),
                new ValueToken(RelatedUser),
                new TextToken("with role"),
                new ValueToken(ToValueObject)
            };
        }

        private List<IMessageToken> GenerateUpdateMessage() {            
            return new List<IMessageToken>()
            {
                new TextToken($"{Creator.FirstName} {Creator.LastName} changed the role of"),
                new ValueToken(RelatedUser),
                new TextToken("from"),
                new DifferenceToken(FromValueObject, ToValueObject),
            };
        }

        private List<IMessageToken> GenerateDeleteMessage()
        {
            return new List<IMessageToken>()
            {
                new TextToken($"{Creator.FirstName} {Creator.LastName} removed the member"),
                new ValueToken(RelatedUser),
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            List<IMessageToken> finalMessage;
            switch (Type)
            {
                case ChangeType.Create:
                    finalMessage = GenerateCreateMessage();
                    break;
                case ChangeType.Update:
                    finalMessage = GenerateUpdateMessage();
                    break;
                case ChangeType.Delete:
                    finalMessage = GenerateDeleteMessage(); 
                    break;
                default:
                    throw new NotSupportedException();               
            }
            return finalMessage;
        }
        
    }
}
