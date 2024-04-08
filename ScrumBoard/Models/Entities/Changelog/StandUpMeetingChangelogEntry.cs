using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog;

public class StandUpMeetingChangelogEntry : ChangelogEntry
{
    public long StandUpMeetingChangedId { get; set; }

    [ForeignKey(nameof(StandUpMeetingChangedId))]
    private StandUpMeeting StandUpMeetingChanged { get; set; }

    public override Type EntityType => typeof(StandUpMeeting);

    // EF Core needs an empty constructor
    public StandUpMeetingChangelogEntry() { }

    public StandUpMeetingChangelogEntry(User creator, StandUpMeeting standUpMeetingChanged, string fieldChanged, Change<object> change) : base(creator, fieldChanged, change) {
        StandUpMeetingChangedId = standUpMeetingChanged.Id;
    }

    private List<IMessageToken> GenerateCreateMessage() {
        return new List<IMessageToken> {
            new TextToken($"{Creator.GetFullName()} scheduled the Daily Scrum"),
            new ValueToken(StandUpMeetingChanged.Name),
        };
    }

    private List<IMessageToken> GenerateUpdateMessage() {           
        return new List<IMessageToken> {
            new TextToken($"{Creator.GetFullName()} changed the Daily Scrum {FieldChangedName}"),
            new DifferenceToken(FromValueObject, ToValueObject),
        };
    }

    private List<IMessageToken> GenerateDeleteMessage() {        
        return new List<IMessageToken> {
            new TextToken($"{Creator.GetFullName()} deleted the Daily Scrum"),
            new ValueToken(StandUpMeetingChanged.Name),
        };    
    }

    public override List<IMessageToken> GenerateMessage()
    {
        return Type switch
        {
            ChangeType.Create => GenerateCreateMessage(),
            ChangeType.Update => GenerateUpdateMessage(),
            ChangeType.Delete => GenerateDeleteMessage(),
            _ => throw new NotSupportedException()
        };
    }
}