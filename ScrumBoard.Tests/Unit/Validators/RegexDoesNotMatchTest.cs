using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    public class RegexDoesNotMatchTest
    {
        
        [Theory]
        [InlineData(@"Foo", "Foo")]
        [InlineData(@"\d", "4")]
        [InlineData(@".", "a")]
        [InlineData(@"^$", null)]
        public void IsValid_InputMatchesRegex_FalseReturned(String pattern, String text)
        {
            var validator = new RegexDoesNotMatch(pattern);
            validator.IsValid(text).Should().BeFalse();
        }

        [Theory]
        [InlineData(@"Foo", "Bar")]
        [InlineData(@"\d", "seven")]
        [InlineData(@".", "")]
        [InlineData(@".+", null)]
        public void IsValid_InputDoesNotMatchesRegex_TrueReturned(String pattern, String text)
        {
            var validator = new RegexDoesNotMatch(pattern);
            validator.IsValid(text).Should().BeTrue();
        }

        [Fact]
        public void IsValid_NonStringInput_ExceptionThrown()
        {
            var validator = new RegexDoesNotMatch("Foo");
            validator.Invoking(y => y.IsValid(1.0)).Should()
                .Throw<ArgumentException>()
                .WithMessage("System.Double cannot be validated using this validator");
        }
    }
}
