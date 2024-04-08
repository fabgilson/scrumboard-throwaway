using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Shared.FormAdministration;

public partial class AssignFormTemplate : ComponentBase
{
    [Parameter]
    public FormTemplate FormTemplate { get; set; }
    
    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }
    
    [Inject]
    protected IProjectService ProjectService { get; set; }
    
    [Inject]
    protected IFormInstanceService FormInstanceService { get; set; }
    
    private FormTemplateAssignmentForm _model;
    
    private EditContext _editContext;

    private ConfirmModal _confirmModal;

    private int _projectCount = 0;

    private int _instanceCount = 0;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _model = new(FormTemplate);
        _editContext = new(_model);
    }

    private async Task ShowConfirmation()
    {
        await CalculateNumberOfFormsToSendOut();
        
        var confirmed = await _confirmModal.Show();
        if (confirmed)
        {
            await CreateForms();
        }
    }

    private async Task CalculateNumberOfFormsToSendOut()
    {
        if (_model.AssignmentType is AssignmentType.Team)
        {
            var numProjects = _model.SelectedLinkedProjects.Count();
            _projectCount = numProjects;
            _instanceCount = numProjects;
            return;
        }
        
        _projectCount = _model.SelectedSingleProjects.Count();
        _instanceCount = 0;
        foreach (var project in _model.SelectedSingleProjects)
        {
            var memberships = await FormInstanceService.GetRecipients(project.Id, _model.SelectedRoles);
            _instanceCount += _model.AssignmentType == AssignmentType.Pairwise ? memberships.Count * (memberships.Count - 1) : memberships.Count;
        }
    }

    /// <summary> 
    /// Persists all new form template instances for the relevant members in the selected project
    /// </summary>
    /// <returns>A Task</returns>
    private async Task CreateForms()
    {
        await FormInstanceService.CreateAndAssignFormInstancesAsync(_model);
        await OnSave.InvokeAsync();
    }
    
    private async Task<VirtualizationResponse<Project>> SearchForProjects(VirtualizationRequest<Project> request)
    {
        return await ProjectService.GetVirtualizedProjectsAsync(request);
    }
    
    private void OnProjectSelectionChanged(IEnumerable<Project> projects)
    {
        ((FormTemplateAssignmentForm)_editContext.Model).SelectedSingleProjects = projects;
    }
    
    private void OnLinkedProjectSelectionChanged(IEnumerable<LinkedProjects> projects)
    {
        ((FormTemplateAssignmentForm)_editContext.Model).SelectedLinkedProjects = projects;
    }

    private void OnRoleSelectionChanged(ProjectRole role)
    {
        bool currentRole = ((FormTemplateAssignmentForm)_editContext.Model).SelectedRoles[role];
        ((FormTemplateAssignmentForm)_editContext.Model).SelectedRoles[role] = !currentRole;
    }
    
    /// <summary>
    /// Toggles the addition of a specific start date on or off.
    /// If there is no start date specified, the date will be set to the current day/time upon saving.
    /// </summary>
    private void ToggleStartDate()
    {
        _model.StartDateEnabled = !_model.StartDateEnabled;
        if (!_model.StartDateEnabled)
        {
            _model.StartDate = null;
        }
    }
}