using System;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Models.Entities
{
    public class WorklogTag : ITag {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public BadgeStyle Style { get; set; }

        protected bool Equals(WorklogTag other)
        {
            return Id == other.Id && Name == other.Name && Style == other.Style;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WorklogTag) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, (int) Style);
        }
    }
}