using System;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class ArchivedSprintViewComponentTest : BaseProjectScopedComponentTestContext<ArchivedSprintView>
    {
        
        private readonly Mock<IUserStoryRepository> _mockUserStoryRepository = new(MockBehavior.Strict);
        private readonly Mock<IUserStoryTaskRepository> _mockUserStoryTaskRepository = new(MockBehavior.Strict);
        private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new(MockBehavior.Strict);
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        
        private IRenderedComponent<ArchivedSprintView> _component;

        private readonly Sprint _sprint;
        private readonly Project _project;
        private readonly User _user;

        private const int TotalPointEstimate = 45;
        private readonly TimeSpan _totalTimeEstimate = TimeSpan.FromHours(3);

        public ArchivedSprintViewComponentTest()
        {
            _user = FakeDataGenerator.CreateFakeUser();
            _project = FakeDataGenerator.CreateFakeProject();
            _sprint = new()
            {
                Id = 13,
                Name = "Test sprint",
                Stage = SprintStage.ReadyToReview,
            };

            _mockUserStoryRepository
                .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
                .ReturnsAsync(TotalPointEstimate);
            
            _mockUserStoryTaskRepository
                .Setup(mock => mock.GetEstimateByStoryGroup(_sprint))
                .ReturnsAsync(_totalTimeEstimate);

            Services.AddScoped(_ => _mockUserStoryRepository.Object);
            Services.AddScoped(_ => _mockUserStoryTaskRepository.Object);
            Services.AddScoped(_ => _mockSprintChangelogRepository.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
        }
        
        private void CreateComponent()
        {
            _component = RenderComponent<ArchivedSprintView>(parameters => parameters
                .AddCascadingValue("ProjectState", new ProjectState { ProjectId = _project.Id })
                .AddCascadingValue("Self", _user)
                .Add(c => c.Sprint, _sprint));
        }

        [Fact]
        public void Rendered_WithSprint_ShowsSprintName()
        {
            CreateComponent();
            _component.Find(".accordion-header").TextContent.Should().Contain(_sprint.Name);
        }

        [Fact]
        public void Rendered_WithSprint_ShowsTotalTimeEstimate()
        {
            CreateComponent();
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetEstimateByStoryGroup(_sprint), Times.Once);
            _component.Find(".time-estimate").TextContent.Should()
                .Contain(DurationUtils.DurationStringFrom(_totalTimeEstimate));
        }
        
        [Fact]
        public void Rendered_WithSprint_ShowsTotalPointEstimate()
        {
            CreateComponent();
            _mockUserStoryTaskRepository
                .Verify(mock => mock.GetEstimateByStoryGroup(_sprint), Times.Once);
            _component.Find(".point-estimate").TextContent.Should()
                .Contain(TotalPointEstimate.ToString());
        }
    }
}
