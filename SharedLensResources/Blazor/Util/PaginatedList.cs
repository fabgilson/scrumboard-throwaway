using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedLensResources.Extensions;

namespace SharedLensResources.Blazor.Util
{
    /// <summary>
    /// Pagination class for any IQueryable source.
    /// Use CreateAsync to instantiate a new list instead of the constructor.
    /// </summary>
    public class PaginatedList<T> : List<T>
    {
        /// <summary>
        /// Number of this page, where 1 is first page
        /// </summary>
        public int PageNumber { get; }
        
        /// <summary>
        /// Total number of elements, including those outside of this page
        /// </summary>
        public int TotalCount { get; }
        
        /// <summary>
        /// Maximum number of elements within this page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int) Math.Ceiling(TotalCount / (double) PageSize);
        
        /// <summary>
        /// Whether there exists a maybe-empty page before this page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;
        
        /// <summary>
        /// Whether there exists a non-empty page after this page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// Use CreateAsync instead of this constructor to make a new PaginatedList (CreateAsync uses this internally)
        /// This is done because constructors cannot be asynchronous.
        public PaginatedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            TotalCount = count;
            PageSize = pageSize;

            AddRange(items);
        }

        /// <summary>
        /// Asynchronously determines the total item count and returns the requested page of items.
        /// This uses EfExtensions async methods (check their docs for more info)
        /// </summary>
        /// <param name="source">The IQueryable source to generate a list from</param> 
        /// <param name="pageNumber">The number of the page to retrieve items from</param>  
        /// <param name="pageSize">The size of the page to return</param>      
        /// <returns>An async task containing the requested paginated list.</returns>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = await source.CountAsyncSafe();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsyncSafe();
            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }

        public static PaginatedList<T> Empty(int pageSize) => new PaginatedList<T>(Array.Empty<T>(), 0, 1, pageSize);
    }
}