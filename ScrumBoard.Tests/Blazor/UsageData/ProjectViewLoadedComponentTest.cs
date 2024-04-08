using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using ScrumBoard.Validators;
using Xunit;

namespace ScrumBoard.Tests.Blazor.UsageData
{
    public class ProjectViewLoadedComponentTest : TestContext
    {
        private readonly User _actingUser = new() { Id = 33, FirstName = "Jeff", LastName = "Jefferson" };

        private readonly ViewLoadedUsageEventType _type = ViewLoadedUsageEventType.Ceremonies;

        private readonly long _currentProjectId = 42;

        private readonly Mock<IUsageDataService> _usageDataServiceMock = new(MockBehavior.Strict);
        
        public ProjectViewLoadedComponentTest()
        {
            _usageDataServiceMock.Setup(m => m.AddUsageEvent(It.IsAny<ProjectViewLoadedUsageEvent>()));
            Services.AddScoped(_ => _usageDataServiceMock.Object);
        }

        private IRenderedComponent<ProjectViewLoaded> CreateComponent()
        {
            return RenderComponent<ProjectViewLoaded>(parameters => parameters
                .Add(c => c.Type, _type)
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue("ProjectState", new ProjectState{ProjectId = _currentProjectId})
            );
        }

        [Fact]
        public void Created_WithParams_GeneratesExpectedUsageEvent()
        {
            CreateComponent();
            var captor = new ArgumentCaptor<ProjectViewLoadedUsageEvent>();
            _usageDataServiceMock.Verify(mock => mock.AddUsageEvent(captor.Capture()));
            var usageEvent = captor.Value;
            usageEvent.ProjectId.Should().Be(_currentProjectId);
            usageEvent.UserId.Should().Be(_actingUser.Id);
            usageEvent.LoadedViewUsageEventType.Should().Be(_type);
            usageEvent.ResourceId.Should().BeNull();
        }
        
        [Fact]
        public void ReRendered_NoChanges_OnlyOneEventLogged()
        {
            var component = CreateComponent();
            component.Render();
            _usageDataServiceMock.Verify(mock => mock.AddUsageEvent(It.IsAny<BaseUsageDataEvent>()), Times.Once);
        }
    }
}