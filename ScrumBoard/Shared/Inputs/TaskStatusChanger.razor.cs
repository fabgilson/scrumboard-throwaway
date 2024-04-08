using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Inputs
{
    public partial class TaskStatusChanger
    {
        [Parameter]
        public Stage Value { get; set; }

        [Parameter]
        public EventCallback<Stage> ValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> AdditionalAttributes { get; set; }

    }
}