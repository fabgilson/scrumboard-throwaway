using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ScrumBoard.Models.Shapes;

namespace ScrumBoard.Models.Entities
{
    public class AcceptanceCriteria : IId, IAcceptanceCriteriaReviewShape
    {
        [Key]
        public long Id { get; set; }
        
        public long InStoryId { get; set; }
        
        public long UserStoryId { get; set; }

        [Required]
        [ForeignKey(nameof(UserStoryId))]
        [JsonIgnore]
        public UserStory UserStory { get; set; }
        
        [Required]
        public string Content { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public AcceptanceCriteriaStatus? Status { get; set; }

        /// <summary>
        /// Comments on this specific acceptance criteria, will not be null if Passes is false
        /// </summary>
        [Display(Name = "Review Comments")]
        public string ReviewComments { get; set; }
    }

    public enum AcceptanceCriteriaStatus
    {
        Fail,
        Pass,
    }
}

