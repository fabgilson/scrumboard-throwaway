using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{    
    public class SprintStoryAssociationChangelogEntry : SprintChangelogEntry
    {  
        public long UserStoryChangedId { get; set; }

        [Required]
        [ForeignKey(nameof(UserStoryChangedId))]
        public UserStory UserStoryChanged { get; set; }

        public override Type FieldType => throw new NotSupportedException();
        
        public override string FieldChangedName => throw new NotSupportedException();

        // EF Core needs an empty constructor
        public SprintStoryAssociationChangelogEntry(){}

        public SprintStoryAssociationChangelogEntry(User creator, Sprint sprintChanged, UserStory storyChanged, ChangeType type) {
            this.Created = DateTime.Now;
            this.CreatorId = creator.Id;
            this.SprintChangedId = sprintChanged.Id;
            this.Type = type;
            this.UserStoryChangedId = storyChanged.Id;
        }

        private List<IMessageToken> GenerateDeleteMessage() 
        {         
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} removed story"),
                new ValueToken(UserStoryChanged),
            };
        }

        private List<IMessageToken> GenerateCreateMessage() 
        {
            return new List<IMessageToken>() {
                new TextToken($"{Creator.GetFullName()} added story"),
                new ValueToken(UserStoryChanged),
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