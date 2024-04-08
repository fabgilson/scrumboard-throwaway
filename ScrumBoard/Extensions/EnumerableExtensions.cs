using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Extensions;

public static class EnumerableExtensions
{
    public static TimeSpan Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> func)
    {
        return new TimeSpan(source.Sum(item => func(item).Ticks));
    }

    public static TimeSpan Sum(this IEnumerable<TimeSpan> source)
    {
        return new TimeSpan(source.Sum(item => item.Ticks));
    }
}