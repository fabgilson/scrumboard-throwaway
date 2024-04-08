using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Utils
{
    [Flags]
    public enum DurationFormatOptions
    {
        None = 0,
        ZeroAsEmptyString = 1,
        UseDaysAsLargestUnit = 2,
        FormatForLongString = 4,
        IgnoreSecondsInOutput = 8,
        TakeHighestUnitOnly = 16,
        TakeTwoHighestUnitsOnly = 32, // Overrides TakeHighestUnitOnly
        AlwaysShowAsPositiveValue = 64, // Removes any potential negative sign at beginning of string
    }
    
    public static class DurationUtils
    {
        private static readonly Dictionary<char, int> secondsPerTimeDivision = new Dictionary<char, int>{
            ['d'] = 60 * 60 * 24,
            ['h'] = 60 * 60,
            ['m'] = 60,
            ['s'] = 1,
        };

        public static TimeSpan? TimeSpanFromDurationString(string text) 
        {
            double totalDuration = 0;
            foreach (var segment in text.Split(' ')) {
                if (!segment.Any()) continue;

                var unitChar = Char.ToLower(segment.Last());
                if (!secondsPerTimeDivision.ContainsKey(unitChar)) return null;
                var unit = secondsPerTimeDivision[unitChar];

                if (!double.TryParse(segment.Substring(0, segment.Count() - 1), out var quantity)) return null;

                totalDuration += Math.Round(unit * quantity);
            }

            try {
                return TimeSpan.FromSeconds(Math.Round(totalDuration));
            } catch (OverflowException) {
                return null;
            }
        }

        /// <summary>
        /// Generate a duration string from a given timespan duration, with a number of formatting options available.
        /// </summary>
        /// <param name="duration">Duration to convert to a string</param>
        /// <param name="formattingOptionFlags">
        /// The following formatting options are available using the DurationFormatOptions flags:
        ///   - ZeroAsEmptyString = 1 -> If the duration is zero, returns an empty string, rather than "0s", or "0 seconds"
        ///   - UseDaysAsLargestUnit = 2 -> Use days as the largest unit, rather than hours (e.g 74 hours -> "3 days, 2 hours")
        ///   - FormatForLongString = 4 -> Use the full names of units, e.g "1 minute, 5 seconds" rather than "1m 5s"
        ///   - IgnoreSecondsInOutput = 8 -> Removes the seconds piece from output UNLESS seconds is highest unit
        ///   - TakeHighestUnitOnly = 16 -> Only return the highest found unit, e.g "1 day, 3 hours, 4 seconds" becomes "1 day"
        ///   - TakeTwoHighestUnitsOnly = 32 -> Only return the two highest found units, e.g "1 day, 3 hours, 4 seconds" becomes "1 day, 3 hours"
        /// </param>
        /// <returns></returns>
        public static string DurationStringFrom(TimeSpan duration, DurationFormatOptions formattingOptionFlags = DurationFormatOptions.None) {
            long seconds = (long)duration.TotalSeconds;
            long minutes = Math.DivRem(seconds, 60, out seconds);
            long hours = Math.DivRem(minutes, 60, out minutes);
            long? days = formattingOptionFlags.HasFlag(DurationFormatOptions.UseDaysAsLargestUnit) ? Math.DivRem(hours, 24, out hours) : null;

            List<string> pieces = new();
            if (days.HasValue && days != 0) {
                pieces.Add(FormatUnitToString(days.Value, "d", "day", "days", 
                    formattingOptionFlags.HasFlag(DurationFormatOptions.FormatForLongString),
                    formattingOptionFlags.HasFlag(DurationFormatOptions.AlwaysShowAsPositiveValue))
                );
            }
            if (hours != 0) {
                pieces.Add(FormatUnitToString(hours, "h", "hour", "hours", 
                    formattingOptionFlags.HasFlag(DurationFormatOptions.FormatForLongString),
                    formattingOptionFlags.HasFlag(DurationFormatOptions.AlwaysShowAsPositiveValue))
                );
            }
            if (minutes != 0) {
                pieces.Add(FormatUnitToString(minutes, "m", "minute", "minutes", 
                    formattingOptionFlags.HasFlag(DurationFormatOptions.FormatForLongString),
                    formattingOptionFlags.HasFlag(DurationFormatOptions.AlwaysShowAsPositiveValue))
                );
            }
            if (seconds != 0 || (!formattingOptionFlags.HasFlag(DurationFormatOptions.ZeroAsEmptyString) && !pieces.Any())) {
                if (!formattingOptionFlags.HasFlag(DurationFormatOptions.IgnoreSecondsInOutput) || !pieces.Any())
                {
                    pieces.Add(FormatUnitToString(seconds, "s", "second", "seconds", 
                        formattingOptionFlags.HasFlag(DurationFormatOptions.FormatForLongString),
                        formattingOptionFlags.HasFlag(DurationFormatOptions.AlwaysShowAsPositiveValue))
                    );
                }
            }

            if (formattingOptionFlags.HasFlag(DurationFormatOptions.TakeTwoHighestUnitsOnly) && pieces.Count >= 2)
            {
                pieces.RemoveRange(2, pieces.Count - 2);
            }
            else if (formattingOptionFlags.HasFlag(DurationFormatOptions.TakeHighestUnitOnly) && pieces.Count >= 1)
            {
                pieces.RemoveRange(1, pieces.Count - 1);
            }

            var final = string.Join(formattingOptionFlags.HasFlag(DurationFormatOptions.FormatForLongString) ? ", " : " ", pieces);
            if(formattingOptionFlags.HasFlag(DurationFormatOptions.AlwaysShowAsPositiveValue)) final = final.TrimStart('-');
            return final;
        }

        private static string FormatUnitToString(
            long value, 
            string shortText, 
            string longSingularText, 
            string longPluralText, 
            bool useLongFormat,
            bool alwaysShowAsPositiveValue
        ) {
            value = alwaysShowAsPositiveValue ? Math.Abs(value) : value;
            if (!useLongFormat) return $"{value}{shortText}";
            string unit = (value == 1) ? longSingularText : longPluralText;
            return $"{value} {unit}";
        }
    }
}