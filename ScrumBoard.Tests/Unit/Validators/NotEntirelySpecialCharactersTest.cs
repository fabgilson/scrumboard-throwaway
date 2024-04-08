using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    public class NotEntirelySpecialCharactersTest
    {
        private NotEntirelySpecialCharacters validator = new NotEntirelySpecialCharacters();
        
        [Fact]
        public void IsValid_InputEmpty_TrueReturned()
        {
            validator.IsValid("").Should().BeTrue();
        }

        [Fact]
        public void IsValid_InputContainsNormalCharacters_TrueReturned()
        {
            validator.IsValid("foo bar").Should().BeTrue();
        }

        [Fact]
        public void IsValid_InputContainsSpecialCharacters_TrueReturned()
        {
            validator.IsValid("a#2-.").Should().BeTrue();
        }

        [Fact]
        public void IsValid_InputContainsOnlyNumbers_TrueReturned()
        {
            validator.IsValid("123456789").Should().BeTrue();
        }

        [Theory]
        [InlineData("abcd1234")]
        [InlineData("1234abcd")]
        [InlineData("1234@@@@abcd")]
        [InlineData("abcd@@@@1234")]
        public void IsValid_InputContainsNumbers_TrueReturned(string input)
        {
            validator.IsValid(input).Should().BeTrue();
        }

        [Fact]
        public void IsValid_NonStringInput_ExceptionThrown()
        {
            validator.Invoking(y => y.IsValid(1.0)).Should()
                .Throw<ArgumentException>()
                .WithMessage("System.Double cannot be validated using this validator");
        }
    }
}
