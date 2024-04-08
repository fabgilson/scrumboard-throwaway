using System;
using System.Collections.Generic;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics;

public class WorkEfficiency : IStatistic
{
    public WorkEfficiency(double value, double population, bool isSprint)
    {
        Value = value;
        Population = population;
        IsSprint = isSprint;
    }

    public double Value { get; } 
    public double Population { get; }
    public string Description => "Work Efficiency";
    public bool IsSprint { get; }

    public List<IMessageToken> GenerateMessage()
    {
        string sprintOrProject = IsSprint ? "sprint" : "project";  

        return new List<IMessageToken>
        {
            new TextToken("You have spent"),
            new DivToken(new List<IMessageToken>
            {
                new ValueToken(Math.Round(Value, 1), TokenSize.VeryLarge),
            }),
            new DivToken(new List<IMessageToken>
            {
                new TextToken("out of"), 
                new ValueToken(Math.Round(Population, 1)), 
                new TextToken($"hours on story work this {sprintOrProject}")
            })
        };
    }
}