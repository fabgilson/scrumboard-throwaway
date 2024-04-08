namespace ScrumBoard.Models.Statistics
{
    public class ProgressBarChartSegment<T>
    {
        public long Id { get; set; }

        public string Label { get; private set; }

        public T Data { get; private set; }

        public ProgressBarChartSegment(long id, string label, T data) {
            Id = id;
            Label = label;
            Data = data;
        }
    }
}