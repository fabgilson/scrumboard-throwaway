#nullable enable
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Google.Protobuf.Collections;
using SharedLensResources;

namespace IdentityProvider.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Generates the filter query as expected by System.Linq.Dynamic.Core.
    /// Making this a separate method also allows us to unit test more easily, as extension methods
    /// can't be mocked to be verified.
    /// </summary>
    /// <param name="filteringOptions">String filtering options to apply to dataset</param>
    /// <param name="searchedPropertyNames">The property names to apply string filter to, must be valid names of string properties.</param>
    /// <returns>Filter query supported by dynamic linq</returns>
    public static string GenerateStringFilterText(
        BasicStringFilteringOptions filteringOptions, 
        params string[] searchedPropertyNames)
    {
        Func<string,string> comparator = filteringOptions.FilterType is StringFilterType.MatchFullText
            ? (prop) => $" == {prop}"
            : (prop) => $".Contains({prop})";

        var predicates = searchedPropertyNames.Select(property => 
            filteringOptions.IsCaseSensitive
                ? $"{property}{comparator("@0")}"
                : $"{property}.ToLower(){comparator("@0.ToLower()")}"
        ).ToList();
        return string.Join(" || ", predicates);
    }
    
    /// <summary>
    /// For any IQueryable set, apply a string filter to it as described by some string filtering request options.
    /// Returns the union of results where multiple searched property names, i.e a result is returned if the filter
    /// matches ANY property.
    /// </summary>
    /// <param name="queryable">Dataset to operate on</param>
    /// <param name="filteringOptions">String filtering options to apply to dataset</param>
    /// <param name="searchedPropertyNames">The property names to apply string filter to, must be valid names of string properties.</param>
    /// <typeparam name="T">Type of IQueryable item</typeparam>
    /// <returns>IQueryable with only results that matched string filter criteria remaining</returns>
    public static IQueryable<T> ApplyStringFilter<T>(
        this IQueryable<T> queryable, 
        BasicStringFilteringOptions? filteringOptions, 
        params string[] searchedPropertyNames
    )
    {
        if (filteringOptions is null) return queryable;
        var finalQueryString = GenerateStringFilterText(filteringOptions, searchedPropertyNames);
        return queryable.Where(finalQueryString, filteringOptions.FilterText);
    }

    /// <summary>
    /// Orders an IQueryable as described by a set of OrderByOptions. 
    /// </summary>
    /// <param name="queryable">Dataset to operate on.</param>
    /// <param name="orderByOptions">Ordering descriptions in descending order of priority</param>
    /// <typeparam name="T">Type of IQueryable item</typeparam>
    /// <returns>IQueryable sorted according to given OrderByOptions</returns>
    public static IQueryable<T> ApplyOrderByOptions<T>(this IQueryable<T> queryable, RepeatedField<OrderByOption> orderByOptions)
    {
        if (orderByOptions.Count == 0) return queryable;
        var filterQuery = string.Join(
            ", ",
            orderByOptions.Select(x => $"{x.PropertyName} {(x.IsAscendingOrder ? "ASC" : "DESC")}")
        );
        return queryable.OrderBy(filterQuery);
    }

    /// <summary>
    /// Applies date time filtering options to an IQueryable with reference to some DateTime property on the entities.
    /// </summary>
    /// <param name="queryable">Dataset to operate on.</param>
    /// <param name="timeFilteringOptions">Description of datetime constraints to apply to dataset</param>
    /// <param name="propertySelector">Property of entity against which to apply datetime constraints</param>
    /// <typeparam name="T">Type of IQueryable item</typeparam>
    /// <returns>IQueryable with only results matching the given datetime filtering options present</returns>
    public static IQueryable<T> ApplyDateTimeFilterOptions<T>(this IQueryable<T> queryable,
        BasicDateTimeFilteringOptions? timeFilteringOptions, Func<T, DateTimeOffset> propertySelector)
    {
        if (timeFilteringOptions is null) return queryable;
        if (timeFilteringOptions.Earliest is not null)
        {
            queryable = queryable.Where(x => propertySelector(x) >= timeFilteringOptions.Earliest.ToDateTimeOffset());
        }
        if (timeFilteringOptions.Latest is not null)
        {
            queryable = queryable.Where(x => propertySelector(x) <= timeFilteringOptions.Latest.ToDateTimeOffset());
        }

        return queryable;
    }

    /// <summary>
    /// Allows for filtering some queryable dataset by a property value, but will only apply the filter if given
    /// filter value is not null.
    /// </summary>
    /// <param name="queryable">Dataset to operate on</param>
    /// <param name="optionalFilterValue">If not null, only returns results where given property is equal to this value</param>
    /// <param name="propertyName">Name of property to be filtered by, if optionalFilterValue is not null</param>
    /// <typeparam name="T">Type of the queryable items</typeparam>
    /// <returns>IQueryable with only items where selected property matches given value, or unchanged queryable where null filter value given</returns>
    public static IQueryable<T> ApplyOptionalPropertyValueFilter<T>(this IQueryable<T> queryable,
        object? optionalFilterValue, string propertyName)
    {
        return optionalFilterValue is null
            ? queryable
            : queryable.Where($"{propertyName}.Equals(@0)", optionalFilterValue);
    }
}