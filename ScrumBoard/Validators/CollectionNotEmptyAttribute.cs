using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CollectionNotEmptyAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        var collection = value as ICollection;
        return collection is { Count: > 0 };
    }
}