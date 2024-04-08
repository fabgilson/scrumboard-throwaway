using System;
using FluentAssertions;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit
{
    public class DateOnlyTypeConverterTest
    {
        private readonly DateOnlyTypeConverter dateOnlyTypeConverter = new();


        [Fact]
        public void CanConvertFrom_String_ReturnsTrue()
        {
           dateOnlyTypeConverter.CanConvertFrom(typeof(string)).Should().BeTrue(); 
        }

        [Fact]
        public void CanConvertFrom_NotString_ReturnsFalse()
        {
           dateOnlyTypeConverter.CanConvertFrom(typeof(int)).Should().BeFalse(); 
        }

        [Fact]
        public void CanConvertTo_String_ReturnsTrue()
        {
           dateOnlyTypeConverter.CanConvertTo(typeof(string)).Should().BeTrue(); 
        }

        [Fact]
        public void CanConvertTo_NotString_ReturnsFalse()
        {
           dateOnlyTypeConverter.CanConvertTo(typeof(int)).Should().BeFalse(); 
        }

        [Theory]
        [InlineData(2021, 12, 17)]
        [InlineData(1, 1, 1)]
        [InlineData(2012, 12, 21)]
        public void ConvertFromThenTo_AnyInputDate_DateIsSameAsInput(int year, int month, int day)
        {
            var expectedDate = new DateOnly(year, month, day);
            var dateString = dateOnlyTypeConverter.ConvertToString(null, null, expectedDate);
            var actualDate = dateOnlyTypeConverter.ConvertFromString(null, null, dateString);

            actualDate.Should().Be(expectedDate);
        }
    }
}