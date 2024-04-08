using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    public class NotEntirelyNumbersOrSpecialCharactersTest
    {
        private NotEntirelyNumbersOrSpecialCharactersAttribute validator = new NotEntirelyNumbersOrSpecialCharactersAttribute();
        
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
        public void IsValid_InputContainsOnlySpecialCharacters_FalseReturned()
        {
            validator.IsValid("#2-.").Should().BeFalse();
        }

        [Fact]
        public void IsValid_InputContainsOnlyNumbers_FalseReturned()
        {
            validator.IsValid("123456789").Should().BeFalse();
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
