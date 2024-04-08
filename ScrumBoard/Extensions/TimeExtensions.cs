using System;

namespace ScrumBoard.Extensions;

public static class TimeExtensions
{
    public static DateTime TrimSeconds(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
    }
}