using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared.FormAdministration;
using ScrumBoard.Shared.Widgets;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormAdministration;

public class ViewFormTemplateComponentTest : TestContext
{
    private IRenderedFragment _component;

    private readonly Mock<IFormTemplateRepository> _mockTemplateRepo =
        new Mock<IFormTemplateRepository>(MockBehavior.Strict);
    
    private Mock<IFormInstanceService> _mockFormInstanceService = new(MockBehavior.Strict);

    private static readonly Project Project1 = new Project { Id = 1 };
    private static readonly Project Project2 = new Project { Id = 2 };

    private static readonly Assignment Assignment1 = new Assignment
    {
        Name = "Sprint 1 Peer Feedback",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(3),
        RunNumber = 0,
        Instances = new List<FormInstance>()
    };

    private static readonly Assignment Assignment2 = new Assignment
    {
        Name = "Sprint 2 Peer Feedback",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(3),
        RunNumber = 1,
        Instances = new List<FormInstance>()
    };

    private static readonly Assignment Assignment3 = new Assignment
    {
        Name = "Sprint 3 Peer Feedback",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(3),
        RunNumber = 2,
        Instances = new List<FormInstance>()
    };

    private readonly FormTemplate _template = new FormTemplate
    {
        Id = 1,
        Name = "Peer Feedback",
        Assignments = new List<Assignment>
        {
            Assignment1,
            Assignment2,
            Assignment3
        }
    };


    private static readonly FormInstance Instance1 = new FormInstance
    {
        Assignment = Assignment1,
        Project = Project1,
        ProjectId = Project1.Id
    };

    private static readonly FormInstance Instance2 = new FormInstance
    {
        Assignment = Assignment2,
        Project = Project2,
        ProjectId = Project2.Id
    };

    private static readonly FormInstance Instance3 = new FormInstance
    {
        Assignment = Assignment3,
        Project = Project1,
        ProjectId = Project1.Id
    };

    private static readonly FormInstance Instance4 = new FormInstance
    {
        Assignment = Assignment1,
        Project = Project2,
        ProjectId = Project2.Id
    };

    private static readonly FormInstance Instance5 = new FormInstance
    {
        Assignment = Assignment1,
        Project = Project1,
        ProjectId = Project1.Id
    };

    public ViewFormTemplateComponentTest()
    {
        foreach (var instance in new List<FormInstance> { Instance1, Instance3, Instance5 })
        {
            instance.Project = Project1;
            instance.ProjectId = Project1.Id;
        }

        foreach (var instance in new List<FormInstance> { Instance2, Instance4 })
        {
            instance.Project = Project2;
            instance.ProjectId = Project2.Id;
        }

        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(_template.Assignments, 3, 1, 5));
        
        Services.AddScoped(_ => _mockFormInstanceService.Object);
        Services.AddScoped(_ => _mockTemplateRepo.Object);
    }

    private bool _assigned = false;

    private bool _previewed = false;

    private bool _edited = false;

    private void CreateComponent()
    {
        _assigned = false;
        _previewed = false;
        _edited = false;

        _component = RenderComponent<ViewFormTemplate>(parameters =>
            parameters
                .Add(x => x.Template, _template)
                .Add(x => x.OnAssigningFormTemplate, () => _assigned = true)
                .Add(x => x.OnPreview, () => _previewed = true)
                .Add(x => x.OnEditing, () => _edited = true)
        );
    }

    private List<Assignment> CreateManyAssignments()
    {
        return new List<Assignment>
        {
            new()
            {
                Name = "Sprint 1 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 0,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 2 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 1,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 3 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 2,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 4 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 3,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 5 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 4,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 1 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 5,
                Instances = new List<FormInstance>()
            },
        };
    }
    

    [Fact]
    public void ViewFormTemplate_UnusedTemplate_ShowsNotUsedMessage()
    {
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(new List<Assignment>(), 0, 1, 5));
        
        CreateComponent();
        _component.Markup.Should().Contain("This template has not yet been used");
    }

    [Fact]
    public void ViewFormTemplate_AtLeastOneAssignment_DisplaysRunInfoCorrectly()
    {
        _template.Assignments = new List<Assignment> { Assignment1 };
        CreateComponent();
        _component.Find(".run-number").InnerHtml.Should().Contain(Assignment1.RunNumber.ToString());
        _component.Find(".instance-name").InnerHtml.Should().Contain(Assignment1.Name);
        _component.Find(".start-date").InnerHtml.Should()
            .Contain(Assignment1.StartDate.ToString(CultureInfo.CurrentCulture));
        _component.Find(".end-date").InnerHtml.Should()
            .Contain(Assignment1.EndDate.ToString(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void ViewFormTemplate_SeveralSeparateRuns_DisplaysCardForEach()
    {
        var assignments = new List<Assignment> { Assignment2, Assignment3 };
        _template.Assignments = assignments;
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(_template.Assignments, 2, 1, 5));
        
        CreateComponent();
        _component.FindComponents<AssignedFormInstanceCard>().Count.Should().Be(assignments.Count);
    }

    [Fact]
    public void ViewFormTemplate_SomeRunsHaveMultipleInstances_DisplaysOneCardPerRun()
    {
        var assignments = new List<Assignment> { Assignment1, Assignment2, Assignment3 };
        _template.Assignments = assignments;
        CreateComponent();
        _component.FindComponents<AssignedFormInstanceCard>().Count.Should().Be(assignments.Count);
    }

    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[]
            {
                new List<Assignment> { new Assignment { Instances = new List<FormInstance> { Instance1 } } }, 1
            }, // One run, one project
            new object[]
            {
                new List<Assignment> { new Assignment { Instances = new List<FormInstance> { Instance1, Instance5 } } },
                1
            }, // One run, same project
            new object[]
            {
                new List<Assignment> { new Assignment { Instances = new List<FormInstance> { Instance1, Instance4 } } },
                2
            }, // One run, different projects
            new object[]
            {
                new List<Assignment>
                {
                    new Assignment { Instances = new List<FormInstance> { Instance1 } },
                    new Assignment { Instances = new List<FormInstance> { Instance2 } }
                },
                1
            }, // Two runs, different projects,
            // so first card only has one project
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void ViewFormTemplate_CreatingCard_PassesProjectListCorrectly(List<Assignment> assignments,
        int expectedNumProjects)
    {
        _template.Assignments = assignments;
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(_template.Assignments, _template.Assignments.Count, 1, 5));
        CreateComponent();
        _component.FindComponent<AssignedFormInstanceCard>().Instance.Projects.Count().Should().Be(expectedNumProjects);
    }

    [Fact]
    public void EditFormTemplate_TemplateAlreadySentOut_ButtonDisabled()
    {
        _template.RunNumber = 1;
        CreateComponent();
        _component.Find("#edit-button").Attributes.Count(x => x.LocalName == "disabled").Should().Be(1);
    }

    [Fact]
    public void ViewFormTemplate_AssigningToProject_CallsCallback()
    {
        CreateComponent();
        _component.Find("#assign-form-button").Click();
        _assigned.Should().BeTrue();
    }

    [Fact]
    public void ViewFormTemplate_Previewing_CallsCallback()
    {
        CreateComponent();
        _component.Find("#preview-button").Click();
        _previewed.Should().BeTrue();
    }

    [Fact]
    public void ViewFormTemplate_Editing_CallsCallback()
    {
        CreateComponent();
        _component.Find("#edit-button").Click();
        _edited.Should().BeTrue();
    }
    
    [Fact]
    public void ViewFormTemplate_OnlyOneAssignment_LoadMoreButtonHidden()
    {
        var newAssignments = new List<Assignment>
        {
            new()
            {
                Name = "Sprint 1 Peer Feedback",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3),
                RunNumber = 0,
                Instances = new List<FormInstance>()
            },
        };
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(newAssignments, 1, 1, 5));
        
        CreateComponent();
        
        var assignedFormInstanceCards = _component.FindComponents<AssignedFormInstanceCard>();
        assignedFormInstanceCards.Count.Should().Be(1);

        _component.FindAll("#load-more-button").Count.Should().Be(0);
    }
    
    [Fact]
    public void ViewFormTemplate_MoreThanOnePageOfAssignments_LoadMoreButtonDisplayed()
    {
        var newAssignments = CreateManyAssignments();
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(newAssignments.Take(5), newAssignments.Count, 1, 5));
        
        CreateComponent();
        
        var assignedFormInstanceCards = _component.FindComponents<AssignedFormInstanceCard>();
        assignedFormInstanceCards.Count.Should().Be(5);

        _component.FindAll("#load-more-button").Count.Should().Be(1);
    }
    
    [Fact]
    public void ViewFormTemplate_MoreThanOnePageOfAssignments_LoadMore_NextPageDisplayed()
    {
        var newAssignments = CreateManyAssignments();
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(newAssignments.Take(5), newAssignments.Count, 1, 5));
        
        CreateComponent();
        
        var assignedFormInstanceCards = _component.FindComponents<AssignedFormInstanceCard>();
        assignedFormInstanceCards.Count.Should().Be(5);
        
        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>( new List<Assignment>{ newAssignments.Last() }, newAssignments.Count, 2, 5));

        _component.Find("#load-more-button").Click();
        
        assignedFormInstanceCards = _component.FindComponents<AssignedFormInstanceCard>();
        assignedFormInstanceCards.Count.Should().Be(6);
    }
}