using System;
using System.Collections.Generic;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics;

public class TimePairedForUser : UserProportion
{
    public TimePairedForUser(User user, double pairedHours, double totalPairedHours) : base(user, pairedHours, totalPairedHours)
    {
    }
    
    public override List<IMessageToken> GenerateMessage()
    {
        return new List<IMessageToken>
        {
            new AvatarToken
            (
                User,
                MinSize + CalculateSizeModifier()
            ),
            new ValueToken(TimeSpan.FromHours(Value)),
        };
    } 
}