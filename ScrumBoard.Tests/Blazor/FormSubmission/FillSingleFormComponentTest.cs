using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormSubmission;

public class FillSingleFormComponentTest : BaseProjectScopedComponentTestContext<FillSingleForm>
{
    private IRenderedFragment _component;

    private readonly Mock<IFormInstanceService> _mockFormInstanceService = new(MockBehavior.Strict);

    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);

    private static readonly User CurrentUser = new() { Id = 1 };

    private static readonly User PairUser = new() { Id = 2 };

    private static readonly Project Project1 = new()
    {
        Id = 1,
        Name = "Team 100",
        MemberAssociations = new List<ProjectUserMembership>
        {
            new() { UserId = CurrentUser.Id, User = CurrentUser },
            new() { UserId = PairUser.Id, User = PairUser },
        }
    };

    private readonly UserFormInstance _userFormInstance1 = new()
    {
        Id = 1,
        Status = FormStatus.Submitted,
        AssigneeId = CurrentUser.Id
    };

    private readonly TeamFormInstance _teamFormInstance1 = new()
    {
        Id = 2,
        Status = FormStatus.Submitted,
        LinkedProject = Project1
    };

    private readonly Assignment _assignment1 = new()
    {
        Name = "Sprint 1 Self-Reflection",
        StartDate = DateTime.Now.AddDays(-1),
        EndDate = DateTime.Now.AddDays(2),
        RunNumber = 0,
    };

    private readonly Assignment _assignment2 = new()
    {
        Name = "Sprint 2 Self-Reflection",
        StartDate = DateTime.Now.AddDays(-1),
        EndDate = DateTime.Now.AddDays(2),
        RunNumber = 0,
    };

    private readonly FormTemplate _formTemplate1 = new()
    {
        Name = "Sprint 1 Self-Reflection",
        RunNumber = 0,
        Blocks = new List<FormTemplateBlock>()
    };

    private readonly FormTemplate _formTemplate2 = new()
    {
        Name = "Sprint 2 Self-Reflection",
        RunNumber = 0,
        Blocks = new List<FormTemplateBlock>()
    };

    public FillSingleFormComponentTest()
    {
        _mockProjectRepository.Setup(x =>
                x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(Project1);

        _assignment1.FormTemplate = _formTemplate1;
        _assignment2.FormTemplate = _formTemplate2;
        _userFormInstance1.Assignment = _assignment1;
        _teamFormInstance1.Assignment = _assignment2;

        SetupFormInstanceService(_userFormInstance1, _teamFormInstance1);

        Services.AddScoped(_ => _mockFormInstanceService.Object);
        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object);
    }

    private void SetupFormInstanceService(UserFormInstance userFormInstance, TeamFormInstance teamFormInstance)
    {
        _mockFormInstanceService.Setup(x => x.GetUserFormInstanceById(It.IsAny<long>()))
            .ReturnsAsync(userFormInstance);
        _mockFormInstanceService.Setup(x => x.GetTeamFormInstanceById(It.IsAny<long>()))
            .ReturnsAsync(teamFormInstance);
    }

    private void CreateComponent(ProjectRole projectRole = ProjectRole.Developer)
    {
        var projectState = new ProjectState
        {
            ProjectRole = projectRole,
            ProjectId = Project1.Id
        };

        _component = RenderComponent<FillSingleForm>(parameters => parameters
            .AddCascadingValue("Self", CurrentUser)
            .AddCascadingValue("ProjectState", projectState));
    }

    [Fact]
    public void PageRendered_FormDisplayed()
    {
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(1);
    }

    [Fact]
    public void PageRendered_UserFormInstanceNull_TeamFormDisplayed()
    {
        SetupFormInstanceService(null, _teamFormInstance1);
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(1);
    }

    [Fact]
    public void PageRendered_BothFormInstancesNull_FormNotDisplayed()
    {
        SetupFormInstanceService(null, null);
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(0);
    }

    [Fact]
    public void PageRendered_UserFormInstance_UserIsNotFormAssignee_FormNotDisplayed()
    {
        _userFormInstance1.AssigneeId = 0;
        SetupFormInstanceService(_userFormInstance1, null);
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(0);
    }

    [Theory]
    [InlineData(ProjectRole.Developer, true)]
    [InlineData(ProjectRole.Leader, true)]
    [InlineData(ProjectRole.Guest, false)]
    [InlineData(ProjectRole.Reviewer, false)]
    public void PageRendered_TeamFormInstance_OnlyDevelopersAndLeadersCanViewForm(ProjectRole projectRole,
        bool formDisplayed)
    {
        SetupFormInstanceService(null, _teamFormInstance1);
        CreateComponent(projectRole);
        var expectedContainerCount = formDisplayed ? 1 : 0;
        _component.FindAll("#form-response-container").Count.Should().Be(expectedContainerCount);
    }

    [Fact]
    public void PageRendered_CurrentDateIsBeforeFormOpeningDate_FormNotDisplayed()
    {
        _assignment1.StartDate = DateTime.Now.AddDays(1);
        SetupFormInstanceService(_userFormInstance1, _teamFormInstance1);
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(0);
    }

    [Fact]
    public void PageRendered_CurrentDateIsBeforeFormOpeningDate_FormAllowsSavingBeforeStartDate_FormDisplayed()
    {
        _assignment1.StartDate = DateTime.Now.AddDays(1);
        _assignment1.AllowSavingBeforeStartDate = true;
        CreateComponent();

        _component.FindAll("#form-response-container").Count.Should().Be(1);
    }
}