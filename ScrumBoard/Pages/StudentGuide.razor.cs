using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services;

namespace ScrumBoard.Pages;

public partial class StudentGuide : ComponentBase
{
    [Parameter]
    public string PageName { get; set; }
    
    [Inject]
    protected ILogger<StudentGuide> Logger { get; set; }
    
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    
    [Inject]
    protected IJsInteropService JsInteropService { get; set; }
    
    private const int MaxAllowedSearchCharacters = 50;
    
    private string _searchText;    
    private string _textToHighlight;
    private IEnumerable<StudentGuideSearchResponse> _searchResults;
    private CancellationTokenSource _searchCancellationToken;

    private string _markdownSource;
    private bool _isShowingErrorMessage;
    private bool _hideSearchResults;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        try
        {
            _markdownSource = await StudentGuideService.GetMarkdownContentAsync(PageName);
            _isShowingErrorMessage = false;
        }
        catch (Exception e)
        {
            _markdownSource = $"_Error reading Student Guide content, please try again later.  \n" +
                              $"Return to [Student Guide Home]({PageRoutes.ToStudentGuidePage()})_";
            _isShowingErrorMessage = true;
            if (e is InvalidOperationException)
            {
                Logger.LogError(e, "Invalid configuration provided for Student Guide service");
                return;
            }
            Logger.LogError(e, "Unexpected issue encountered when trying to access markdown content");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_textToHighlight is not null)
        {
            var text = _textToHighlight;
            _textToHighlight = null;
            await JsInteropService.MarkTextInsideElement("student-guide-markdown-container", text.Trim());
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Search(ChangeEventArgs e)
    {
        _hideSearchResults = false;
        _searchText = e.Value!.ToString();
        _searchCancellationToken?.Cancel();
        _searchCancellationToken = new CancellationTokenSource();
        
        _searchResults = null;
        var token = _searchCancellationToken.Token;
        await Task.Delay(250, token);
        await SearchStudentGuideContent(_searchText, token);
    }
    
    private async Task SearchStudentGuideContent(string searchText, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;
        try
        {
            await InvokeAsync(async () =>
            {
                _searchResults = await StudentGuideService.SearchForText(string.Concat(searchText.Take(MaxAllowedSearchCharacters)));
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // Ignore the exception if the task was cancelled
        }
    }

    private void NavigateToSearchResult(string toStudentGuidePage, string searchResultOriginalText)
    {
        _hideSearchResults = true;
        _textToHighlight = searchResultOriginalText;
        NavigationManager.NavigateTo(toStudentGuidePage);
    }
}