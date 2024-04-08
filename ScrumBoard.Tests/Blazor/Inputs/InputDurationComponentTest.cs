using Bunit;
using FluentAssertions;
using ScrumBoard.Shared.Inputs;
using System.Collections.Generic;
using Xunit;
using Microsoft.AspNetCore.Components.Forms;
using System;

namespace ScrumBoard.Tests.Blazor.Inputs
{
    public class TestModel {
        public TimeSpan Duration { get; set; }
    }

    public class InputDurationComponentTest : TestContext
    {

        private readonly IRenderedComponent<InputDuration> _component;
        private readonly TestModel _model = new();
        private EditContext _editContext;

        public InputDurationComponentTest() 
        {
            _editContext = new(_model);

            _component = RenderComponent<InputDuration>(parameters => parameters
                .AddCascadingValue(_editContext)
                .Add(cut => cut.ValueExpression, () => _model.Duration)
            );
        }

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
        public void SetInput_InvalidDurationString_ErrorMessageDisplayed(string invalidDurationString)
        {
            var input = _component.Find("input");
            input.Change(invalidDurationString);

            _editContext.GetValidationMessages().Should().BeEquivalentTo(new List<string>{"Invalid duration format"});
        }

        public static IEnumerable<object[]> ValidDurations =>
        new List<object[]>
        {
            new object[]{"1h",       3600},
            new object[]{"30m",      1800},
            new object[]{"1.125h",   4050},
            new object[]{"1h 1m 1s", 3661},
            new object[]{"1h    1s", 3601},
            new object[]{"2H",       7200},
            new object[]{"1M",       60},
            new object[]{"7S",       7},
            new object[]{"1.001m",   60},
            new object[]{"7.5s",     8},
            new object[]{"7.49s",    7},
            new object[]{"1s",       1},
            new object[]{"10m 20m",  1800},
            new object[]{"10m -10m", 0},
            new object[]{"",         0},
            new object[]{"   3s",    3},
            new object[]{"3s    ",   3},
        };

        [Theory]
        [MemberData(nameof(ValidDurations))]
        public void SetInput_ValidDurationString_NoValidationMessage(string validDurationString, int duration)
        {
            var input = _component.Find("input");
            input.Change(validDurationString);

            

            _editContext.GetValidationMessages().Should().BeEmpty($"\"{validDurationString}\" should parse to {duration}");
        }

        [Theory]
        [MemberData(nameof(ValidDurations))]
        public void SetInput_ValidDurationString_FieldValueShouldBeExpectedDuration(string validDurationString, int durationSeconds)
        {
            var input = _component.Find("input");
            input.Change(validDurationString);
            var duration = TimeSpan.FromSeconds(durationSeconds);
            _component.Instance.Value.Should().Be(duration, $"\"{validDurationString}\" should parse to {duration}");
        }
    }
}
