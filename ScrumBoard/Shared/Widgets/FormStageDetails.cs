using System.Collections.Generic;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Shared.Widgets;

public class FormStageDetails
{
    public static readonly Dictionary<FormStatus, string> StageDescriptions = new() {
        [FormStatus.Upcoming]    = "Upcoming",
        [FormStatus.Todo]        = "To Do",
        [FormStatus.Started]     = "Started",
        [FormStatus.Submitted]   = "Submitted"
    };

    public static readonly Dictionary<FormStatus, BadgeStyle> StageStyles = new() {
        [FormStatus.Upcoming]    = BadgeStyle.Dark,
        [FormStatus.Todo]        = BadgeStyle.Light,
        [FormStatus.Started]     = BadgeStyle.Primary,
        [FormStatus.Submitted]   = BadgeStyle.Success
    };
}