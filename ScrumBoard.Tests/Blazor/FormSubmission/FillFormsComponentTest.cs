using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Widgets;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormSubmission;

public class FillFormsComponentTest : TestContext
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
            new(){UserId = CurrentUser.Id, User = CurrentUser},
            new(){UserId = PairUser.Id, User = PairUser},
        }
    };
    
    private static readonly Project Project2 = new()
    {
        Id = 2, 
        Name = "Team 200",
        MemberAssociations = new List<ProjectUserMembership>
        {
            new(){UserId = CurrentUser.Id, User = CurrentUser},
            new(){UserId = PairUser.Id, User = PairUser},
        }
    };

    private readonly Assignment _assignment1 = new()
    {
        Name = "Sprint 1 Self-Reflection",
        StartDate = DateTime.Now.AddDays(1),
        EndDate = DateTime.Now.AddDays(2),
        RunNumber = 0,
    };
    
    private readonly Assignment _assignment2 = new()
    {
        Name = "Sprint 2 Self-Reflection",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(1),
        RunNumber = 1,
    };
    
    private readonly Assignment _assignment3 = new()
    {
        Name = "Sprint 3 Self-Reflection",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(1),
        RunNumber = 2,
    };
    
    private readonly Assignment _assignment4 = new()
    {
        Name = "Sprint 4 Self-Reflection",
        StartDate = DateTime.Now.AddDays(-2),
        EndDate = DateTime.Now.AddDays(-1),
        RunNumber = 3,
    };
    
    private readonly UserFormInstance _userFormInstance1 = new()
    {
        Id = 1,
        Status = FormStatus.Submitted,
    };
    
    private readonly UserFormInstance _userFormInstance2 = new()
    {
        Id = 2,
        Status = FormStatus.Started,
        Pair = PairUser,
        PairId = PairUser.Id
    };
    
    private readonly UserFormInstance _userFormInstance3 = new()
    {
        Id = 3,
        Status = FormStatus.Upcoming,
    };
    
    private readonly UserFormInstance _userFormInstance4 = new()
    {
        Id = 4,
        Status = FormStatus.Todo,
    };
    
    private readonly TeamFormInstance _teamFormInstance1 = new()
    {
        Id = 4,
        Status = FormStatus.Todo,
        ProjectId = Project2.Id,
        LinkedProjectId = Project1.Id
    };
    
    private readonly TeamFormInstance _teamFormInstance2 = new()
    {
        Id = 4,
        Status = FormStatus.Todo,
        ProjectId = Project1.Id,
        LinkedProjectId = Project2.Id
    };
    
    private readonly Assignment _assignment5 = new()
    {
        Name = "Sprint 9 Self-Reflection",
        StartDate = DateTime.Now.AddDays(1),
        EndDate = DateTime.Now.AddDays(2),
        RunNumber = 0,
    };
    
    private readonly Assignment _assignment6 = new()
    {
        Name = "Sprint 10 Self-Reflection",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(1),
        RunNumber = 1,
    };

    public FillFormsComponentTest()
    {
        _userFormInstance1.Assignment = _assignment1;
        _userFormInstance2.Assignment = _assignment2;
        _userFormInstance3.Assignment = _assignment3;
        _userFormInstance4.Assignment = _assignment4;
        var userFormInstances = new List<UserFormInstance>
        {
            _userFormInstance1,
            _userFormInstance2,
            _userFormInstance3,
            _userFormInstance4
        };
        _teamFormInstance1.Assignment = _assignment5;
        _teamFormInstance2.Assignment = _assignment6;
        var teamFormInstances = new List<TeamFormInstance>
        {
            _teamFormInstance1,
            _teamFormInstance2
        };
        SetupFormInstanceService(userFormInstances, teamFormInstances);

        _mockProjectRepository.Setup(x =>
                x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(Project1);

        Services.AddScoped(_ => _mockFormInstanceService.Object);
        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
    }

    private void SetupFormInstanceService(IEnumerable<UserFormInstance> userFormInstances, IEnumerable<TeamFormInstance> teamFormInstances)
    {
        _mockFormInstanceService.Setup(x => x.GetUserFormsForUserInProject(It.IsAny<long>(), It.IsAny<long>()))
            .ReturnsAsync(userFormInstances);
        _mockFormInstanceService.Setup(x => x.GetTeamFormsForProject(It.IsAny<long>()))
            .ReturnsAsync(teamFormInstances);
    }

    private void CreateComponent(ProjectRole projectRole = ProjectRole.Developer)
    {
        var projectState = new ProjectState
        {
            ProjectRole = projectRole,
            ProjectId = Project1.Id
        };
        
        _component = RenderComponent<FillForms>(parameters => parameters
            .AddCascadingValue("Self", CurrentUser)
            .AddCascadingValue("ProjectState", projectState));
    }

    [Fact]
    public void PageRendered_AllFormsDisplayed()
    {
        CreateComponent();

        _component.FindAll(".form-instance-row").Count.Should().Be(6);
    }

    [Fact]
    public void PageRendered_PairUserForms_UserDetailsShown()
    {
        CreateComponent();
        var formInstances = new List<UserFormInstance>
        {
            _userFormInstance1,
            _userFormInstance2
        };
        SetupFormInstanceService(formInstances, new List<TeamFormInstance>());

        var userListItems = _component.FindComponents<UserListItem>();
        using (new AssertionScope())
        {
            userListItems.Count.Should().Be(1);
            userListItems[0].Instance.User.Should().Be(_userFormInstance2.Pair);
        }
    }
    
    [Fact]
    public void PageRendered_BeforeDueDate_DueDateNotHighlighted()
    {
        CreateComponent();

        var classList = _component.Find($"#form-instance-end-date-{_userFormInstance1.Id}").ClassList;
        using (new AssertionScope())
        {
            classList.Length.Should().Be(1);
            classList.Should().Contain("col");
        }
    }

    [Fact]
    public void PageRendered_AfterDueDate_DueDateHighlighted()
    {
        CreateComponent();
        
        var classList = _component.Find($"#form-instance-end-date-{_userFormInstance4.Id}").ClassList;
        using (new AssertionScope())
        {
            classList.Length.Should().Be(3);
            classList.Should().Contain("col");
            classList.Should().Contain("text-danger");
            classList.Should().Contain("fw-bold");
        }
    }
    
    [Fact]
    public void PageRendered_StartedFormsAreClickable()
    {
        CreateComponent();

        var instance2 = _component.Find($"#form-instance-{_userFormInstance2.Id}");
        instance2.Attributes["style"]?.TextContent.Should().NotContain("none");
        
        var instance3 = _component.Find($"#form-instance-{_userFormInstance3.Id}");
        instance3.Attributes["style"]?.TextContent.Should().NotContain("none");
        
        var instance4 = _component.Find($"#form-instance-{_userFormInstance4.Id}");
        instance4.Attributes["style"]?.TextContent.Should().NotContain("none");
    }

    [Fact]
    public void PageRendered_AllowSavingBeforeStartDateFalse_UpcomingFormIsNotClickable()
    {
        CreateComponent();
        
        var instance1 = _component.Find($"#form-instance-{_userFormInstance1.Id}");
        instance1.Attributes["style"]?.TextContent.Should().Contain("none");
    }

    [Fact]
    public void PageRendered_AllowSavingBeforeStartDateTrue_UpcomingFormIsClickable()
    {
        CreateComponent();
        _userFormInstance1.Assignment.AllowSavingBeforeStartDate = true;
        
        var instance1 = _component.Find($"#form-instance-{_userFormInstance1.Id}");
        instance1.Attributes["style"]?.TextContent.Should().Contain("none");
    }
    
    [Theory]
    [InlineData(ProjectRole.Reviewer, 4)]
    [InlineData(ProjectRole.Guest, 4)]
    [InlineData(ProjectRole.Leader, 6)]
    [InlineData(ProjectRole.Developer, 6)]
    public void PageRendered_OnlyDevelopersAndLeadersCanViewTeamForms(ProjectRole projectRole, int expectedNumberOfForms)
    {
        CreateComponent(projectRole);

        _component.FindAll(".form-instance-row").Count.Should().Be(expectedNumberOfForms);
    }

}