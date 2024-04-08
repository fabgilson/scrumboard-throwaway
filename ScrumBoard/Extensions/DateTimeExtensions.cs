using System;

namespace ScrumBoard.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Floor a datetime - round it to the nearest hour, minute, second etc
    /// </summary>
    /// <param name="dateTime">The DateTime to floor</param>
    /// <param name="interval">
    /// The interval over which you would like the DateTime rounded
    /// e.g. if you would like it floored to the nearest minute you could pass in TimeSpan.FromMinutes(1);
    /// </param>
    /// <returns></returns>
    public static DateTime Floor(this DateTime dateTime, TimeSpan interval)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
    }
}