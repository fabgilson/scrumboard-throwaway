using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AngleSharp.Common;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Report;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports;

public class MyStatisticsReportTest : TestContext
{
    private IRenderedComponent<MyStatistics> _component;
    private readonly User _actingUser;
    private readonly User _someOtherUser;
    private readonly Project _currentProject;
    private readonly WorklogTag _reviewTag;

    private readonly Mock<IProjectRepository> _mockProjectRepository = new(); 
    private readonly Mock<IProjectStatsService> _projectStatsServiceMock = new();
    private readonly Mock<IUserStatsService> _userStatsServiceMock = new();
   
    public MyStatisticsReportTest()
    {
        _actingUser = new User
        {
            Id = 13,
            FirstName = "Jimmy",
            LastName = "Neutron",
        };
        _someOtherUser = new User
        {
            Id = 14,
            FirstName = "Jammy",
            LastName = "Notron",
        };
        _currentProject = new Project
        {
            Id = 1, 
            Name = "Test project" 
        };
        _reviewTag = FakeDataGenerator.CreateWorklogTag(name: "Review");
        
        _mockProjectRepository       
            .Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>())).ReturnsAsync(_currentProject);

        Services.AddScoped(_ => _projectStatsServiceMock.Object);
        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => _userStatsServiceMock.Object);
        
        Services.AddScoped(_ => new Mock<IUserRepository>().Object);
        Services.AddScoped(_ => new Mock<ISprintRepository>().Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object); 
        Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
        Services.AddScoped(_ => new Mock<IUserStoryRepository>().Object);
        Services.AddScoped(_ => new Mock<IOverheadEntryRepository>().Object);
        Services.AddScoped(_ => new Mock<IUserStoryTaskRepository>().Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        Services.AddScoped<IClock, SystemClock>();
    }

    private void CreateComponent(ProjectRole role, User userToGenerateStatisticsFor)
    {
        _component = RenderComponent<MyStatistics>(parameters => parameters              
            .AddCascadingValue("Self", _actingUser)
            .AddCascadingValue("ProjectState", new ProjectState{ProjectId = _currentProject.Id, ProjectRole = role})
            .Add(x => x.SelectedUser, userToGenerateStatisticsFor)
        );
    }

    [Theory]
    [InlineData(ProjectRole.Guest)]
    [InlineData(ProjectRole.Reviewer)]
    private void Rendered_UsersRoleIsNotPermittedToViewReport_ErrorMessageShown(ProjectRole role)
    {
        CreateComponent(role, _actingUser);
        _component.FindAll("#my-statistics-report-forbidden-error-message").Count.Should().Be(1);
        _component.FindAll("#my-statistics-report-container").Count.Should().Be(0);
    }
    
    [Theory]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Leader)]
    private void Rendered_UsersRoleIsPermittedToViewReport_StatisticsReportShown(ProjectRole role)
    {
        CreateComponent(role, _actingUser);
        _component.FindAll("#my-statistics-report-forbidden-error-message").Count.Should().Be(0);
        _component.FindAll("#my-statistics-report-container").Count.Should().Be(1);
    }
    
    [Fact]
    private void Rendered_DeveloperTryingToAccessReportThatIsNotTheirOwn_ErrorMessageShown()
    {
        CreateComponent(ProjectRole.Developer, _someOtherUser);
        _component.FindAll("#my-statistics-report-forbidden-error-message").Count.Should().Be(0);
        _component.FindAll("#my-statistics-report-viewing-other-user-report-error-message").Count.Should().Be(1);
        _component.FindAll("#my-statistics-report-container").Count.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    private void Rendered_DeveloperTryingToAccessReportThatIsTheirOwn_IsShownTheirOwnReport(bool selectedUserParamIsNull)
    {
        CreateComponent(ProjectRole.Developer, selectedUserParamIsNull ? null : _actingUser);
        _component.FindAll("#my-statistics-report-forbidden-error-message").Count.Should().Be(0);
        _component.FindAll("#my-statistics-report-viewing-other-user-report-error-message").Count.Should().Be(0);
        _component.FindAll("#my-statistics-report-container").Count.Should().Be(1);
    }
    
    [Fact]
    private void Rendered_UserHasNoReviewedTasks_TasksReviewedShowsZero()
    {
        _userStatsServiceMock
            .Setup(x => x.GetStatCardData(It.IsAny<User>(), It.IsAny<Project>(), It.IsAny<Sprint>()))
            .ReturnsAsync((User _, Project _, bool isSprint) => new[] { new TasksReviewed(0, 0, isSprint) });

        CreateComponent(ProjectRole.Developer, _actingUser);
        
        var card = _component.FindComponents<StatCard>()
            .First(c => c.Find("#stat-card-title").Text().Equals("Tasks Reviewed"));
    
        card.Find("#stat-card-text").Children.GetItemByIndex(1).Text().Should().Be("0");
    }
    
    [Fact]
    private void Rendered_UserHasOneReviewedTask_TasksReviewedShowsOne() 
    {
        _userStatsServiceMock
            .Setup(x => x.GetStatCardData(It.IsAny<User>(), It.IsAny<Project>(), It.IsAny<Sprint>()))
            .ReturnsAsync((User _, Project _, bool isSprint) => new[] { new TasksReviewed(1, 1, isSprint) });
    
        CreateComponent(ProjectRole.Developer, _actingUser);
        
        var card = _component.FindComponents<StatCard>()
            .First(c => c.Find("#stat-card-title").Text().Equals("Tasks Reviewed"));
    
        card.Find("#stat-card-text").Children.GetItemByIndex(1).Text().Should().Be("1");
    }
}