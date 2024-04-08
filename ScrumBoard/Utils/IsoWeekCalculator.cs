using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScrumBoard.Utils;

public static class IsoWeekCalculator
{
    public static int GetIsoWeekForDate(DateOnly date)
    {
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date.ToDateTime(new TimeOnly()), CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }

    public static IList<DateOnly> GetWeekStartsBetweenDates(DateOnly start, DateOnly end)
    {
        var weekStarts = new List<DateOnly>();
        var currentDay = start;

        var lastMondayDiff = DayOfWeekToMondayStart(DayOfWeek.Monday) - DayOfWeekToMondayStart(start.DayOfWeek);
        var lastMondayDate = start.AddDays(lastMondayDiff);
        weekStarts.Add(lastMondayDate);

        while (currentDay <= end)
        {
            if (DayOfWeekToMondayStart(currentDay.DayOfWeek) == DayOfWeekToMondayStart(DayOfWeek.Monday))
            {
                weekStarts.Add(currentDay);
            }

            currentDay = currentDay.AddDays(1);
        }

        return weekStarts.Distinct().ToList();    
    }

    /// <summary>
    /// Changes the start day of the week from Sunday to Monday in the DayOfWeek system enum.
    /// </summary>
    /// <param name="dayOfWeek"></param>
    /// <returns>The DayOfWeek enum value for the previous day</returns>
    public static DayOfWeek DayOfWeekToMondayStart(DayOfWeek dayOfWeek)
    {
        var shiftedDay = (int) (dayOfWeek - 1) % 7;
        return (DayOfWeek)(shiftedDay < 0 ? shiftedDay + 7 : shiftedDay);
    } 
}