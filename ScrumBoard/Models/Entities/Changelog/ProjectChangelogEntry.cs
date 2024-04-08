using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class ProjectChangelogEntry : ChangelogEntry
    {   
        public long ProjectChangedId { get; set; }
        
        [ForeignKey(nameof(ProjectChangedId))]        
        public Project ProjectChanged { get; set; }  

        // EF Core needs an empty constructor
        public ProjectChangelogEntry(){}

        public ProjectChangelogEntry(User creator, Project projectChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
            this.ProjectChangedId = projectChanged.Id;
        }

        private List<IMessageToken> GenerateUpdateMessage()
        {
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} changed the project {FieldChangedName}"),
                new DifferenceToken(FromValueObject, ToValueObject),
            };
        }

        private List<IMessageToken> GenerateCreateMessage()
        {
            return new List<IMessageToken>()
            {
                new TextToken($"{Creator.GetFullName()} added GitLab credentials"),                
                new ValueToken(ToValueObject)
            };
        }

        private List<IMessageToken> GenerateDeleteMessage()
        {
            return new List<IMessageToken>()
            {
                new TextToken($"{Creator.GetFullName()} removed GitLab credentials"),
                new ValueToken(FromValueObject)              
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (Type)
            {
                case ChangeType.Update:
                    return GenerateUpdateMessage();
                case ChangeType.Create:
                    return GenerateCreateMessage();
                case ChangeType.Delete:
                    return GenerateDeleteMessage();
                default:
                    throw new NotSupportedException();                    
            }
        }

        public override Type EntityType => typeof(Project);
    }
}
