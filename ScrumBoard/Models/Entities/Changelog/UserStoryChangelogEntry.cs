using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog;

public class UserStoryChangelogEntry : ChangelogEntry
{   

    public long UserStoryChangedId { get; set; }

    [ForeignKey(nameof(UserStoryChangedId))]        
    public UserStory UserStoryChanged { get; set; }

    public override Type EntityType => typeof(UserStory);

    public override Type FieldType => IsAcceptanceCriteriaChange ? typeof(string) : base.FieldType;

    private bool IsAcceptanceCriteriaChange => FieldChanged.StartsWith("AC");

    /// <summary>
    /// Whether this change is associated with a sprint review
    /// </summary>
    [NotMapped]
    public virtual bool IsReviewChange => FieldChanged == nameof(UserStory.ReviewComments);

    // EF Core needs an empty constructor
    public UserStoryChangelogEntry(){}

    public UserStoryChangelogEntry(
        long creatorId, 
        long userStoryChangedId, 
        string fieldChanged, 
        Change<object> change, 
        Guid? editingSessionGuid=null
    ) : base(creatorId, fieldChanged, change, editingSessionGuid)
    {
        UserStoryChangedId = userStoryChangedId;
    }

    private List<IMessageToken> GenerateCreateMessage() {
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} added"),
            new TextToken(FieldChangedName, IsAcceptanceCriteriaChange ? FontStyle.Bold : FontStyle.Normal),
            new ValueToken(ToValueObject),
            new TextToken("to the story"),
        };
    }

    private List<IMessageToken> GenerateUpdateMessage() {            
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} changed the story"),
            new TextToken(FieldChangedName, IsAcceptanceCriteriaChange ? FontStyle.Bold : FontStyle.Normal),
            new DifferenceToken(FromValueObject, ToValueObject),
        };
    }

    private List<IMessageToken> GenerateDeleteMessage() {
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} removed"),
            new TextToken(FieldChangedName, IsAcceptanceCriteriaChange ? FontStyle.Bold : FontStyle.Normal),
            new ValueToken(FromValueObject),
            new TextToken("from the story"),
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
                throw new InvalidOperationException();             
        }
    }
        
}