using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SharedLensResources.Extensions;

public static class EfExtensions {

    /// <summary>
    /// EFCore's in memory database provider does not serve sequences that implement IAsyncEnumerable.
    /// This means that ToListAsync cannot function when using the in memory database.
    /// To remedy this, this extension checks if the incoming IQueryable implements the correct interface,
    /// and if it does not, returns a syncronous list. 
    /// In production it will simply return the async list as normal.     
    /// </summary>  
    /// <param name="source">The IQueryable source to generate the list from</param>     
    /// <returns>An async task containing the requested list.</returns>
    public static Task<List<TSource>> ToListAsyncSafe<TSource>(this IQueryable<TSource> source) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!(source is IAsyncEnumerable<TSource>))
            return Task.FromResult(source.ToList());
        return source.ToListAsync();

    }

    /// <summary>
    /// EFCore's in memory database provider does not serve sequences that implement IAsyncEnumerable.
    /// This means that CountAsync cannot function when using the in memory database.
    /// To remedy this, this extension checks if the incoming IQueryable implements the correct interface,
    /// and if it does not, returns a syncronous count of the elements
    /// In production it will simply return the async count as normal.     
    /// </summary>  
    /// <param name="source">The IQueryable source to count the items in</param>     
    /// <returns>An async task containing the requested count integer.</returns>
    public static Task<int> CountAsyncSafe<TSource>(this IQueryable<TSource> source) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!(source is IAsyncEnumerable<TSource>))
            return Task.FromResult(source.Count());
        return source.CountAsync();
    }

}