using Bunit;
using FluentAssertions;
using Xunit;
using ScrumBoard.Shared.Modals;
using System.Threading.Tasks;


namespace ScrumBoard.Tests.Blazor.Modals
{
    public class ModalWrapperComponentTest : TestContext
    {
        private IRenderedComponent<ModalWrapper> _component; 

        private IRenderedComponent<ModalTrigger> _trigger;      

        public ModalWrapperComponentTest()
        {                

            _component = RenderComponent<ModalWrapper>(parameters => parameters
                .AddChildContent<ModalTrigger>(parameters => parameters
                    .AddChildContent("<div id=\"test-modal-content\">test<\\div>"))                                
            );
            _trigger = _component.FindComponent<ModalTrigger>();     

        }

        [Fact]
        public void InitialState_ModalNotVisible()
        {            
            _component.FindAll("#test-modal-content").Should().BeEmpty();
            _component.FindAll(".modal.show").Should().BeEmpty(); 
        }

        [Fact]
        public async Task ShowCalled_ModalVisible()
        {            
            await _component.InvokeAsync(() => _trigger.Instance.Show());    
            _component.FindAll("#test-modal-content").Should().HaveCount(1);       
            _component.FindAll(".modal.show").Should().HaveCount(1); 
        }
        
        [Fact]
        public async Task HideCalled_ModalNotVisible()
        {            
            await _component.InvokeAsync(() => _trigger.Instance.Show());            
            await _component.InvokeAsync(() => _trigger.Instance.Hide());
            _component.FindAll("#test-modal-content").Should().BeEmpty();            
            _component.FindAll(".modal.show").Should().BeEmpty(); 
        }

    }
}