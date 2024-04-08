using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Models.Entities
{
    /// <summary> Class defining a group of stories </summary>
    public abstract class StoryGroup : IId
    {
        [Key]
        public long Id { get; set; }            

        public List<UserStory> Stories { get; set; } = new List<UserStory>();      

        public abstract Project Project { get; set; } 
    }

}