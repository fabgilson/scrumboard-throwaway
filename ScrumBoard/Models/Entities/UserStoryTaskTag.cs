using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities
{
    public class UserStoryTaskTag : ITag {
        [Key]
        public long Id { get; set; }
        
        public string Name { get; set; }
        
        public BadgeStyle Style { get; set; }
        
        public ICollection<UserStoryTask> Tasks { get; set; }

        protected bool Equals(UserStoryTaskTag other)
        {
            return Id == other.Id && Name == other.Name && Style == other.Style;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserStoryTaskTag) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, (int) Style);
        }
    }
}