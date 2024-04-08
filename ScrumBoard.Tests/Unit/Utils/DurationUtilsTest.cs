using FluentAssertions;
using Xunit;
using ScrumBoard.Utils;
using System;

namespace ScrumBoard.Tests.Unit.Utils
{
    public class DurationUtilsTest
    {

        [Theory]
        [InlineData("1y")]
        [InlineData("60")]
        [InlineData("10mm")]
        [InlineData("h")]
        [InlineData(" m")]
        [InlineData("s ")]
        [InlineData("\t7m\t")]
        [InlineData("3m\n")]
        [InlineData("6s6s")]
        [InlineData("10000000000h")]
        [InlineData("-10000000000h")]
        public void TimeSpanFromDurationString_InvalidDurationString_NullReturned(string invalidDurationString)
        {
            var actualDuration = DurationUtils.TimeSpanFromDurationString(invalidDurationString);
            actualDuration.Should().BeNull();
        }


        [Theory]
        [InlineData("1h",       3600)]
        [InlineData("30m",      1800)]
        [InlineData("1.125h",   4050)]
        [InlineData("1h 1m 1s", 3661)]
        [InlineData("1h    1s", 3601)]
        [InlineData("2H",       7200)]
        [InlineData("1M",       60)]
        [InlineData("7S",       7)]
        [InlineData("1.001m",   60)]
        [InlineData("7.5s",     8)]
        [InlineData("7.49s",    7)]
        [InlineData("1s",       1)]
        [InlineData("10m 20m",  1800)]
        [InlineData("10m -10m", 0)]
        [InlineData("",         0)]
        [InlineData("   3s",    3)]
        [InlineData("3s    ",   3)]
        public void TimeSpanFromDurationString_ValidDurationString_ReturnsExpectedDuration(string validDurationString, int expectedDurationSeconds)
        {
            var actualDuration = DurationUtils.TimeSpanFromDurationString(validDurationString);
            var expectedDuration = TimeSpan.FromSeconds(expectedDurationSeconds);
            actualDuration.Should().Be(expectedDuration, $"\"{validDurationString}\" should parse to {expectedDuration}");
        }

        [Theory]
        [InlineData(0, "0s")]
        [InlineData(1, "1s")]
        [InlineData(90, "1m 30s")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h 1m 1s")]
        [InlineData(3601, "1h 1s")]
        public void DurationStringFrom_NotUsingZeroAsEmpty_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration);
            durationString.Should().Be(expectedDurationString);
        }

        [Theory]
        [InlineData(3601, "1h 1s")]
        [InlineData(90001, "25h 1s")]
        public void DurationStringFrom_LargestUnitNotDay_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration);
            durationString.Should().Be(expectedDurationString);
        }

        [Theory]
        [InlineData(3601, "1h 1s")]
        [InlineData(90001, "1d 1h 1s")]
        public void DurationStringFrom_LargestUnitIsDay_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration, DurationFormatOptions.UseDaysAsLargestUnit);
            durationString.Should().Be(expectedDurationString);
        }

        [Theory]
        [InlineData(1, "1 second")]
        [InlineData(30, "30 seconds")]
        [InlineData(90, "1 minute, 30 seconds")]
        [InlineData(180, "3 minutes")]
        [InlineData(3600, "1 hour")]
        [InlineData(3661, "1 hour, 1 minute, 1 second")]
        [InlineData(3601, "1 hour, 1 second")]
        [InlineData(90001, "25 hours, 1 second")]
        public void DurationStringFrom_UsesLongFormatWithoutDays_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration, DurationFormatOptions.FormatForLongString);
            durationString.Should().Be(expectedDurationString);
        }

        [Theory]
        [InlineData(3601, "1 hour, 1 second")]
        [InlineData(90001, "1 day, 1 hour, 1 second")]
        [InlineData(180002, "2 days, 2 hours, 2 seconds")]
        public void DurationStringFrom_UsesLongFormatWithDays_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration, 
                DurationFormatOptions.UseDaysAsLargestUnit | DurationFormatOptions.FormatForLongString
                );
            durationString.Should().Be(expectedDurationString);
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "1s")]
        [InlineData(90, "1m 30s")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h 1m 1s")]
        [InlineData(3601, "1h 1s")]
        public void DurationStringFrom_UsingZeroAsEmpty_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration, DurationFormatOptions.ZeroAsEmptyString);
            durationString.Should().Be(expectedDurationString);
        }
        
        [Theory]
        [InlineData(1, "1s")]
        [InlineData(90, "1m")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h 1m")]
        [InlineData(3601, "1h")]
        public void DurationStringFrom_IgnoreSeconds_ConvertedToExpectedString(int durationSeconds, string expectedDurationString)
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var durationString = DurationUtils.DurationStringFrom(duration, DurationFormatOptions.IgnoreSecondsInOutput);
            durationString.Should().Be(expectedDurationString);
        }
        
        [Theory]
        [InlineData(1, "1s")]
        [InlineData(90, "1m")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h")]
        [InlineData(3601, "1h")]
        [InlineData(90001, "1d", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(90601, "25h")]
        [InlineData(180002, "2d", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(180602, "50h")]
        public void DurationStringFrom_HighestUnitOnly_ConvertedToExpectedString(
            int durationSeconds, 
            string expectedDurationString, 
            DurationFormatOptions additionalOptions=DurationFormatOptions.None
        ) {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var formatOptions = DurationFormatOptions.TakeHighestUnitOnly | additionalOptions;
            var durationString = DurationUtils.DurationStringFrom(duration, formatOptions);
            durationString.Should().Be(expectedDurationString);
        }
        
        [Theory]
        [InlineData(1, "1s")]
        [InlineData(90, "1m 30s")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h 1m")]
        [InlineData(3601, "1h 1s")]
        [InlineData(90001, "1d 1h", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(90601, "25h 10m")]
        [InlineData(180002, "2d 2h", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(180602, "50h 10m")]
        public void DurationStringFrom_TwoHighestUnitsOnly_ConvertedToExpectedString(
            int durationSeconds, 
            string expectedDurationString, 
            DurationFormatOptions additionalOptions=DurationFormatOptions.None
        ) {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var formatOptions = DurationFormatOptions.TakeTwoHighestUnitsOnly | additionalOptions;
            var durationString = DurationUtils.DurationStringFrom(duration, formatOptions);
            durationString.Should().Be(expectedDurationString);
        }
        
        [Theory]
        [InlineData(1, "1s")]
        [InlineData(90, "1m 30s")]
        [InlineData(180, "3m")]
        [InlineData(3600, "1h")]
        [InlineData(3661, "1h 1m")]
        [InlineData(3601, "1h 1s")]
        [InlineData(90001, "1d 1h", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(90601, "25h 10m")]
        [InlineData(180002, "2d 2h", DurationFormatOptions.UseDaysAsLargestUnit)]
        [InlineData(180602, "50h 10m")]
        public void DurationStringFrom_TwoHighestUnitsOnlyAndHighestUnitOnly_ConvertedToExpectedStringBecauseTwoHighestOverridesHighest(
            int durationSeconds, 
            string expectedDurationString, 
            DurationFormatOptions additionalOptions=DurationFormatOptions.None
        ) {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var formatOptions = DurationFormatOptions.TakeHighestUnitOnly | DurationFormatOptions.TakeTwoHighestUnitsOnly | additionalOptions;
            var durationString = DurationUtils.DurationStringFrom(duration, formatOptions);
            durationString.Should().Be(expectedDurationString);
        }
    }
}
