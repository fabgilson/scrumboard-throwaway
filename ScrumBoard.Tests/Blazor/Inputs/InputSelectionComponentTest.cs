using Bunit;
using FluentAssertions;
using ScrumBoard.Shared.Inputs;
using System.Collections.Generic;
using Xunit;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.Models.Entities;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.Models;
using ScrumBoard.Repositories;

namespace ScrumBoard.Tests.Blazor.Inputs
{
    public class InputSelectionComponentTest : TestContext
    {
        
        private readonly IRenderedComponent<InputSelection<string>> _component;

        private readonly List<string> _options = new()
        {
            "Hey",
            "Test value",
            "Another",
        };

        private readonly string _initialValue = "Initial";

        private readonly Mock<Action<string>> _onValueChanged = new(MockBehavior.Strict);

        public InputSelectionComponentTest()
        {
            _component = RenderComponent<InputSelection<string>>(
                parameters => parameters
                    .Add(cut => cut.Template, item => $"<span>{item}</span>")
                    .Add(cut => cut.Options, _options)
                    .Add(cut => cut.Value, _initialValue)
                    .Add(cut => cut.ValueChanged, _onValueChanged.Object)
            );
        }

        private void SetValue(string value) {
            _component.SetParametersAndRender(parameters => parameters
                .Add(cut => cut.Value, value)
            );
        }
        
        [Fact]
        public void Rendered_WithValue_ValueShown()
        {
            _component.Find("#selection-menu-button").TextContent.Should().Be(_initialValue);
        }

        [Fact]
        public void Select_ClickCorrespondingItemInDropdown_ItemSelected()
        {
            var selection = _options[1];
            _onValueChanged.Setup(mock => mock(selection));
            _component.FindAll("button.dropdown-item span").First(elem => elem.TextContent == selection).Click();
            _onValueChanged.Verify(mock => mock(selection), Times.Once());
        }
    }
}
