using System;

namespace ScrumBoard.Models
{    
    public class CommitTags
    {
        public long TaskTag { get; set; }
        public string PairTag { get; set; }
        public TimeSpan TimeTag { get; set; }       
    }
}
