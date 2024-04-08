using System;
using System.Collections.Generic;

namespace ScrumBoard.Utils;

public struct ChangelogForField
{
    public string FieldName { get; set; }
    public Change<object> Change { get; set; }
}

public static class ChangelogGenerator
{
    /// <summary>
    /// Calculates the changes between two objects, typically some old value and some new value of an object.
    /// </summary>
    /// <param name="oldObject">Old state of object</param>
    /// <param name="newObject">New state of object</param>
    /// <param name="fieldComparisons">Array of tuples in form (property name, old value selector function, new value selector function)</param>
    /// <typeparam name="T1">Type of old object</typeparam>
    /// <typeparam name="T2">Type of new object</typeparam>
    /// <returns>All changelogs for the fields specified as calculated between the two objects</returns>
    public static IEnumerable<ChangelogForField> GenerateChangesBetweenObjects<T1, T2>(
        T1 oldObject, 
        T2 newObject, 
        params (string, Func<T1, object>, Func<T2, object>)[] fieldComparisons
    ) {
        foreach (var fieldComparison in fieldComparisons)
        {
            var oldObjectVal = fieldComparison.Item2(oldObject);
            var newObjectVal = fieldComparison.Item3(newObject);
            
            if (Change<object>.Generate(oldObjectVal, newObjectVal) is { } change)
            {
                yield return new ChangelogForField
                {
                    FieldName = fieldComparison.Item1,
                    Change = change
                };
            }
        }
    }
    
    /// <summary>
    /// Calculates the changes on some object given a series of new values to be applied to fields.
    /// </summary>
    /// <param name="oldObject">Old state of object</param>
    /// <param name="fieldChanges">Array of tuples in form (property name, old value selector function, new value)</param>
    /// <typeparam name="T1">Type of old object</typeparam>
    /// <returns>All changelogs for the fields specified as calculated based on new field values given</returns>
    public static IEnumerable<ChangelogForField> GenerateChangesForObject<T1>(
        T1 oldObject, 
        params (string, Func<T1, object>, object)[] fieldChanges
    ) {
        foreach (var fieldChange in fieldChanges)
        {
            var oldObjectVal = fieldChange.Item2(oldObject);
            
            if (Change<object>.Generate(oldObjectVal, fieldChange.Item3) is { } change)
            {
                yield return new ChangelogForField
                {
                    FieldName = fieldChange.Item1,
                    Change = change
                };
            }
        }
    }
}