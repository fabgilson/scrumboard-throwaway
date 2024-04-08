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
using ScrumBoard.Shared;
using ScrumBoard.Tests.Util;

namespace ScrumBoard.Tests.Blazor.Inputs
{
    public class InputMarkdownComponentTest : TestContext
    {
        
        private IRenderedComponent<InputMarkdown> _component;
        
        private readonly EditContext _editContext;

        private string _value = "hello world";
        private readonly Mock<Action<string>> _onValueChanged = new(MockBehavior.Strict);

        public InputMarkdownComponentTest()
        {
            _editContext = new(new {});
            
            ComponentFactories.AddDummyFactoryFor<Markdown>();
        }

        private void CreateComponent()
        {
            _component = RenderComponent<InputMarkdown>(
                parameters => parameters
                    .Add(c => c.Value, _value)
                    .Add(c => c.ValueChanged, _onValueChanged.Object)
                    .Add(c => c.ValueExpression, () => _value)
                    .AddCascadingValue(_editContext)
            );
        }
        
        
        [Fact]
        public void Rendered_WithValue_ValueShown()
        {
            CreateComponent();
            _component.Find("textarea").GetAttribute("value").Should().Be(_value);
        }
        
        [Fact]
        public void Rendered_UpdateValue_OnValueChangedTriggered()
        {
            var newValue = "new value";
            CreateComponent();
            
            _onValueChanged
                .Setup(mock => mock(newValue));
            
            _component.Find("textarea").Change(newValue);
            
            _onValueChanged
                .Verify(mock => mock(newValue), Times.Once);
        }

        [Fact]
        public void Rendered_WithValue_MarkdownPreviewShown()
        {
            CreateComponent();
            var markdown = _component.FindComponent<Dummy<Markdown>>().Instance;
            markdown.GetParam(c => c.Source).Should().Be(_value);
        }
    }
}
