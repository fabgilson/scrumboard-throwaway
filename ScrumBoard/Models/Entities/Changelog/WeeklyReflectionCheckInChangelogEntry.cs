using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog;

public class WeeklyReflectionCheckInChangelogEntry : ChangelogEntry
{
    public long WeeklyReflectionCheckInId { get; set; }
    [ForeignKey(nameof(WeeklyReflectionCheckInId))]
    public WeeklyReflectionCheckIn WeeklyReflectionCheckIn { get; set; }
    
    public override Type EntityType => typeof(WeeklyReflectionCheckIn);
    
    // EF Core needs an empty constructor
    public WeeklyReflectionCheckInChangelogEntry(){}
    
    public WeeklyReflectionCheckInChangelogEntry(
        WeeklyReflectionCheckIn weeklyReflectionCheckIn, 
        long creatorId, 
        string fieldChanged, 
        Change<object> change, 
        Guid? editingSessionGuid = null
    ) : base(creatorId, fieldChanged, change, editingSessionGuid)
    {
        if (weeklyReflectionCheckIn.Id == default) throw new ArgumentException("WeeklyReflectionCheckInId must a non-default value");
        WeeklyReflectionCheckInId = weeklyReflectionCheckIn.Id;
    }

    private List<IMessageToken> GenerateCreateMessage()
    {
        return [new TextToken($"{Creator.GetFullName()} started the reflection check-in")];
    }
    
    private List<IMessageToken> GenerateUpdateMessage() 
    {   
        return
        [
            new TextToken($"{Creator.GetFullName()} changed {FieldChangedName}"),
            new DifferenceToken(FromValueObject, ToValueObject)
        ];
    }
    
    public override List<IMessageToken> GenerateMessage()
    {
        return Type switch
        {
            ChangeType.Update => GenerateUpdateMessage(),
            ChangeType.Create => GenerateCreateMessage(),
            _ => throw new NotSupportedException()
        };
    }
}