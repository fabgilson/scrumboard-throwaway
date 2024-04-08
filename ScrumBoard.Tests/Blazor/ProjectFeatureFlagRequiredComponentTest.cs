using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.ProjectFeatureFlags;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class ProjectFeatureFlagRequiredComponentTest : TestContext
{
    private IRenderedComponent<ProjectFeatureFlagRequiredComponent> _component;
    
    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectFeatureFlagService> _mockProjectFeatureFlagService = new(MockBehavior.Strict);
    
    private readonly Project _projectWithStandUpMeetingScheduleFeatureFlagSet;
    private readonly Project _projectWithoutAnyFeatureFlagSet;

    public ProjectFeatureFlagRequiredComponentTest()
    {
        _projectWithStandUpMeetingScheduleFeatureFlagSet = new Project
        {
            Id = 100,
            Name = "Feature flag enabled project"
        };
        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(It.Is<long>(id => id == _projectWithStandUpMeetingScheduleFeatureFlagSet.Id)))
            .ReturnsAsync(_projectWithStandUpMeetingScheduleFeatureFlagSet);
        _mockProjectFeatureFlagService
            .Setup(x => x.ProjectHasFeatureFlagAsync(
                It.Is<Project>(p => p.Id == _projectWithStandUpMeetingScheduleFeatureFlagSet.Id), 
                It.Is<FeatureFlagDefinition>(ff => ff == FeatureFlagDefinition.StandUpMeetingSchedule))
            )
            .ReturnsAsync(true);
        
        _projectWithoutAnyFeatureFlagSet = new Project
        {
            Id = 200,
            Name = "Feature flag not enabled project"
        };
        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(It.Is<long>(id => id == _projectWithoutAnyFeatureFlagSet.Id)))
            .ReturnsAsync(_projectWithoutAnyFeatureFlagSet);
        _mockProjectFeatureFlagService
            .Setup(x => x.ProjectHasFeatureFlagAsync(
                It.Is<Project>(p => p.Id == _projectWithoutAnyFeatureFlagSet.Id), 
                It.IsAny<FeatureFlagDefinition>())
            )
            .ReturnsAsync(false);
        
        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => _mockProjectFeatureFlagService.Object);
    }

    private void CreateComponent(FeatureFlagDefinition requiredFeatureFlag, Project project)
    {
        _component = RenderComponent<ProjectFeatureFlagRequiredComponent>(parameters => parameters
            .Add(cut => cut.RequiredFeatureFlag, requiredFeatureFlag)
            .AddCascadingValue("ProjectState", new ProjectState {ProjectId = project.Id, Project = project})
            .AddChildContent("<div id=\"child-content\">Hello world!</div>")
        );
    }

    [Fact]
    private void RendersComponentRequiringStandUpMeetingScheduleProjectFeatureFlag_FlagIsEnabled_RendersChild()
    {
        CreateComponent(FeatureFlagDefinition.StandUpMeetingSchedule, _projectWithStandUpMeetingScheduleFeatureFlagSet);
        _component.FindAll("#child-content").Should().ContainSingle();
        _component.Find("#child-content").TextContent.Should().Be("Hello world!");
    }
    
    [Fact]
    private void RendersComponentRequiringStandUpMeetingScheduleProjectFeatureFlag_FlagIsNotEnabled_DoesNotRenderChild()
    {
        CreateComponent(FeatureFlagDefinition.StandUpMeetingSchedule, _projectWithoutAnyFeatureFlagSet);
        _component.FindAll("#child-content").Should().BeEmpty();
    }
}