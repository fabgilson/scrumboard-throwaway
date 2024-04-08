using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.UsageData
{
    public abstract class BaseUsageDataEvent
    {
        [Key]
        public long Id { get; set; }

        public DateTime Occurred { get; set; }

        public long UserId { get; set; }
        
        public BaseUsageDataEvent(long userId)
        {
            Occurred = DateTime.Now;
            UserId = userId;
        }
    }
}