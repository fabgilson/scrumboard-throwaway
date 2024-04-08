using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog;


public class StandUpMeetingUserMembershipChangelogEntry : StandUpMeetingChangelogEntry
{     
    public long RelatedUserId { get; set; }
    
    [ForeignKey(nameof(RelatedUserId))]
    public User RelatedUser { get; set; }

    public override Type EntityType => typeof(StandUpMeeting);

    public override Type FieldType => typeof(User);

    public override string FieldChangedName => throw new NotSupportedException();

    // EF Core needs an empty constructor
    public StandUpMeetingUserMembershipChangelogEntry(){}

    public StandUpMeetingUserMembershipChangelogEntry(User creator, StandUpMeeting standUpMeetingChanged, User relatedUser, Change<object> change) 
        : base(creator, standUpMeetingChanged, null, change)
    {
        RelatedUserId = relatedUser.Id;
    }        

    private List<IMessageToken> GenerateCreateMessage() {
        return new List<IMessageToken>()
        {
            new TextToken($"{Creator.FirstName} {Creator.LastName} added a new attendee"),
            new ValueToken(RelatedUser),
            new TextToken("with role"),
            new ValueToken(ToValueObject)
        };
    }
    private List<IMessageToken> GenerateDeleteMessage()
    {
        return new List<IMessageToken>()
        {
            new TextToken($"{Creator.FirstName} {Creator.LastName} removed the attendee"),
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
            case ChangeType.Delete:
                finalMessage = GenerateDeleteMessage(); 
                break;
            default:
                throw new NotSupportedException();               
        }
        return finalMessage;
    }
}