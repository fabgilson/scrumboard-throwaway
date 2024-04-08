using System.Collections.Generic;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Messages;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Statistics;

public class TasksReviewedForUser : UserProportion
{

    public TasksReviewedForUser(User user, double value, double population) : base (user, value, population)
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
            new ValueToken(Value),
            new TextToken("Tasks")
        };
    }
}