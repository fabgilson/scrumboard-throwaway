using System.Collections.Generic;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Widgets
{
    public class SprintStageDetails {
        public static readonly Dictionary<SprintStage, string> StageDescriptions = new() {
            [SprintStage.Created]       = "Not Started",
            [SprintStage.Started]       = "In Progress",
            [SprintStage.ReadyToReview] = "Preparing For Review",
            [SprintStage.InReview]      = "Reviewing",
            [SprintStage.Reviewed]      = "Reviewed",
            [SprintStage.Closed]        = "Closed",
        };

        public static readonly Dictionary<SprintStage, BadgeStyle> StageStyles = new() {
            [SprintStage.Created]       = BadgeStyle.Light,
            [SprintStage.Started]       = BadgeStyle.Primary,
            [SprintStage.ReadyToReview] = BadgeStyle.Warning,
            [SprintStage.InReview]      = BadgeStyle.Warning,
            [SprintStage.Reviewed]      = BadgeStyle.Success,
            [SprintStage.Closed]        = BadgeStyle.Success,
        };
    }
}