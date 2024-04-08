using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Widgets;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class ProjectMemberListViewTest : TestContext
{
    private IRenderedComponent<ProjectMemberListView> _projectMemberListView;

    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    
    private static readonly Project Project = new()
    {
        Name = "Team X",
        Id = 789,
        MemberAssociations = new List<ProjectUserMembership>()
        {
            new()
            {
                User = new User { Id = 50, FirstName = "Timmy", LastName = "Developer" }, 
                Role = ProjectRole.Developer
            },
            new()
            {
                User = new User { Id = 51, FirstName = "Tiffany", LastName = "Leader" }, 
                Role = ProjectRole.Leader
            }
        }
    };
    
    private IElement Checkbox => _projectMemberListView.Find("#dev-filter-checkbox");
    private IReadOnlyList<IRenderedComponent<UserListItem>> FoundUserItems => _projectMemberListView.FindComponents<UserListItem>();

    public ProjectMemberListViewTest()
    {
        _projectRepositoryMock.Setup(x => x.GetByIdAsync(
            It.IsAny<long>(), 
            It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>())
        ).ReturnsAsync(Project);
 
        Services.AddScoped(_ => _projectRepositoryMock.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
    }

    private void CreateComponent(bool isChecked = true)
    {
        _projectMemberListView = RenderComponent<ProjectMemberListView>(parameters => parameters
            .AddCascadingValue("ProjectState",
                new ProjectState { ProjectId = Project.Id, ProjectRole = ProjectRole.Developer })
            .AddCascadingValue("Self", new User { Id = 1 }));
        
        Checkbox.Change(isChecked);
        _projectMemberListView.WaitForElement("#user-list-item");
    }

    [Fact]
    private void Rendered_InitialLoad_CheckboxIsChecked()
    {
        CreateComponent();
        Checkbox.Attributes.Any(x => x.Name == "checked").Should().BeTrue();
    }

    [Fact]
    private void Rendered_CheckboxIsChecked_OnlyDevelopersAreShown()
    {
        CreateComponent();
        FoundUserItems.Should().HaveCount(1);
        FoundUserItems[0].Instance.User.Id.Should().Be(50);
    }


    [Fact]
    private void Rendered_CheckboxIsNotChecked_EveryoneIsShown()
    {
        CreateComponent(false);
        FoundUserItems.Should().HaveCount(2);
    }
}