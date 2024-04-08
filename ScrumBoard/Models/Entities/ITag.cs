namespace ScrumBoard.Models.Entities
{
    public interface ITag
    {
        long Id { get; set; }

        string Name { get; set; }

        BadgeStyle Style { get; set; }
    }
}