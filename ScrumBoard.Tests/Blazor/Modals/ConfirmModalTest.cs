using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using ScrumBoard.Shared.Modals;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Modals;

public class ConfirmModalTest : TestContext
{
    private IRenderedComponent<ConfirmModal> _component;

    private string _bodyMarkup = "<div id=\"test-body\"></div>";
    

    public ConfirmModalTest() 
    {
        // Add dummy ModalTrigger
        ComponentFactories.Add(new ModalTriggerComponentFactory());
        
    }

    private void CreateComponent()
    {
        _component = RenderComponent<ConfirmModal>(p => p.Add(x => x.Body, _bodyMarkup).Add(x => x.Title, "Header text"));
    }
    
    /// <summary>
    /// Shows the modal with _sprint as sprint to start review for
    /// </summary>
    /// <returns>
    /// Task that will complete when the modal is shown, which contains another task for when the modal returns a value
    /// </returns>
    private async Task<Task<bool>> Show()
    {
        Task<bool> showResultTask = null;
        await _component.InvokeAsync(() =>
        {
            showResultTask = _component.Instance.Show();
        });
        return showResultTask;
    }

    [Fact]
    public async Task CloseModal_ModalCloses()
    {
        CreateComponent();
        var result = await Show();
        _component.FindAll(".modal-body").Should().NotBeEmpty();
        _component.Find("#cancel-modal-button").Click();
        (await result).Should().BeFalse();
    }
    
    [Fact]
    public async Task ConfirmModal_ModalReturnsTrue()
    {
        CreateComponent();
        var result = await Show();
        _component.FindAll(".modal-body").Should().NotBeEmpty();
        _component.Find("#confirm-modal-button").Click();
        (await result).Should().BeTrue();
    }
    
    [Fact]
    public async Task OpenModal_ModalBodyDisplayed()
    {
        CreateComponent();
        var result = await Show();
        _component.Find(".modal-body").InnerHtml.Should().Contain(_bodyMarkup);
    }
    
    [Fact]
    public async Task OpenModal_ModalHeaderDisplayed()
    {
        CreateComponent();
        var result = await Show();
        _component.Find("#confirm-modal-header").TextContent.Should().BeEquivalentTo("Header text");
    }
}