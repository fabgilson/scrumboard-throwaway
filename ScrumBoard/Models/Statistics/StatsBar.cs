using System.Collections.Generic;

namespace ScrumBoard.Models.Statistics
{
    public interface IStatsBar
    {
        List<ProgressBarChartSegment<double>> Data { get; set; }
        double Total { get; set; }
    }
    
    public class StatsBar : IStatsBar
    {
        public List<ProgressBarChartSegment<double>> Data { get; set; }
        public double Total { get; set; }

        public StatsBar(List<ProgressBarChartSegment<double>> data, double total)
        {
            Data = data;
            Total = total;
        }
    }
}