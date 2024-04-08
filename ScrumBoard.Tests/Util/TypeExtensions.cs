using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ScrumBoard.Tests.Util;

public static class TypeExtensions
{
    /// <summary>
    /// Gets an attribute of some class using reflection to allow tests to check that the correct error text
    /// is shown for different scenarios. This method allows us to change the error message in the annotation
    /// without also needing to change the expected test field.
    /// </summary>
    /// <typeparam name="T">The specific type of attribute you are getting</typeparam>
    /// <param name="objectType">Type of object from which we are drawing the attribute</param>
    /// <param name="propertyName">Property for which we are getting the attribute</param>
    /// <returns>The first instance of the specified attribute type found on the specified property</returns>
    public static T GetAttribute<T>(this Type objectType, string propertyName) where T : Attribute
    {
        var attrType = typeof(T);
        var property = objectType.GetProperty(propertyName);
        return (T)property!.GetCustomAttributes(attrType, false).First();
    }
        
    /// <summary>
    /// Retrieves the error message from a validation attribute applied to a property of a class.
    /// This method is specifically designed for use with validation attributes derived from ValidationAttribute,
    /// allowing for easy retrieval of the ErrorMessage property. 
    /// </summary>
    /// <typeparam name="T">The type of validation attribute from which to retrieve the error message. Must be a subclass of ValidationAttribute.</typeparam>
    /// <param name="objectType">The Type of the object from which the validation attribute is to be retrieved.</param>
    /// <param name="propertyName">The name of the property to which the validation attribute is applied.</param>
    /// <returns>The error message string defined in the validation attribute.</returns>
    public static string GetErrorMessage<T>(this Type objectType, string propertyName) where T : ValidationAttribute
    {
        var attribute = objectType.GetAttribute<T>(propertyName);
        return attribute!.ErrorMessage;
    }
}