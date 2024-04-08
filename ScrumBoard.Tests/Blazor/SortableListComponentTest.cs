using System.Collections.Generic;
using Bunit;
using ScrumBoard.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using Xunit;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace ScrumBoard.Tests.Blazor
{
    public class SortableListComponentTest : TestContext
    {
        private readonly int _key = 14;

        private readonly string _fullGroupKey = "test_group_2";

        private readonly string _group = "test_group";

        private readonly string _handle = "test_handle";

        private Mock<ISortableService<int>> _mockSortableService = new();
        private Mock<IJsInteropService> _mockJSInteropService = new();

        private IRenderedComponent<SortableList<int>> _component;

        private List<int> _data = new() { 2, 4, 6 };

        public SortableListComponentTest() 
        {
            _mockSortableService.Setup(mock => mock.Register(It.IsAny<SortableList<int>>())).Returns(_key);
            _mockSortableService.Setup(mock => mock.GetGroupKey(It.IsAny<string>())).Returns(_fullGroupKey);
            Services.AddScoped(_ => _mockSortableService.Object);
            Services.AddScoped(_ => _mockJSInteropService.Object);
            _component = RenderComponent<SortableList<int>>(parameters => parameters
                .Add(component => component.Items, _data)
                .Add(component => component.Group, _group)
                .Add(component => component.Handle, _handle)
                .Add(component => component.Template, item => $"<span id=\"item-{item}\"/>")
            );             
        }

        [Fact]
        public void ComponentRendered_NoChangesMade_AllItemsRendered() 
        {
            foreach (var item in _data) {
                _component.FindAll($"#item-{item}").Should().HaveCount(1);
            }
        }

        [Fact]
        public void ComponentRendered_Initialised_makeSortableCalled() 
        {
            _mockJSInteropService.Verify(mock => mock.MakeSortable(It.IsAny<DotNetObjectReference<SortableList<int>>>(), It.IsAny<ElementReference>(), _key, _handle, _fullGroupKey));
        }

        [Fact]
        public void ComponentRendered_Initialised_SortableRegisteredWithService() 
        {
            _mockSortableService.Verify(mock => mock.Register(_component.Instance), Times.Once());
        }

        [Fact]
        public async Task ComponentRendered_SortableEventOccurred_SortableServiceNotified() 
        {
            var args = new SortableEventArgs();
            
            await _component.InvokeAsync(() => _component.Instance.OnSortableEnd(args));
            _mockSortableService.Verify(mock => mock.HandleEvent(args));
        }

        [Fact]
        public void ComponentRendered_Disposed_SortableUnregisteredWithService() 
        {
            _mockSortableService.Verify(mock => mock.Unregister(It.IsAny<int>()), Times.Never());

            _component.Instance.Dispose();

            _mockSortableService.Verify(mock => mock.Unregister(_key), Times.Once());
        }

        [Fact]
        public void ComponentRendered_ItemsUpdated_SortableServiceAskedToSyncronizeGroup() {

            _mockSortableService.Verify(mock => mock.SynchronizeGroup(_group), Times.Once);
            
            _data.Add(14);
            _component.SetParametersAndRender(parameters => parameters.Add(cut => cut.Items, _data));

            _mockSortableService.Verify(mock => mock.SynchronizeGroup(_group), Times.Exactly(2));
        }
    }
}