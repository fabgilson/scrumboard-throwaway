using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Report;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports;

public class ProjectStatisticsReportTest : TestContext
{
    private IRenderedComponent<ProjectStatistics> _component;
    private readonly User _actingUser;
    private readonly Project _currentProject;

    public ProjectStatisticsReportTest()
    {
        _actingUser = new User
        {
            Id = 13,
            FirstName = "Jimmy",
            LastName = "Neutron",
        };
        _currentProject = new Project
        {
            Id = 1, 
            Name = "Test project" 
        };

        Services.AddScoped(_ => new Mock<IUserRepository>().Object);
        Services.AddScoped(_ => new Mock<ISprintRepository>().Object);
        Services.AddScoped(_ => new Mock<IProjectRepository>().Object);
        Services.AddScoped(_ => new Mock<IProjectStatsService>().Object);
        Services.AddScoped(_ => new Mock<IUserStatsService>().Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
    }

    private void CreateComponent(ProjectRole role) {
        _component = RenderComponent<ProjectStatistics>(parameters => parameters              
            .AddCascadingValue("Self", _actingUser)
            .AddCascadingValue("ProjectState", new ProjectState{ProjectId = _currentProject.Id, ProjectRole = role})
        );
    }
    
    [Theory]
    [InlineData(ProjectRole.Guest)]
    [InlineData(ProjectRole.Reviewer)]
    [InlineData(ProjectRole.Developer)]
    private void Rendered_UsersRoleIsNotPermittedToViewReport_ErrorMessageShown(ProjectRole role)
    {
        CreateComponent(role);
        _component.FindAll("#project-statistics-report-forbidden-error-message").Count.Should().Be(1);
        _component.FindAll("#project-statistics-report-container").Count.Should().Be(0);
    }
    
    [Theory]
    [InlineData(ProjectRole.Leader)]
    private void Rendered_UsersRoleIsPermittedToViewReport_StatisticsReportShown(ProjectRole role)
    {
        CreateComponent(role);
        _component.FindAll("#project-statistics-report-forbidden-error-messagee").Count.Should().Be(0);
        _component.FindAll("#project-statistics-report-container").Count.Should().Be(1);
    }
}