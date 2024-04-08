using System.Collections.Generic;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics;

public class TasksWorked : IStatistic
{
    public double Value { get; }
    public double Population { get; }
    public string Description => "Tasks Worked";
    public bool IsSprint { get; }

    public TasksWorked(double value, double population, bool isSprint)
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
            new TextToken("You have worked on"),
            new DivToken(new List<IMessageToken>
            {
                new ValueToken(Value, TokenSize.VeryLarge),
            }),
            new DivToken(new List<IMessageToken>
            {
                new TextToken("out of the"), 
                new ValueToken(Population), 
                new TextToken($"tasks estimated this {sprintOrProject}")
            })
        };
    }
}