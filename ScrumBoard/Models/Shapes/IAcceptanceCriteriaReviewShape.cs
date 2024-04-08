using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Shapes
{
    public interface IAcceptanceCriteriaReviewShape
    {
        public AcceptanceCriteriaStatus? Status { get; set; }
        
        public string ReviewComments { get; set; }
    }
}

