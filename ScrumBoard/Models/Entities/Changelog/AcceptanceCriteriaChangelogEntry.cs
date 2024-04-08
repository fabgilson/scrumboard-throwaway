using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog;

public class AcceptanceCriteriaChangelogEntry : UserStoryChangelogEntry
{   

    public long AcceptanceCriteriaChangedId { get; set; }

    [ForeignKey(nameof(AcceptanceCriteriaChangedId))]        
    public AcceptanceCriteria AcceptanceCriteriaChanged { get; set; }

    public override Type EntityType => typeof(AcceptanceCriteria);

    public override bool IsReviewChange => true;

    // EF Core needs an empty constructor
    public AcceptanceCriteriaChangelogEntry(){}

    public AcceptanceCriteriaChangelogEntry(
        long creatorId, 
        AcceptanceCriteria acceptanceCriteria, 
        string fieldChanged, 
        Change<object> change, 
        Guid? editingSessionGuid
    ) : base(creatorId, acceptanceCriteria.UserStoryId, fieldChanged, change, editingSessionGuid: editingSessionGuid)
    {
        AcceptanceCriteriaChangedId = acceptanceCriteria.Id;
    }

    private List<IMessageToken> GenerateCreateMessage() {
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} added {FieldChangedName}"),
            new ValueToken(ToValueObject),
            new TextToken("to"),
            new TextToken($"AC{AcceptanceCriteriaChanged.InStoryId}", FontStyle.Bold),
        };
    }

    private List<IMessageToken> GenerateUpdateMessage() {            
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} changed"),
            new TextToken($"AC{AcceptanceCriteriaChanged.InStoryId}", FontStyle.Bold),
            new TextToken(FieldChangedName),
            new DifferenceToken(FromValueObject, ToValueObject),
        };
    }

    private List<IMessageToken> GenerateDeleteMessage() {
        return new List<IMessageToken>() {
            new TextToken($"{Creator.GetFullName()} removed {FieldChangedName}"),
            new ValueToken(FromValueObject),
            new TextToken("from"),
            new TextToken($"AC{AcceptanceCriteriaChanged.InStoryId}", FontStyle.Bold),
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