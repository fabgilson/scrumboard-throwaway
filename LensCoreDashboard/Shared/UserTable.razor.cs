using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using SharedLensResources;

namespace LensCoreDashboard.Shared;

public partial class UserTable
{
    private string _searchText = "";
    private long? _editingUserId;
    private UserResponse _editingUserNewValue, _editingUserOldValue;
    private bool _isSaveButtonLoading;
    private Virtualize<UserResponse> _userVirtualizer;

    [Inject]
    protected LensUserService.LensUserServiceClient LensUserServiceClient { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        
    }

    private async ValueTask<ItemsProviderResult<UserResponse>> LoadItems(ItemsProviderRequest request)
    {
        var usersResponse = await LensUserServiceClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            FilteringOptions = new BasicStringFilteringOptions
            {
                FilterText = _searchText, 
                FilterType = StringFilterType.Contains, 
                IsCaseSensitive = false
            },
            PaginationRequestOptions = new PaginationRequestOptions
            {
                Limit = request.Count,
                Offset = request.StartIndex
            }
        });
        return new ItemsProviderResult<UserResponse>(usersResponse.UserResponses, usersResponse.PaginationResponseOptions.ResultSetSize);
    }

    private async Task SaveEditingUser()
    {
        _isSaveButtonLoading = true;
        var request = new UpdateUserRequest
        {
            UserId = _editingUserId!.Value,
            NewEmail = _editingUserNewValue.Email == _editingUserOldValue.Email ? null : _editingUserNewValue.Email,
            NewFirstName = _editingUserNewValue.FirstName == _editingUserOldValue.FirstName ? null : _editingUserNewValue.FirstName,
            NewLastName = _editingUserNewValue.LastName == _editingUserOldValue.LastName ? null : _editingUserNewValue.LastName,
            NewUserName = _editingUserNewValue.UserName == _editingUserOldValue.UserName ? null : _editingUserNewValue.UserName,
        };
        // We set the role separately, as protobuf needs to know whether it has been explicitly set or not, given that 
        // it is marked as `optional`. The internal setter method sets a `hasBits` flag, which we want to keep accurate.
        if(_editingUserNewValue.LensRole != _editingUserOldValue.LensRole) request.NewRole = _editingUserNewValue.LensRole;
        
        await LensUserServiceClient.UpdateUserAsync(request);
        _isSaveButtonLoading = false;
        StopEditing();
        await RefreshVirtualizeData();
    }
    
    private async Task PerformSearch(string text)
    {
        _searchText = text;
        await RefreshVirtualizeData();
    }
    
    private async Task RefreshVirtualizeData()
    {
        await _userVirtualizer.RefreshDataAsync();
        StateHasChanged();
    }

    private void StopEditing()
    {
        _editingUserNewValue = null;
        _editingUserOldValue = null;
        _editingUserId = null;
    }

    private void StartEditing(UserResponse user)
    {
        _editingUserNewValue = user.Clone();
        _editingUserOldValue = user;
        _editingUserId = user.Id;
    }
}