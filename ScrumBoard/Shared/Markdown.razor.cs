using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ScrumBoard.Pages;
using ScrumBoard.Services;

namespace ScrumBoard.Shared;

public partial class Markdown
{
    private string _content;

    private ElementReference _element;
    
    /// <summary>
    /// Raw markdown source to render
    /// </summary>
    [Parameter]
    public string Source { get; set; }
    
    /// <summary>
    /// Render the source as plain text, stripping out markdown syntax
    /// </summary>
    [Parameter]
    public bool NoFormat { get; set; }
    
    /// <summary>
    /// Find links that look like they are pointing to a student guide page and sanitise the links to work correctly.
    /// </summary>
    [Parameter]
    public bool SanitiseLinksForStudentGuideContent { get; set; }
    
    /// <summary>
    /// Whether or not to render HTML included in markdown content. This should NEVER be enabled for any markdown content
    /// that includes user content.
    /// </summary>
    [Parameter]
    public bool AllowHtml { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes {  get; set; }
    
    private MarkdownPipeline _pipeline;
    
    [Inject]
    protected ILogger<Markdown> Logger { get; set; } 
    
    [Inject]
    protected IJsInteropService JsInteropService { get; set; } 
    
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Whether this markdown component needs to redo code syntax highlighting on the next render
    /// </summary>
    private bool _needsReHighlighting = true;
    
    private string StudentGuideBaseUri => NavigationManager.ToAbsoluteUri(PageRoutes.ToStudentGuidePage()).AbsoluteUri;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        var pipelineBuilder = new MarkdownPipelineBuilder()
            .UseAutoIdentifiers(AutoIdentifierOptions.AutoLink | AutoIdentifierOptions.GitHub)
            .UseAutoLinks();
        if (!AllowHtml) pipelineBuilder = pipelineBuilder.DisableHtml();
        pipelineBuilder.DocumentProcessed += Postprocess;
        
        _pipeline = pipelineBuilder.Build();
        
        _content = NoFormat ? 
            Markdig.Markdown.ToPlainText(Source ?? "", _pipeline) : 
            Markdig.Markdown.ToHtml(Source ?? "", _pipeline);
        _needsReHighlighting = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_needsReHighlighting && !NoFormat)
        {
            try
            {
                await JsInteropService.HighlightDescendants(_element);
            }
            catch (JSException e)
            {
                Logger.LogWarning(e, "Calling JS function 'highlightDescendants' failed");
            }
        }
        // If an anchor was specified that is included in the SG markdown content, re-run navigation to scroll to it
        if (SanitiseLinksForStudentGuideContent && NavigationManager.Uri.Contains('#'))
        {
            NavigationManager.NavigateTo(NavigationManager.Uri, true);
        }
    }

    private bool LinkIsToOutsideWorld(LinkInline link) => 
        link.Url is not null
        && !NavigationManager.ToAbsoluteUri(link.Url).AbsoluteUri.StartsWith(NavigationManager.BaseUri);

    private bool ShouldTransformsLinkForStudentGuideContent(LinkInline link) => 
        SanitiseLinksForStudentGuideContent
        && NavigationManager.Uri.StartsWith(StudentGuideBaseUri);

    private void HandleImageLink(LinkInline link)
    {
        if (SanitiseLinksForStudentGuideContent)
        {
            link.Url = link.Url?.Replace("../imgs/", PageRoutes.StudentGuideMediaFolder);
        }
        else
        {
            if (!link.Descendants().Any())
            {
                link.AppendChild(new LiteralInline(link.Url ?? ""));
            }
            link.IsImage = false;
        }
    }

    /// <summary>
    /// If the link is to an external site, modify the generated html anchor element to open it in a new tab
    /// </summary>
    private static string HandleExternalLinkDynamicUrl(LinkInline link)
    {
        link.SetAttributes(new HtmlAttributes
        {
            Properties = new List<KeyValuePair<string, string>> { new("target", "_blank"), new("rel", "noopener noreferrer")}
        });
        return link.Url;
    }

    /// <summary>
    /// Extract possible anchor from link and replace URL with appropriate SG relative path
    /// </summary>
    private string HandleLocalLinkDynamicUrl(LinkInline link)
    {
        var endingAnchorParts = link.Url!.Split('#', 2);
        var filename = endingAnchorParts[0].TrimStart('.', '/');
        if (filename.EndsWith(".md")) filename = filename[..^3];
        if (endingAnchorParts.Length == 2)
        {
            var anchor = endingAnchorParts.Length == 2 ? endingAnchorParts[1] : null;
            if (!string.IsNullOrWhiteSpace(filename)) return PageRoutes.ToStudentGuidePage(filename, anchor);
            link.SetAttributes(new HtmlAttributes
            {
                // Workaround for blazor not scrolling to linked anchors properly
                // https://github.com/dotnet/aspnetcore/issues/8393#issuecomment-526545768
                Properties = new List<KeyValuePair<string, string>> { new("onclick", "event.stopPropagation();") }
            });
            return NavigationManager.Uri.Split('#')[0] + "#" + anchor;
        }
        if (filename.EndsWith(".md")) filename = filename[..^3];
        return PageRoutes.ToStudentGuidePage(filename);
    }

    /// <summary>
    /// Applies postprocessing steps on a parsed markdown document
    /// </summary>
    /// <param name="document">Document to postprocess</param>
    private void Postprocess(MarkdownDocument document)
    {
        // Replace any images with links
        foreach (var link in document.Descendants().OfType<LinkInline>())
        {
            if (link.IsImage)
            {
                HandleImageLink(link);
            }
            else if(ShouldTransformsLinkForStudentGuideContent(link)) {
                link.GetDynamicUrl = () =>
                {
                    if (link.Url is null) return "";
                    
                    return LinkIsToOutsideWorld(link) 
                        ? HandleExternalLinkDynamicUrl(link) 
                        : HandleLocalLinkDynamicUrl(link);
                };
            }
        }
    }
}