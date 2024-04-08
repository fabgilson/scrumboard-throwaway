using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Statistics;

namespace ScrumBoard.Shared.Chart
{
    public partial class ProjectStatsBar : ComponentBase
    {
        [Parameter]
        public IStatsBar StatsBar { get; set; }

        [Parameter]
        public ProjectStatsType Type { get; set; }
        
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public bool HideTotal { get; set; }

        [Parameter]
        public bool HideLegend { get; set; }

        /// <summary>
        /// Gets the pluralised string representation of the given quantity based on the 
        /// type of project statistic being displayed..
        /// </summary>
        /// <param name="quantity"></param>
        /// <returns></returns>
        private string GetPluralisedUnit(int quantity)
        {
            return quantity == 1 ? _unit[Type].Singular : _unit[Type].Plural;
        }

        private Dictionary<ProjectStatsType, (string Singular, string Plural)> _unit = new()
        {
            [ProjectStatsType.StoriesWorked] = (Singular: "story", Plural: "stories"), 
            [ProjectStatsType.StoriesWithTaskReviewed] = (Singular: "story", Plural: "stories"), 
            [ProjectStatsType.TasksWorked] = (Singular: "task", Plural: "tasks"), 
            [ProjectStatsType.TimeLogged] = (Singular: "hour", Plural: "hours"), 
            [ProjectStatsType.TagsWorked] = (Singular: "hour", Plural: "hours")
        };

        private string[] _colors = {
            "#f44336", 
            "#8fce00", 
            "#2986cc", 
            "#6a329f", 
            "#c90076", 
            "#f1c232", 
            "#e69138", 
            "#a64d79",
            "#444444",
            "#274e13",
            "#cc0000"
        };
    }
}