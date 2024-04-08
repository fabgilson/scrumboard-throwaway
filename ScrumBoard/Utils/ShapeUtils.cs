using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Utils;

public static class ShapeUtils
{
    /// <summary>
    /// Given two objects with the same shape compute the changes of all the properties 
    /// defined in the shape required to convert the first object into the second.
    /// Then apply all those changes onto the second object.
    /// </summary>
    /// <param name="source">Source for initial values</param>
    /// <param name="destination">Source for final values and object that will be updated</param>
    /// <param name="ignoredPropertyNames">Names of properties that should not be checked for changes</param>
    /// <returns>Enumerable of (field name, Change) tuples </returns>
    public static IEnumerable<(string, Change<object>)> ApplyChanges<TShape>(TShape source, TShape destination, params string[] ignoredPropertyNames) {
        return GenerateChanges(source, destination, true, ignoredPropertyNames);
    }
        
    /// <summary>
    /// Given two objects with the same shape compute the changes of all the properties 
    /// defined in the shape required to convert the first object into the second.
    /// Objects value are left untouched.
    /// </summary>
    /// <param name="source">Source for initial values</param>
    /// <param name="destination">Source for final value</param>
    /// <param name="ignoredPropertyNames">Names of properties that should not be checked for changes</param>
    /// <returns>Enumerable of (field name, Change) tuples </returns>
    public static IEnumerable<(string, Change<object>)> CalculateChangesOnly<TShape>(TShape source, TShape destination, params string[] ignoredPropertyNames) {
        return GenerateChanges(source, destination, false, ignoredPropertyNames);
    }

    /// <summary>
    /// Given two objects with the same shape compute the changes of all the properties 
    /// defined in the shape required to convert the first object into the second.
    /// Then apply all those changes onto the second object if shouldApplyChanges = true;
    /// </summary>
    /// <param name="source">Source for initial values</param>
    /// <param name="destination">Source for final values and object that will be updated</param>
    /// <param name="shouldApplyChanges">If true, will update values of destination object, otherwise it is left untouched</param>
    /// <param name="ignoredPropertyNames">Names of properties that should not be checked for changes</param>
    /// <returns>Enumerable of (field name, Change) tuples </returns>
    private static IEnumerable<(string, Change<object>)> GenerateChanges<TShape>(
        TShape source,
        TShape destination,
        bool shouldApplyChanges,
        params string[] ignoredPropertyNames
    ) {
        foreach (var property in typeof(TShape).GetProperties())
        {
            if (ignoredPropertyNames.Contains(property.Name)) continue;
            var sourceValue = property.GetValue(source);
            var destinationValue = property.GetValue(destination);

            if (Change<object>.Generate(destinationValue, sourceValue) is { } change)
            {
                if (shouldApplyChanges) property.SetValue(destination, sourceValue);
                yield return (property.Name, change);
            }
        }
    }
}