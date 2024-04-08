using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Services;
using ScrumBoard.Shared.FormAdministration;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Blazor.Modals;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormAdministration;

public class AssignFormTemplateComponentTest : TestContext
{
    private bool _saved;
    private bool _canceled;

    private ICollection<Project> _projects;
    private readonly Project _project1 = new Project();
    private readonly Project _project2 = new Project();
    private string _formInstanceName = "Sprint 1 Peer Feedback";

    private IRenderedComponent<AssignFormTemplate> _component;

    private readonly Mock<IProjectService> _projectServiceMock = new(MockBehavior.Strict);
    private Mock<IFormInstanceService> _formInstanceServiceMock = new(MockBehavior.Strict);

    public AssignFormTemplateComponentTest()
    {
        _projects = new List<Project> { _project1, _project2 };
        _projectServiceMock.Setup(x => x.GetVirtualizedProjectsAsync(It.IsAny<VirtualizationRequest<Project>>()))
            .ReturnsAsync(new VirtualizationResponse<Project>
            {
                Results = _projects,
                TotalPossibleResultCount = _projects.Count
            });


        Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
        Services.AddScoped(_ => _projectServiceMock.Object);
        Services.AddScoped(_ => _formInstanceServiceMock.Object);

        ComponentFactories.Add(new ModalTriggerComponentFactory());
    }

    private void CreateComponent()
    {
        _formInstanceServiceMock = new Mock<IFormInstanceService>(MockBehavior.Strict);
        _formInstanceServiceMock
            .Setup(x => x.GetRecipients(It.IsAny<long>(), It.IsAny<Dictionary<ProjectRole, bool>>()))
            .ReturnsAsync(new List<ProjectUserMembership>());

        _saved = false;
        _canceled = false;
        var formTemplate = new FormTemplate
        {
            Id = 1,
            Name = "Peer Feedback"
        };
        _component = RenderComponent<AssignFormTemplate>(parameters => parameters
            .Add(cut => cut.OnSave, () => { _saved = true; })
            .Add(cut => cut.OnCancel, () => { _canceled = true; })
            .Add(cut => cut.FormTemplate, formTemplate));
    }

    private void SetupFormInstanceServiceMock()
    {
        _formInstanceServiceMock
            .Setup(fis => fis.CreateAndAssignFormInstancesAsync(It.IsAny<FormTemplateAssignmentForm>()))
            .Returns(Task.CompletedTask);
    }

    private void PopulateForm()
    {
        _component.InvokeAsync(() => _component.FindComponent<SearchableDropDown<Project>>().Instance
            .OnMultipleSelectionUpdated
            .InvokeAsync(new List<Project> { _project1 }));
        _component.Find("#assigned-form-name-input").Change(_formInstanceName);
    }

    private void PopulateTeamForm()
    {
        var linkedProject = new LinkedProjects
        {
            FirstProject = _project1,
            SecondProject = _project2
        };
        _component.InvokeAsync(() => _component.FindComponent<LinkedProjectSelector>().Instance.OnSelectionUpdated
            .InvokeAsync(new List<LinkedProjects> { linkedProject }));
        _component.Find("#assigned-form-name-input").Change(_formInstanceName);
    }

    private void AttemptSubmitForm(bool confirm)
    {
        _component.Find("#assign-form-template-form").Submit();
        if (confirm)
            _component.Find("#confirm-modal-button").Click();
    }

    [Fact]
    public void AssigningForm_CancelButtonPressed_CancelCallbackCalled()
    {
        CreateComponent();
        _component.Find("#cancel-btn").Click();
        _canceled.Should().BeTrue();
    }

    [Fact]
    public void AssigningForm_SubmitButtonPressed_SubmitCallbackCalled()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();
        PopulateForm();
        AttemptSubmitForm(true);
        _saved.Should().BeTrue();
    }

    [Fact]
    public void AssigningForm_ValidValues_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        PopulateForm();
        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.SelectedSingleProjects.SequenceEqual(new List<Project> { _project1 })
                && form.Name == _formInstanceName
                && form.SelectedRoles[ProjectRole.Developer] == true)));
    }


    [Fact]
    public void AssigningForm_MultipleRoles_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        PopulateForm();

        _component.Find("#guest-button").Click();
        _component.Find("#reviewer-button").Click();

        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.SelectedRoles[ProjectRole.Developer] == true
                && form.SelectedRoles[ProjectRole.Guest] == true
                && form.SelectedRoles[ProjectRole.Reviewer] == true
                && form.SelectedRoles[ProjectRole.Leader] == false)));
    }

    [Fact]
    public void AssigningForm_MultipleProjects_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        PopulateForm();
        _component.InvokeAsync(() => _component.FindComponent<SearchableDropDown<Project>>().Instance
            .OnMultipleSelectionUpdated
            .InvokeAsync(_projects));
        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.SelectedSingleProjects.SequenceEqual(_projects))));
    }

    [Fact]
    public void AssigningForm_Pairwise_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        PopulateForm();
        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Pairwise));
        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.AssignmentType == AssignmentType.Pairwise)));
    }

    [Fact]
    public void AssigningForm_TeamForm_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.AssignmentType == AssignmentType.Team)));
    }

    [Fact]
    public void AssigningForm_ValidStartDate_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        var startDate = DateTime.Now.AddDays(-1);

        PopulateForm();
        _component.Find("#enable-start-date-button").Click();

        _component.InvokeAsync(() =>
            _component.FindComponent<DateTimePicker>().Instance.ValueChanged.InvokeAsync(startDate));
        AttemptSubmitForm(true);
        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.StartDate.Value == startDate)));
    }

    [Fact]
    public void AssigningForm_StartDateAfterEndDate_ShowsErrorMessage()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        var startDate = DateTime.Now.AddDays(10);

        PopulateForm();
        _component.Find("#enable-start-date-button").Click();

        _component.InvokeAsync(() =>
            _component.FindComponent<DateTimePicker>().Instance.ValueChanged.InvokeAsync(startDate));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }

    [Fact]
    public void AssigningForm_NoProjects_Selected_ShowsErrorMessage()
    {
        CreateComponent();
        PopulateForm();
        _component.InvokeAsync(() => _component.FindComponent<SearchableDropDown<Project>>().Instance
            .OnMultipleSelectionUpdated
            .InvokeAsync(new List<Project>()));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }

    [Fact]
    public void AssigningTeamForm_NoProjectsSelected_ShowsErrorMessage()
    {
        CreateComponent();
        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        _component.InvokeAsync(() => _component.FindComponent<LinkedProjectSelector>().Instance.OnSelectionUpdated
            .InvokeAsync(new List<LinkedProjects>()));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }

    [Fact]
    public void AssigningTeamForm_OnlyFirstProjectSelected_ShowsErrorMessage()
    {
        CreateComponent();
        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        var linkedProject = new LinkedProjects
        {
            FirstProject = _project1
        };
        _component.InvokeAsync(() => _component.FindComponent<LinkedProjectSelector>().Instance.OnSelectionUpdated
            .InvokeAsync(new List<LinkedProjects> { linkedProject }));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }

    [Fact]
    public void AssigningTeamForm_OnlySecondProjectSelected_ShowsErrorMessage()
    {
        CreateComponent();
        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        var linkedProject = new LinkedProjects
        {
            SecondProject = _project2
        };
        _component.InvokeAsync(() => _component.FindComponent<LinkedProjectSelector>().Instance.OnSelectionUpdated
            .InvokeAsync(new List<LinkedProjects> { linkedProject }));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }
    
    [Fact]
    public void AssigningTeamForm_ProjectsSelectedAreNull_ShowsErrorMessage()
    {
        CreateComponent();
        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        var linkedProject = new LinkedProjects();
        _component.InvokeAsync(() => _component.FindComponent<LinkedProjectSelector>().Instance.OnSelectionUpdated
            .InvokeAsync(new List<LinkedProjects>{linkedProject}));
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }

    [Fact]
    public void AssigningForm_InvalidName_ShowsErrorMessage()
    {
        CreateComponent();
        PopulateForm();
        _component.Find("#assigned-form-name-input").Change("11111");
        AttemptSubmitForm(false);
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }
    
    [Fact]
    public void AssigningUserForm_NoRolesSelected_ShowsErrorMessage()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        PopulateForm();

        _component.Find("#developer-button").Click();

        AttemptSubmitForm(false);
        
        _component.FindAll(".validation-message").Count.Should().Be(1);
    }
    
    [Fact]
    public void AssigningTeamForm_NoRolesSelected_CallsServiceMethod()
    {
        CreateComponent();
        SetupFormInstanceServiceMock();

        _component.Find("#developer-button").Click();

        _component.InvokeAsync(() =>
            _component.FindComponent<InputRadioGroup<AssignmentType>>().Instance.ValueChanged
                .InvokeAsync(AssignmentType.Team));
        PopulateTeamForm();
        AttemptSubmitForm(true);

        _formInstanceServiceMock.Verify(fis => fis.CreateAndAssignFormInstancesAsync(It.Is<FormTemplateAssignmentForm>(
            form =>
                form.AssignmentType == AssignmentType.Team)));
    }
}