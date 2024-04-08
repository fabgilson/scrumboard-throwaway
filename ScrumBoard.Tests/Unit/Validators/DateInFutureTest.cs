using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    /// <summary>
    /// Validator used for checking that a DateOnly field has a value that represents a date that is either today or in the future
    /// </summary>
    /// <param name="services">The IServiceCollection used to configure this application's services</param>
    public class DateInFutureTest
    {
        private DateInFuture validator = new DateInFuture();

        [Fact]
        public void IsValid_DateInFarFuture_TrueReturned()
        {
            validator.IsValid(DateOnly.MaxValue).Should().BeTrue();
        }

        [Fact]
        public void IsValid_Today_TrueReturned()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            validator.IsValid(today).Should().BeTrue();
        }

        [Fact]
        public void IsValid_Yesterday_FalseReturned()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
            validator.IsValid(yesterday).Should().BeFalse();
        }

        [Fact]
        public void IsValid_DateInDistantPast_FalseReturned()
        {
            validator.IsValid(DateOnly.MinValue).Should().BeFalse();
        }

        [Fact]
        public void IsValid_WrongDataTypeEntered_ExceptionThrown()
        {
            validator.Invoking(y => y.IsValid("invalid")).Should()
                .Throw<ArgumentException>()
                .WithMessage("System.String cannot be validated using this validator");
        }
    }
}
