using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms;

public class StoryReviewForm
{
    public StoryReviewForm(UserStory story)
    {
        ReviewComments = story.ReviewComments;
        AcceptanceCriteria = story.AcceptanceCriterias
            .Select(ac => new AcceptanceCriteriaReviewForm { Status = ac.Status, ReviewComments = ac.ReviewComments})
            .ToImmutableList();
    }

    [NotEntirelyNumbersOrSpecialCharacters]
    [MaxLength(500, ErrorMessage = "Comments cannot be longer than 500 characters")]
    public string ReviewComments { get; set; } = "";

    [ValidateComplexType]
    public IReadOnlyList<AcceptanceCriteriaReviewForm> AcceptanceCriteria { get; }
}

