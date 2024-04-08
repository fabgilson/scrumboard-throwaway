using ScrumBoard.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Extensions
{
    public static class EnumExtensions
    {
        private static readonly Dictionary<Stage, string> StageDescriptions = new Dictionary<Stage, string>{
                [Stage.Todo]        = "Todo",
                [Stage.InProgress]  = "In Progress",
                [Stage.Done]        = "Done",
                [Stage.UnderReview] = "Under Review",
                [Stage.Deferred]    = "Deferred",
        };

        /// <summary>
        /// Creates a new enum with a given flag set or cleared
        /// </summary>
        /// <param name="value">Enum to make apply operation on</param>
        /// <param name="flag">Flag to set or clear<param>
        /// <param name="set">Whether to add the flag onto the enum or clear the flag</param>
        /// <returns>Copy of the original enum with the provided flag set or cleared</param>
        public static T SetFlag<T>(this Enum value, T flag, bool set)
        { 
            Type underlyingType = Enum.GetUnderlyingType(value.GetType());

            dynamic valueAsIntegral = Convert.ChangeType(value, underlyingType);
            dynamic flagAsIntegral = Convert.ChangeType(flag, underlyingType);
            if (set)
            {
                valueAsIntegral |= flagAsIntegral;
            }
            else
            {
                valueAsIntegral &= ~flagAsIntegral;
            }

            return (T)valueAsIntegral;
        }

        public static string StageDescription(this Stage stage)
        {
            return StageDescriptions[stage];
        }
    }
}