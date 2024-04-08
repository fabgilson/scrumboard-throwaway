using System;
using Moq;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Tests.Util;

[TypeMatcher]
public class IdTypeMatcher<T> : IId, ITypeMatcher where T : IId
{
    public long Id { get; set; }
    
    bool ITypeMatcher.Matches(Type typeArgument)
    {
        return typeof(T).IsAssignableFrom(typeArgument);
    }
}
