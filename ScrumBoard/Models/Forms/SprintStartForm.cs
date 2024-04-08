using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ScrumBoard.Models.Entities;
using System.Linq;
using ScrumBoard.Extensions;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms
{
    public class SprintStartForm
    {
        
        [DateInPast(ErrorMessage = "It is not past the start date")]
        public DateOnly StartDate { get; set; }
        
        [NotEquals(false, ErrorMessage = "Previous sprint must be closed")]
        public bool PreviousSprintClosed { get; set; }

        [MinLength(1, ErrorMessage = "Must have at least one story")]
        [ValidateComplexType]
        public ICollection<UserStoryStartForm> Stories { get; set; }

        public SprintStartForm(Sprint sprint, bool previousSprintClosed)
        {
            PreviousSprintClosed = previousSprintClosed;
            StartDate = sprint.StartDate;
            Stories = sprint.Stories.Select(story => new UserStoryStartForm(story)).ToList();
        }
    }
    
}