using System.Collections.Generic;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics;

public class StoriesReviewed : IStatistic
{
    public StoriesReviewed(double value, double population, bool isSprint)
    {
        Value = value;
        Population = population;
        IsSprint = isSprint;
    }

    public List<IMessageToken> GenerateMessage()
    {
        string sprintOrProject = IsSprint ? "sprint" : "project";

        return new List<IMessageToken>
        {
            new TextToken("The tasks you have reviewed belong to"),
            new DivToken(new List<IMessageToken>
            {
                new ValueToken(Value, TokenSize.VeryLarge),
            }),
            new TextToken($"committed stories reviewed so far in the {sprintOrProject}")
        };
    }

    public double Value { get; }
    public double Population { get; }
    public string Description => "Stories Reviewed";
    public bool IsSprint { get; }
}