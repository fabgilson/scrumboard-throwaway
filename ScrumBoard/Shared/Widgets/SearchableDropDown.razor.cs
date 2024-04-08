using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using ScrumBoard.Models;
using ScrumBoard.Services;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared.Widgets;

public partial class SearchableDropDown<T> : ComponentBase where T : class
{
    /// <summary>
    /// Delegate to return a virtualized set of objects returned by some search string
    /// </summary>
    [Parameter]
    public Func<VirtualizationRequest<T>, Task<VirtualizationResponse<T>>> SearchDelegate { get; set; }
    
    /// <summary>
    /// Delegate to convert generic item type to a string representation in the list
    /// </summary>
    [Parameter]
    public Func<T, string> ConvertItemToStringDelegate { get; set; }
    
    /// <summary>
    /// Delegate to generate text in button, given the number of results selected
    /// </summary>
    [Parameter]
    public Func<int, string> GenerateButtonTextDelegate { get; set; }
    
    [Parameter]
    public string StartingPrompt { get; set; }
    
    /// <summary>
    /// Whether to allow more than one item to be selected at a time
    /// </summary>
    [Parameter]
    public bool AllowMultipleSelect { get; set; }
    
    
    /// <summary>
    /// Whether to close the dropdown after one item is selected
    /// (instead of leaving it open so multiple items can be selected)
    /// </summary>
    [Parameter]
    public bool CollapseAfterSingleItemSelected { get; set; }
    
    /// <summary>
    /// Callback run when selection is changed if AllowMultipleSelect=false
    /// </summary>
    [Parameter]
    public EventCallback<T> OnSingleSelect { get; set; }
    
    /// <summary>
    /// Callback run when selection is changed if AllowMultipleSelect=true
    /// </summary>
    [Parameter]
    public EventCallback<IEnumerable<T>> OnMultipleSelectionUpdated { get; set; }
    
    /// <summary>
    /// Action to execute when a selection exists, and button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnPerformAction { get; set; }
    
    /// <summary>
    /// If this is set to true, clicking the main button will only ever open the dropdown, never invoking OnPerformAction
    /// </summary>
    [Parameter]
    public bool ButtonClickDoesNothing { get; set; }

    [Parameter] 
    public bool ClearSelectionAfterActionPerformed { get; set; }
    
    [Parameter]
    public bool HideSelectorWhenOneItemIsSelected { get; set; }
    
    /// <summary>
    /// Adds additional styling to the selected entries to make them look nicer when we have more space 
    /// </summary>
    [Parameter]
    public bool LargeSelectedEntries { get; set; }

    private string _searchString = "";
    private bool _showDropDown;

    private readonly IList<T> _currentlySelected = new List<T>();
    private Virtualize<T> _virtualizeComponent;
    private ElementReference _dropDownComponent;

    private bool _shouldDisplaySelector = true;
    
    private async Task PerformAction()
    {
        if (ButtonClickDoesNothing || !_currentlySelected.Any())
        {
            await ToggleDropDown();
            return;
        }
        await OnPerformAction.InvokeAsync();
        if (ClearSelectionAfterActionPerformed)
        {
            _showDropDown = false;
            _currentlySelected.Clear();
            await RefreshAfterSelectionChange();
        }
    }

    private async Task ToggleDropDown()
    {
        _showDropDown = !_showDropDown;
        if (_showDropDown)
        {
            await _dropDownComponent.FocusAsync();
            await RefreshVirtualizeData();
        }
    }

    private async Task RefreshVirtualizeData()
    {
        await _virtualizeComponent.RefreshDataAsync();
        StateHasChanged();
    }
    
    private async Task PerformSearch(ChangeEventArgs args)
    {
        _searchString = args.Value?.ToString() ?? "";
        await RefreshVirtualizeData();
    }

    private async ValueTask<ItemsProviderResult<T>> LoadItems(ItemsProviderRequest request)
    {
        var searchResult = await SearchDelegate(new VirtualizationRequest<T> {
            SearchQuery = _searchString,
            Count = request.Count,
            Excluded = _currentlySelected,
            StartIndex = request.StartIndex
        });
        return new ItemsProviderResult<T>(searchResult.Results, searchResult.TotalPossibleResultCount);
    }

    private async Task HandleSelect(MouseEventArgs mouseEventArgs, T item)
    {
        if (!AllowMultipleSelect) _currentlySelected.Clear();
        if (CollapseAfterSingleItemSelected) _showDropDown = false;
        _currentlySelected.Add(item);
        if (HideSelectorWhenOneItemIsSelected)
        {
            _shouldDisplaySelector = false;
        }
        await RefreshAfterSelectionChange();
    }

    private async Task DeSelectItem(T item)
    {
        _currentlySelected.Remove(item);
        if (HideSelectorWhenOneItemIsSelected)
        {
            _shouldDisplaySelector = true;
        }
        await RefreshAfterSelectionChange();
    }

    private async Task RefreshAfterSelectionChange()
    {
        if (!AllowMultipleSelect) await OnSingleSelect.InvokeAsync(_currentlySelected.FirstOrDefault());
        else await OnMultipleSelectionUpdated.InvokeAsync(_currentlySelected);
        await RefreshVirtualizeData();
    }
}