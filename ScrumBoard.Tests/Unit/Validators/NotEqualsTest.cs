using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    public class NotEqualsTest
    {
        
        [Theory]
        [InlineData(1)]
        [InlineData("a")]
        [InlineData(null)]
        public void IsValid_InputIsEqual_FalseReturned(object value)
        {
            var validator = new NotEquals(value);
            validator.IsValid(value).Should().BeFalse();
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData("Foo", "Bar")]
        [InlineData(1, "a")]
        [InlineData(null, "a")]
        [InlineData("a", null)]
        public void IsValid_InputIsNotEqual_TrueReturned(object value, object input)
        {
            var validator = new NotEquals(value);
            validator.IsValid(input).Should().BeTrue();
        }
    }
}
