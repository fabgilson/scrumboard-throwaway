using FluentAssertions;
using ScrumBoard.Validators;
using System;
using Xunit;

namespace ScrumBoard.Tests.Unit.Validators
{
    /// <summary>
    /// Validator used for checking that a DateOnly field has a value that represents a date that is in the past
    /// </summary>
    /// <param name="services">The IServiceCollection used to configure this application's services</param>
    public class DateInPastTest
    {
        private DateInPast validator = new DateInPast();

        [Fact]
        public void IsValid_DateInFarFuture_FalseReturned()
        {
            validator.IsValid(DateOnly.MaxValue).Should().BeFalse();
        }

        [Fact]
        public void IsValid_Today_TrueReturned()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            validator.IsValid(today).Should().BeTrue();
        }

        [Fact]
        public void IsValid_Yesterday_TrueReturned()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
            validator.IsValid(yesterday).Should().BeTrue();
        }

        [Fact]
        public void IsValid_Tomorrow_FalseReturned() {
            var tomorrow = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            validator.IsValid(tomorrow).Should().BeFalse();
        }

        [Fact]
        public void IsValid_DateInDistantPast_TrueReturned()
        {
            validator.IsValid(DateOnly.MinValue).Should().BeTrue();
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
