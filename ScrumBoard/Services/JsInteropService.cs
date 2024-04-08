using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace ScrumBoard.Services;

public interface IJsInteropService
{
    Task ChartSetup(ElementReference chartElem, object config);
    Task ChartSetup(string chartElemId, object config);
    Task UseExternalTooltip<T1>(ElementReference chartElem, DotNetObjectReference<T1> dotnet) where T1 : class;
    Task BlurElementAndDescendents(ElementReference element);
    Task MakeSortable<T1>(DotNetObjectReference<T1> dotnet, ElementReference root, double listKey, string handle, string groupKey) where T1 : class;
    Task ScrollToTop();
    Task ScrollTo(ElementReference element);
    Task<bool> WindowMatchMedia(string mediaString);
    Task UpdateTooltip(ElementReference elem, ElementReference target);
    Task ToggleCommitDropdown();
    Task<string> GetDocumentUrl();
    Task HighlightDescendants(ElementReference elem);
    Task AddClassToElement(string id, string className);
    Task RemoveClassFromElement(string id, string className);
    Task ApplyTutorialBlurring(string parentContainerId);
    Task ClickElement(ElementReference element);
    Task BindSlideNumberLabelForBootstrapCarousel(string carouselId, string labelId);
    Task MarkTextInsideElement(string containerId, string text);
    Task SetTooltipHtml(string tooltipId, string htmlContentContainerId);
}

public class JsInteropService : IJsInteropService
{
    private readonly IJSRuntime _jsRuntime;
    public JsInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    public async Task ChartSetup(ElementReference chartElem,object config)
    {
        await _jsRuntime.InvokeVoidAsync("chartSetup",chartElem,config);
    }
    public async Task ChartSetup(string chartElemId, object config)
    {
        await _jsRuntime.InvokeVoidAsync("chartSetupById", chartElemId, config);
    }
    public async Task UseExternalTooltip<T1>(ElementReference chartElem,DotNetObjectReference<T1> dotnet) where T1 : class
    {
        await _jsRuntime.InvokeVoidAsync("useExternalTooltip",chartElem,dotnet);
    }
    public async Task BlurElementAndDescendents(ElementReference element)
    {
        await _jsRuntime.InvokeVoidAsync("blurElementAndDescendents",element);
    }
    public async Task MakeSortable<T1>(DotNetObjectReference<T1> dotnet,ElementReference root,double listKey,string handle,string groupKey) where T1 : class
    {
        await _jsRuntime.InvokeVoidAsync("makeSortable",dotnet,root,listKey,handle,groupKey);
    }
    public async Task ScrollToTop()
    {
        await _jsRuntime.InvokeVoidAsync("scrollToTop");
    }
    public async Task ScrollTo(ElementReference element)
    {
        await _jsRuntime.InvokeVoidAsync("scrollToElement",element);
    }
    public async Task<bool> WindowMatchMedia(string mediaString)
    {
        return await _jsRuntime.InvokeAsync<bool>("windowMatchMedia",mediaString);
    }
    public async Task UpdateTooltip(ElementReference elem,ElementReference target)
    {
        await _jsRuntime.InvokeVoidAsync("updateTooltip",elem,target);
    }
    public async Task ToggleCommitDropdown()
    {
        await _jsRuntime.InvokeVoidAsync("ToggleCommitDropdown");
    }
    public async Task<string> GetDocumentUrl()
    {
        return await _jsRuntime.InvokeAsync<string>("getDocumentUrl");
    }
    public async Task HighlightDescendants(ElementReference elem)
    {
        await _jsRuntime.InvokeVoidAsync("highlightDescendants",elem);
    }

    public async Task AddClassToElement(string id, string className)
    {
        await _jsRuntime.InvokeVoidAsync("addClassToElement", id, className);
    }
    
    public async Task RemoveClassFromElement(string id, string className)
    {
        await _jsRuntime.InvokeVoidAsync("removeClassFromElement", id, className);
    }
        
    public async Task ApplyTutorialBlurring(string parentContainerId)
    {
        await _jsRuntime.InvokeVoidAsync("applyTutorialBlurring", parentContainerId);
    }

    public async Task ClickElement(ElementReference element)
    {
        await _jsRuntime.InvokeVoidAsync("clickElement", element);
    }

    public async Task BindSlideNumberLabelForBootstrapCarousel(string carouselId, string labelId)
    {
        await _jsRuntime.InvokeVoidAsync("bindSlideNumberLabelForBootstrapCarousel", carouselId, labelId);
    }

    public async Task MarkTextInsideElement(string containerId, string text)
    {
        await _jsRuntime.InvokeVoidAsync("markTextInsideElement", containerId, text);
    }

    public async Task SetTooltipHtml(string tooltipId, string htmlContentContainerId)
    {
        await _jsRuntime.InvokeVoidAsync("setTooltipHtml", tooltipId, htmlContentContainerId);
    }
}