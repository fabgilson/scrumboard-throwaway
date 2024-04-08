using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class MarkdownComponentTest : TestContext
    {
        private Mock<IJsInteropService> _mockJsInteropService = new(MockBehavior.Strict);
        
        private IRenderedComponent<Markdown> _component;

        public MarkdownComponentTest()
        {
            Services.AddScoped(_ => _mockJsInteropService.Object);
            _mockJsInteropService
                .Setup(mock => mock.HighlightDescendants(It.IsAny<ElementReference>()))
                .Returns(Task.CompletedTask);
        }

        private void CreateComponent(string source, bool noFormat)
        {
            _component = RenderComponent<Markdown>(builder => builder
                .Add(c => c.Source, source)
                .Add(c => c.NoFormat, noFormat));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_ScriptTagInContent_TextContentContainsScript(bool noFormat)
        {
            var content = "<script>console.log('hello world');</script>";
            CreateComponent(content, noFormat);

            _component.Find(".markdown").TextContent.Trim().Should().Be(content);
            _component.FindAll("script").Should().BeEmpty();
        }

        [Fact]
        public void Rendered_NoFormat_MarkdownSyntaxRemoved()
        {
            var content = "**bold**\n # Heading";
            CreateComponent(content, true);
            _component.Find(".markdown").TextContent.Trim().Should().Be("bold\nHeading");
            _component.FindAll("strong").Should().BeEmpty();
            _component.FindAll("h1").Should().BeEmpty();
        }
        
        [Fact]
        public void Rendered_Formatted_MarkdownSyntaxConvertedToHtml()
        {
            var content = "**bold**\n # Heading";
            CreateComponent(content, false);
            _component.Find(".markdown").TextContent.Should()
                .NotContain("*").And
                .NotContain("#");
            _component.FindAll("strong").Should().ContainSingle();
            _component.FindAll("h1").Should().ContainSingle();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rendered_NoFormatSet_HighlightsDecendantsBasedOnNoFormat(bool noFormat)
        {
            var content = "doesn't matter";
            CreateComponent(content, noFormat);
            
            _mockJsInteropService
                .Verify(mock => mock.HighlightDescendants(It.IsAny<ElementReference>()), Times.Exactly(noFormat ? 0 : 1));
        }

        [Fact]
        public void Rendered_WithImage_ImageRenderedAsLink()
        {
            var url = "https://localhost/image.png";
            var label = "image";
            var content = $"![{label}]({url})";
            CreateComponent(content, false);

            _component.FindAll("img").Should().BeEmpty();
            var link = _component.FindAll("a").Should().ContainSingle().Which;
            link.TextContent.Should().Be(label);
            link.GetAttribute("href").Should().Be(url);
        }
    }
}

