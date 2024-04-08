using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
     /// <summary>
    /// Validator used for checking that a DateOnly field has a value that is within the next two years of the current date
    /// </summary>
    /// <param name="services">The IServiceCollection used to configure this application's services</param>
    public class DateWithinTwoYearsTest
    {
        private readonly DateWithinTwoYears _validator = new DateWithinTwoYears();

        [Fact]
        public void IsValid_DateBeyondTwoYears_FalseReturned()
        {
            _validator.IsValid(DateOnly.MaxValue).Should().BeFalse();
        }

        [Fact]
        public void IsValid_Today_TrueReturned()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            _validator.IsValid(today).Should().BeTrue();
        }

        [Fact]
        public void IsValid_TwoYearsMinusOneDay_TrueReturned()
        {
            var today = DateOnly.FromDateTime(DateTime.Now.AddYears(1).AddDays(364));
            _validator.IsValid(today).Should().BeTrue();
        }

        [Fact]
        public void IsValid_Yesterday_TrueReturned()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
            _validator.IsValid(yesterday).Should().BeTrue();
        }

        [Fact]
        public void IsValid_WrongDataTypeEntered_ExceptionThrown()
        {
            _validator.Invoking(y => y.IsValid("invalid")).Should()
                .Throw<ArgumentException>()
                .WithMessage("System.String cannot be validated using this validator");
        }
    }
}
