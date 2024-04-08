using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Services;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages;

public partial class FillForms : BaseProjectScopedComponent
{
    [Inject] 
    public IFormInstanceService FormInstanceService { get; set; }

    private List<FormInstance> _allForms = new();

    private static bool CanEditForm(FormInstance formInstance)
    {
        if (formInstance.Assignment.AllowSavingBeforeStartDate) return true;

        return formInstance.Assignment.StartDate < DateTime.Now;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        
        var userForms = await FormInstanceService.GetUserFormsForUserInProject(Self.Id, Project.Id);
        var teamForms = new List<TeamFormInstance>();
        
        if (RoleInCurrentProject is ProjectRole.Developer or ProjectRole.Leader)
        {
            teamForms = (await FormInstanceService.GetTeamFormsForProject(Project.Id)).ToList();
        }
 
        _allForms = new List<FormInstance>();
        _allForms.AddRange(userForms);
        _allForms.AddRange(teamForms);
    }
}