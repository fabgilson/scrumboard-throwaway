using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Models.Entities;

public class IssueTag : ITag {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public BadgeStyle Style { get; set; }
    }
