namespace ScrumBoard.Models
{
    public record TableColumnConfiguration
    {
        public bool Hidden { get; set; }
        public TableColumn Column { get; init; }

        public override string ToString()
        {
            return $"TableColumnConfiguration({nameof(Column)}={Column}, {nameof(Hidden)}={Hidden})";
        }
    }
}