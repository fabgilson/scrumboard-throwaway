using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Chart
{
    public partial class TaskEstimateBar : ComponentBase
    {
        [Parameter, EditorRequired]
        public TimeSpan EstimatedTime { get; set; }
        
        [Parameter, EditorRequired]
        public TimeSpan TimeSpent { get; set; }
        
        [Parameter]
        public DurationFormatOptions DurationFormat { get; set; }

        [Parameter]
        public string Title { get; set; }
        private bool HasOverrun => TimeSpent > EstimatedTime;
        private TimeSpan Total => new[] { EstimatedTime, TimeSpent }.Max();
    }
}