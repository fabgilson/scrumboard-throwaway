namespace ScrumBoard.Models.Entities
{
    /// <summary>
    /// Interface for any entity that can be referenced by a single key
    /// </summary>
    public interface IId
    {
        /// <summary>
        /// Entity primary key
        /// </summary>
        public long Id { get; set; }
    }  
}
