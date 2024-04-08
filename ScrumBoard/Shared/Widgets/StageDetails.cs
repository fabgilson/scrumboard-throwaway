using System.Collections.Generic;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Widgets
{
    public class StageDetails {
        public static readonly Dictionary<Stage, string> StageDescriptions = new() {
            [Stage.Todo]        = "To Do",
            [Stage.InProgress]  = "In Progress",
            [Stage.Done]        = "Done",
            [Stage.UnderReview] = "Under Review",
            [Stage.Deferred]    = "Deferred",
        };

        public static readonly Dictionary<Stage, BadgeStyle> StageStyles = new() {
            [Stage.Todo]        = BadgeStyle.Light,
            [Stage.InProgress]  = BadgeStyle.Primary,
            [Stage.Done]        = BadgeStyle.Success,
            [Stage.UnderReview] = BadgeStyle.Warning,
            [Stage.Deferred]    = BadgeStyle.Danger,
        };
    }
}