using Bunit;
using FluentAssertions;
using Xunit;
using ScrumBoard.Shared.Modals;
using System.Threading.Tasks;

namespace ScrumBoard.Tests.Blazor.Modals
{
    public class ResultModalComponentTest : TestContext
    {
        private readonly IRenderedComponent<ResultModal<int>> _component;

        public ResultModalComponentTest() 
        {
            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());
            _component = RenderComponent<ResultModal<int>>(parameters => parameters
                .AddChildContent("<div id=\"test-div\"/>")
            );
        }

        [Fact]
        public void InitialState_WithContent_ContentHidden() {
            _component.FindAll("#test-div").Should().BeEmpty();
        }   

        [Fact]
        public void Show_WithContent_ContentShown() {
            var _ = _component.Instance.Show();
            _component.Render();
            _component.FindAll("#test-div").Should().HaveCount(1);
        }     

        [Fact]
        public async Task SetResult_AfterShowWithContent_ContentHidden() {
            var _ = _component.InvokeAsync(_component.Instance.Show);
            _component.Render();
            await _component.InvokeAsync(() => _component.Instance.SetResult(6));
            _component.Render();
            _component.FindAll("#test-div").Should().BeEmpty();
        }     

        [Fact]
        public async Task SetResult_AfterShow_ResultReturnedAsync() {
            var expectedValue = 6;
            int? actualValue = null;

            var task = _component.InvokeAsync(async () => {
                actualValue = await _component.Instance.Show();
            });
            _component.Instance.SetResult(expectedValue);
            await task;
            actualValue.Should().Be(expectedValue);
        }  
    }
}
