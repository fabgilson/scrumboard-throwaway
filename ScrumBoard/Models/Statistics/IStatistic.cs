using System.Collections.Generic;
using ScrumBoard.Models.Messages;

namespace ScrumBoard.Models.Statistics
{
    public interface IStatistic : IWritable
    {
        double Value { get; }

        double Population { get; }
        
        string Description { get; } 

        bool IsSprint { get; }
    }
}