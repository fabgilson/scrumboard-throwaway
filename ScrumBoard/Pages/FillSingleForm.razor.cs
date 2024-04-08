using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Services;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages;

public partial class FillSingleForm : BaseProjectScopedComponent
{
    [Inject] 
    public IFormInstanceService FormInstanceService { get; set; }

    [Parameter] 
    public long FormId { get; init; }

    private FormInstance _formInstance;
    private bool _userIsAllowedToView;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _userIsAllowedToView = false;
        if (_formInstance is null)
        {
            _formInstance = await FormInstanceService.GetUserFormInstanceById(FormId);
            if (_formInstance is null)
            {
                _formInstance = await FormInstanceService.GetTeamFormInstanceById(FormId);
            }
        }
        
        if (_formInstance is null)
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access non-existing form with ID={FormInstanceId}", Self.Id, FormId);
            NavigationManager.NavigateTo("", true);
            return;
        }

        if (_formInstance is UserFormInstance userFormInstance && userFormInstance.AssigneeId != Self.Id)
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access a form (ID={FormInstanceId}) without sufficient permissions", Self.Id, FormId);
            NavigationManager.NavigateTo("", true);
            return;
        }
        
        if (_formInstance is TeamFormInstance && RoleInCurrentProject is ProjectRole.Guest or ProjectRole.Reviewer)
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access a form (ID={FormInstanceId}) without sufficient permissions", Self.Id, FormId);
            NavigationManager.NavigateTo("", true);
            return;
        }

        if (!_formInstance.Assignment.AllowSavingBeforeStartDate && _formInstance.Assignment.StartDate > DateTime.Now)
        {
            Logger.LogWarning("User (ID={SelfId}) tried to access a form (ID={FormInstanceId}) before it opened", Self.Id, FormId);
            NavigationManager.NavigateTo("", true);
            return;
        }

        _userIsAllowedToView = true;
    }
}