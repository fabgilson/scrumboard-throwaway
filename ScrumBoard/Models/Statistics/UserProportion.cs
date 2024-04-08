using System.Collections.Generic;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics;

public abstract class UserProportion : IStatistic
{
    protected User User { get; }
    protected double MinSize => 40;
    public double Value { get; }
    public double Population { get; }
    public string Description => User.GetFullName();
    public bool IsSprint { get; } 

    protected UserProportion(User user, double value, double population)
    {
        User = user;
        Value = value;
        Population = population;
    }
    
    protected double CalculateSizeModifier()
    {
        if (Population == 0)
        {
            return 0;
        }

        return Value / Population * 100;
    }

    public abstract List<IMessageToken> GenerateMessage();
}