using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Repositories;
using ScrumBoard.Services;

namespace ScrumBoard.Pages;


public enum FormManagementState
{
    Overview,
    Assigning,
    Editing,
    Previewing,
    PreviewingEdit // This state exists because if you preview a form while editing it, we need to keep the edit
                   // component loaded so that when you exit the preview your changes are still there
}

public partial class AdminFormManagement : ComponentBase
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    
    [Inject]
    protected IConfigurationService ConfigurationService { get; set; }
    
    [Inject]
    protected IFormTemplateRepository FormTemplateRepository { get; set; }

    [Inject]
    protected ILogger<AdminFormManagement> Logger { get; set; }
    
    private List<FormTemplate> _formTemplates = new();

    private FormTemplate _editingFormTemplate;

    private FormTemplate _previewFormTemplate;

    private FormTemplate _assigningFormTemplate;

    private FormManagementState PageState
    {
        get
        {
            if (_assigningFormTemplate is not null)
            {
                return FormManagementState.Assigning;
            }
            
            if (_editingFormTemplate is not null && _previewFormTemplate is not null)
            {
                return FormManagementState.PreviewingEdit;
            }
            
            if (_editingFormTemplate is not null && _previewFormTemplate is null)
            {
                return FormManagementState.Editing;
            }

            if (_previewFormTemplate is not null)
            {
                return FormManagementState.Previewing;
            }
            
            return FormManagementState.Overview;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (!ConfigurationService.FeedbackFormsEnabled)
        {
            Logger.LogWarning("User tried to access feedback form page, while feedback forms are disabled");
            NavigationManager.NavigateTo("", true);
            return;
        }

        await UpdateForms();
    }

    private async Task UpdateForms()
    {
        _formTemplates = await FormTemplateRepository.GetAllAsync();
    }

    private void CreateForm()
    {
        _editingFormTemplate = new FormTemplate();
    }

    private async Task OnSave()
    {
        _editingFormTemplate = null;
        _assigningFormTemplate = null;
        await UpdateForms();
    }
    
    private async Task StartPreviewing(FormTemplate template)
    {
        if (template.Blocks is not null)
        {
            _previewFormTemplate = template;
        }
        else
        {
            var fullForm = await FormTemplateRepository.GetByIdAsync(template.Id, FormTemplateIncludes.Blocks);
            _previewFormTemplate = fullForm;
        }
    }
    
    private async Task StartEditing(FormTemplate template)
    {
        var fullTemplate = await FormTemplateRepository.GetByIdAsync(template.Id, FormTemplateIncludes.Blocks);
        _editingFormTemplate = fullTemplate;
    }

    private void StartAssigningFormTemplate(FormTemplate formTemplate)
    {
        _assigningFormTemplate = formTemplate;
    }
}