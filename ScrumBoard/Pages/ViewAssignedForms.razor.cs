using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Services;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages;

public partial class ViewAssignedForms : BaseProjectScopedComponent
{
    [Inject] protected IFormInstanceService FormInstanceService { get; set; }
    
    private ICollection<Assignment> _assignmentsForProject = [];

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _assignmentsForProject = await FormInstanceService.GetAllAssignmentsForProjectAsync(Project.Id);
    }
}