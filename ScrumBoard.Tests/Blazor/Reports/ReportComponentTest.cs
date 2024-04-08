using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Diffing.Extensions;
using Bunit;
using Bunit.Extensions.WaitForHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Report;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;
using FakeNavigationManager = Bunit.TestDoubles.FakeNavigationManager;

namespace ScrumBoard.Tests.Blazor.Reports;

public class ReportComponentTest : TestContext
{
    private IRenderedComponent<Report> _component;
        
    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict); 
    private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);       
    private readonly Mock<IBurndownService> _mockBurnDownService = new(MockBehavior.Strict);
    private readonly Mock<MyStatistics> _mockMyStatistics = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IUserStatsService> _mockUserStatsService = new();
    private readonly Mock<ISprintRepository> _mockSprintRepository = new();
    private readonly Mock<IConfigurationService> _mockConfigurationService = new();
    private readonly Mock<IStandUpMeetingService> _mockStandUpMeetingService = new();
    private readonly Mock<IProjectFeatureFlagService> _mockProjectFeatureFlagService = new();
    private readonly Mock<IUsageDataService> _mockUsageDataService = new();
    private readonly Mock<IJsInteropService> _jsInteropServiceMock = new();
    private readonly Mock<IWeeklyReflectionCheckInService> _mockWeeklyReflectionCheckInService = new();
    private readonly Mock<IClock> _mockClock = new();

    private readonly Project _currentProject;
    private readonly User _actingUser;
    private readonly User _anotherUser;
    private readonly ProjectUserMembership _membership;
    
    public ReportComponentTest()
    {
        _actingUser = new User
        {
            Id = 13,
            FirstName = "Jimmy",
            LastName = "Neutron",
            LDAPUsername = "jmn123"
        };
        _anotherUser = new User
        {
            Id = 14,
            FirstName = "James",
            LastName = "Bond",
        };
        _currentProject = new Project
        {
            Id = 1, 
            Name = "Test project" 
        };
        _membership = new ProjectUserMembership
        { 
            UserId = _actingUser.Id, 
            ProjectId = _currentProject.Id,
            Project = _currentProject,
            User = _actingUser,
            Role = ProjectRole.Developer
        };
        ProjectUserMembership anotherMembership = new() { 
            UserId = _anotherUser.Id, 
            ProjectId = _currentProject.Id,
            Project = _currentProject,
            User = _anotherUser,
            Role = ProjectRole.Developer
        };
        _currentProject.MemberAssociations.AddRange(new [] {_membership, anotherMembership });
        _actingUser.ProjectAssociations.Add(_membership);
        _anotherUser.ProjectAssociations.Add(anotherMembership);
            
        _mockProjectRepository       
            .Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>())).ReturnsAsync(_currentProject);  
        _mockStateStorageService
            .Setup(x => x.GetSelectedProjectIdAsync()).ReturnsAsync(_currentProject.Id);

        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => _mockStateStorageService.Object);
        Services.AddScoped(_ => _mockBurnDownService.Object);
        Services.AddScoped(_ => _mockUserRepository.Object);
        Services.AddScoped(_ => _mockUserStatsService.Object);
        Services.AddScoped(_ => _mockSprintRepository.Object);
        Services.AddScoped(_ => _mockConfigurationService.Object);
        Services.AddScoped(_ => _mockStandUpMeetingService.Object);
        Services.AddScoped(_ => _mockProjectFeatureFlagService.Object);
        Services.AddScoped(_ => _mockUsageDataService.Object);
        Services.AddScoped(_ => _jsInteropServiceMock.Object);
        Services.AddScoped(_ => _mockWeeklyReflectionCheckInService.Object);
        Services.AddScoped(_ => _mockClock.Object);
            
        ComponentFactories.AddDummyFactoryFor<BurndownReport>();
        ComponentFactories.AddDummyFactoryFor<WorklogReport>();
        ComponentFactories.AddDummyFactoryFor<ProjectStatistics>();
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
        ComponentFactories.AddMockComponent(_mockMyStatistics);
    }

    private void CreateComponent(
        ProjectRole role = ProjectRole.Developer, 
        ReportType? reportType = ReportType.MyStatistics, 
        bool areStandUpCheckInsEnabledForProject = false,
        bool isStandUpCheckInReportEnabledForProject = false
    ) {
        _mockProjectFeatureFlagService
            .Setup(x => x.ProjectHasFeatureFlagAsync(It.IsAny<Project>(), FeatureFlagDefinition.WeeklyReflectionCheckIn))
            .ReturnsAsync(areStandUpCheckInsEnabledForProject);
        _mockProjectFeatureFlagService
            .Setup(x => x.ProjectHasFeatureFlagAsync(It.IsAny<Project>(), FeatureFlagDefinition.WeeklyReflectionCheckInReportPage))
            .ReturnsAsync(isStandUpCheckInReportEnabledForProject);
        Services.AddScoped(_ => _mockConfigurationService.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
        _component = RenderComponent<Report>(parameters => parameters              
            .AddCascadingValue("Self", _actingUser)
            .AddCascadingValue("ProjectState", new ProjectState{ProjectId = _currentProject.Id, ProjectRole = role, Project = _currentProject})
            .Add(x => x.ReportTypeParam, (int?)reportType)
        );
    }

    [Theory]
    [InlineData(ReportType.BurnDown)]
    [InlineData(ReportType.WorkLog)]
    [InlineData(ReportType.ProjectStatistics)]
    public void Rendered_NotViewingMyStatistics_UserSelectorNotShown(ReportType reportType)
    {
        _membership.Role = ProjectRole.Leader;
        CreateComponent(reportType: reportType);
        _component.FindAll("#report-user-select-button").Should().BeEmpty();
    }

    [Fact]
    public void Rendered_IsLeaderAndViewingMyStatistics_UserSelectorShown()
    {
        _membership.Role = ProjectRole.Leader;
        CreateComponent(ProjectRole.Leader);
        _component.WaitForState(() => _component.FindAll("#report-user-select-button").Any(), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Rendered_IsNotLeaderAndViewingMyStatistics_UserSelectorNotShownOnMyStatisticsView()
    {
        _membership.Role = ProjectRole.Developer;
        CreateComponent(reportType: ReportType.MyStatistics);
        _component.FindAll("#report-user-select-button").Should().BeEmpty();
    }

    [Fact]
    public void Rendered_IsNotAProjectLeader_UserSelectorNotShownOnCheckInsView()
    {
        CreateComponent(reportType: ReportType.MyWeeklyReflections);
        _component.FindAll("#report-user-select-button").Should().BeEmpty();
    }
    
    [Fact]
    public void Rendered_IsAProjectLeader_UserSelectorIsShownOnCheckInsView()
    {
        CreateComponent(ProjectRole.Leader, reportType: ReportType.MyWeeklyReflections);
        _component.FindAll("#report-user-select-button").Should().ContainSingle();
    }

    [Fact]
    public void StandUpCheckInsReportRendered_StandUpCheckInsFlagNotSet_NoContentShown()
    {
        CreateComponent(reportType: ReportType.MyWeeklyReflections, areStandUpCheckInsEnabledForProject: false);
        var findContainerAction = () => _component.WaitForElement("#weekly-check-in-report-container");
        findContainerAction.Should().Throw<WaitForFailedException>();
    }
    
    [Fact]
    public void StandUpCheckInsReportRendered_StandUpCheckInsFlagIsSetButCheckInsReportFlagIsNot_NoContentShown()
    {
        CreateComponent(
            reportType: ReportType.MyWeeklyReflections, 
            areStandUpCheckInsEnabledForProject: true, 
            isStandUpCheckInReportEnabledForProject: false
        );
        var findContainerAction = () => _component.WaitForElement("#weekly-check-in-report-container");
        findContainerAction.Should().Throw<WaitForFailedException>();
    }
    
    [Fact]
    public void StandUpCheckInsReportRendered_StandUpCheckInsFlagAndCheckInsReportFlagAreSet_ContentContainerExists()
    {
        CreateComponent(
            reportType: ReportType.MyWeeklyReflections, 
            areStandUpCheckInsEnabledForProject: true, 
            isStandUpCheckInReportEnabledForProject: true
        );
        var findContainerAction = () => _component.WaitForElement("#weekly-check-in-report-container");
        findContainerAction.Should().NotThrow();
    }

    [Fact]
    public void Rendered_UserSelectorShown_SelectUser_ChangesToNewUser()
    {
        _membership.Role = ProjectRole.Leader;
        CreateComponent(ProjectRole.Leader);
        _component.Find($"#select-report-type-MyStatistics").Click();
        _component.WaitForState(() => _component.FindAll("#report-user-select-button").Any(), TimeSpan.FromSeconds(2));
        _component.Find($"#select-report-user-{_anotherUser.Id}").Click();
        _component.WaitForState(() => _component.Find("#current-selected-user").TextContent.Contains(_anotherUser.GetFullName()), TimeSpan.FromSeconds(2));
    }
    
    private List<ReportType> GetListedReportTypesFromMenu()
    {
        var reportOptions = _component.FindAll("[id^=select-report-type]");
        var foundEnums = reportOptions
            .Select(x => x.Id!.Replace("select-report-type-", ""))
            .Select(Enum.Parse<ReportType>) // Cast report type param from string value to enum
            .ToList();
        return foundEnums;
    }
    
    [Theory]
    [InlineData(ProjectRole.Guest, false)]
    [InlineData(ProjectRole.Developer, false)]
    [InlineData(ProjectRole.Reviewer, false)]
    [InlineData(ProjectRole.Leader, false)]
    [InlineData(ProjectRole.Guest, true)]
    [InlineData(ProjectRole.Developer, true)]
    [InlineData(ProjectRole.Reviewer, true)]
    [InlineData(ProjectRole.Leader, true)]
    public void Rendered_MultipleRoles_UserSeesCorrectReportTypesBasedOnRole(ProjectRole role, bool isCheckInsReportEnabled)
    {
        _membership.Role = role;
        CreateComponent(role, isStandUpCheckInReportEnabledForProject: isCheckInsReportEnabled);
        var foundReportTypes = GetListedReportTypesFromMenu();
        var expectedReportTypes = ReportTypeUtils.GetAllowedReportTypesForRole(role).ToList();
        if (!isCheckInsReportEnabled) expectedReportTypes.Remove(ReportType.MyWeeklyReflections);
        using (new AssertionScope())
        {
            foundReportTypes.Should().ContainInOrder(expectedReportTypes);
            foundReportTypes.Should().HaveCount(expectedReportTypes.Count);
        } 
    }
        
    [Theory]
    [InlineData(ProjectRole.Guest, ReportType.WorkLog)]
    [InlineData(ProjectRole.Developer, ReportType.MyStatistics)]
    [InlineData(ProjectRole.Reviewer, ReportType.WorkLog)]
    [InlineData(ProjectRole.Leader, ReportType.ProjectStatistics)]
    public void Rendered_NoReportTypeSpecified_TakenToDefaultReportTypeForRole(ProjectRole role, ReportType expectedReportType)
    {
        _membership.Role = role;
        CreateComponent(role, reportType: null);
        var fakeNavManager = Services.GetRequiredService<FakeNavigationManager>();
        var foundReportTypeIntValue = Convert.ToInt32(fakeNavManager.Uri.Split('/').Last());
        ((ReportType)foundReportTypeIntValue).Should().Be(expectedReportType);
    }
}