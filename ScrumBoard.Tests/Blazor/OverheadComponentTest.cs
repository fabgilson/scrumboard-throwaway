using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Util;
using SharedLensResources.Blazor.Util;
using Xunit;
using FakeNavigationManager = Bunit.TestDoubles.FakeNavigationManager;

namespace ScrumBoard.Tests.Blazor
{
    public class OverheadComponentTest : TestContext
    {
        private readonly int _pageSize = 25;
        
        private readonly User _actingUser = new()
        {
            Id = 33,
            FirstName = "Jeff",
            LastName = "Geoff"
        };
        
        private readonly User _otherUser = new()
        {
            Id = 34,
            FirstName = "Jimmy",
            LastName = "Neutron"
        };

        private readonly OverheadSession _planning1 = new() {Id = 403, Name = "Planning 1"}; 
        private readonly OverheadSession _planning2 = new() {Id = 404, Name = "Planning 2"}; 
        
        private readonly TimeSpan _totalTimeLogged = TimeSpan.FromHours(4.5);

        private IRenderedComponent<Overhead> _component;

        private readonly Project _project;

        private readonly Sprint _sprint;

        private readonly PaginatedList<OverheadEntry> _overheadEntries;

        // Mocks
        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);

        private readonly Mock<IOverheadEntryRepository> _mockOverheadEntryRepository = new(MockBehavior.Strict);
        
        private readonly Mock<IOverheadSessionRepository> _mockOverheadSessionRepository = new(MockBehavior.Strict);

        private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);

        public OverheadComponentTest()
        {
            _project = new()
            {
                Id = 43
            };

            _project.MemberAssociations = new List<ProjectUserMembership>
            {
                new()
                {
                    ProjectId = _project.Id,
                    UserId = _actingUser.Id,
                    Project = _project, 
                    User = _actingUser,
                    Role = ProjectRole.Developer,
                },
                new()
                {
                    ProjectId = _project.Id,
                    UserId = _otherUser.Id,
                    Project = _project, 
                    User = _otherUser,
                    Role = ProjectRole.Developer,
                },
            };
            
            _sprint = new()
            {
                Project = _project, 
                Stage = SprintStage.Started, 
                TimeStarted = DateTime.Now
            };
            _project.Sprints.Add(_sprint);

            _overheadEntries = new(new List<OverheadEntry>(), 10, 1, 25);

            _mockProjectRepository
                .Setup(x => x.GetByIdAsync(_project.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
                .ReturnsAsync(_project);

            _mockOverheadEntryRepository
                .Setup(mock =>
                    mock.GetAllPaginatedAsync(
                        1, 
                        _pageSize, 
                        OverheadEntryIncludes.Session, 
                        OverheadEntryIncludes.User, 
                        It.IsAny<Func<IQueryable<OverheadEntry>,IQueryable<OverheadEntry>>>(), 
                        It.IsAny<Func<IQueryable<OverheadEntry>,IQueryable<OverheadEntry>>>(), 
                        It.IsAny<Func<IQueryable<OverheadEntry>,IQueryable<OverheadEntry>>>())
                    )
                .ReturnsAsync(_overheadEntries);

            _mockOverheadEntryRepository
                .Setup(mock => mock.GetTotalTimeLogged(
                    It.IsAny<Func<IQueryable<OverheadEntry>, IQueryable<OverheadEntry>>[]>()
                ))
                .ReturnsAsync(_totalTimeLogged);

            _mockOverheadSessionRepository
                .Setup(mock => mock.GetAllAsync(It.IsAny<Func<IQueryable<OverheadSession>,IQueryable<OverheadSession>>>()))
                .ReturnsAsync(new List<OverheadSession>() {_planning1, _planning2});

            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockOverheadEntryRepository.Object);
            Services.AddScoped(_ => _mockOverheadSessionRepository.Object);
            Services.AddScoped<NavigationManager, FakeNavigationManager>();
            Services.AddScoped(_ => _mockStateStorageService.Object);
            Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

            ComponentFactories.AddDummyFactoryFor<EditOverheadEntry>();
            ComponentFactories.AddDummyFactoryFor<OverheadEntryListItem>();
            ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
        }

        private void CreateComponent(ProjectRole role = ProjectRole.Developer)
        {
            _component = RenderComponent<Overhead>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue("ProjectState", new ProjectState{ ProjectId = _project.Id, ProjectRole = role}));
        }

        [Fact]
        public void Rendered_ProjectWithoutSprints_NoSprintMessageShown()
        {
            _project.Sprints.Clear();
            
            CreateComponent();

            _component.Find(".container")
                .TextContent
                .Should()
                .Contain("This project does not have a sprint to log formal event time against");
        }

        [Theory]
        [InlineData(null)]
        [InlineData(ProjectRole.Reviewer)]
        public void Rendered_InvalidProjectRole_NavigatesAwayFromPage(ProjectRole? role)
        {
            var navigation = Services.GetRequiredService<NavigationManager>();
            var relativePath = PageRoutes.ToProjectCeremonies(_project.Id);
            var trimmedRelativePath = relativePath.TrimStart('.').TrimStart('/');
            
            navigation.NavigateTo(relativePath);
            navigation.ToBaseRelativePath(navigation.Uri).Should().Be(trimmedRelativePath);
            
            if (role == null)
            {
                _project.MemberAssociations.Clear();
                CreateComponent();
            }
            else
            {
                _project.MemberAssociations.Single(membership => membership.UserId == _actingUser.Id).Role = role.Value;
                CreateComponent(role.Value);
            }

            navigation.ToBaseRelativePath(navigation.Uri).Should().NotBe(trimmedRelativePath);
        }
        
        [Theory]
        [InlineData(ProjectRole.Guest)]
        [InlineData(ProjectRole.Developer)]
        [InlineData(ProjectRole.Leader)]
        public void Rendered_ValidProjectRole_DoesNotNavigatesAwayFromPage(ProjectRole role)
        {
            _project.MemberAssociations.Single(membership => membership.UserId == _actingUser.Id).Role = role;
            
            var navigation = Services.GetRequiredService<NavigationManager>();
            var relativePath = PageRoutes.ToProjectCeremonies(_project.Id);
            var trimmedRelativePath = relativePath.TrimStart('.').TrimStart('/');
            navigation.NavigateTo(relativePath);
            navigation.ToBaseRelativePath(navigation.Uri).Should().Be(trimmedRelativePath);
            
            CreateComponent(role);

            navigation.ToBaseRelativePath(navigation.Uri).Should().Be(trimmedRelativePath);
        }

        [Theory]
        [InlineData(ProjectRole.Guest)]
        public void Rendered_ReadOnlyRole_CannotStartCreatingNewOverheadEntry(ProjectRole role)
        {
            CreateComponent(role);
            _component.FindAll("#start-logging-overhead").Should().BeEmpty();
        }

        [Theory]
        [InlineData(ProjectRole.Developer)]
        [InlineData(ProjectRole.Leader)]
        public void Rendered_RoleWithWritePermissions_CanStartCreatingNewOverheadEntry(ProjectRole role)
        {
            CreateComponent(role);
            _component.FindAll("#start-logging-overhead").Should().ContainSingle();
        }

        [Fact]
        public void Rendered_WithOverheadEntries_OverheadEntriesShown()
        {
            var first = new OverheadEntry()
            {
                Id = 10,
                Description = "test",
            };
            var second = new OverheadEntry()
            {
                Id = 11,
                Description = "second",
            };
            _overheadEntries.Add(first);
            _overheadEntries.Add(second);
            
            CreateComponent();

            var entries = _component.FindComponents<Dummy<OverheadEntryListItem>>().ToList();
            entries.Should().HaveCount(2);
            entries.First().Instance.GetParam(c => c.Entry).Should().Be(first);
            entries.Last().Instance.GetParam(c => c.Entry).Should().Be(second);
        }
        
        [Fact]
        public void Rendered_NoOverheadEntries_NoOverheadLoggedMessageShown()
        {
            _overheadEntries.Clear();
            
            CreateComponent();

            _component.Find(".list-group-item").TextContent.Trim().Should()
                .Be("No formal events have been logged for this sprint");
        }
        
        [Fact]
        public async Task Rendered_EditOverheadClicked_StartedEditingOverheadEntry()
        {
            var first = new OverheadEntry()
            {
                Id = 10,
                Description = "test",
            };
            _overheadEntries.Add(first);
            
            CreateComponent();

            _component.FindComponents<Dummy<EditOverheadEntry>>().Should().BeEmpty();
            
            await _component.InvokeAsync(() => _component.FindComponent<Dummy<OverheadEntryListItem>>()
                .Instance
                .GetParam(c => c.EditOverhead)
                .InvokeAsync(first));
            
            _component.FindComponents<Dummy<OverheadEntryListItem>>().Should().BeEmpty();
            _component.FindComponents<Dummy<EditOverheadEntry>>().Should().ContainSingle();
            
            var editOverhead = _component.FindComponents<Dummy<EditOverheadEntry>>().Should().ContainSingle().Which;
            editOverhead.Instance.GetParam(c => c.Entry).Should().Be(first);
        }
        
        [Fact]
        public async Task Rendered_EditOverheadCancelled_StoppedEditingOverheadEntry()
        {
            var first = new OverheadEntry()
            {
                Id = 10,
                Description = "test",
            };
            _overheadEntries.Add(first);
            
            CreateComponent();

            // Start editing
            await _component.InvokeAsync(() => _component.FindComponent<Dummy<OverheadEntryListItem>>()
                .Instance
                .GetParam(c => c.EditOverhead)
                .InvokeAsync(first));
            
            // Stop editing
            await _component.InvokeAsync(() => _component.FindComponent<Dummy<EditOverheadEntry>>()
                .Instance
                .GetParam(c => c.OnClose)
                .InvokeAsync());
            
            _component.FindComponents<Dummy<EditOverheadEntry>>().Should().BeEmpty();
            _component.FindComponents<Dummy<OverheadEntryListItem>>().Should().ContainSingle();
        }

        [Fact]
        public void Rendered_LogOverheadClicked_StartedAddingOverheadEntry()
        {
            CreateComponent();

            _component.FindComponents<Dummy<EditOverheadEntry>>().Should().BeEmpty();
            
            _component.Find("#start-logging-overhead").Click();
            
            var editOverhead = _component.FindComponents<Dummy<EditOverheadEntry>>().Should().ContainSingle().Which;
            editOverhead.Instance.GetParam(c => c.Entry).Sprint.Should().Be(_sprint);
        }
        
        [Fact]
        public async Task Rendered_LogOverheadCancelled_CancelledAddingOverheadEntry()
        {
            CreateComponent();

            _component.Find("#start-logging-overhead").Click();
            await _component.InvokeAsync(() => _component.FindComponent<Dummy<EditOverheadEntry>>().Instance.GetParam(c => c.OnClose).InvokeAsync());
            _component.FindComponents<Dummy<EditOverheadEntry>>().Should().BeEmpty();
        }
        
        [Fact]
        public void Rendered_SelectDifferentSprint_LogOverheadButtonHidden()
        {
            var testSprint = new Sprint()
            {
                Id = 60,
                Name = "Other sprint",
                Stage = SprintStage.Closed,
            };
            _project.Sprints.Add(testSprint);
            
            CreateComponent();

            _component.FindAll("#start-logging-overhead").Should().ContainSingle();
            
            _component.Find($"#sprint-select-{testSprint.Id}").Click();
            
            _component.FindAll("#start-logging-overhead").Should().BeEmpty();
        }
        
        [Fact]
        public void Rendered_SelectWholeProject_LogOverheadButtonHidden()
        {
            CreateComponent();

            _component.FindAll("#start-logging-overhead").Should().ContainSingle();

            _component.Find($"#select-whole-project").Click();
            
            _component.FindAll("#start-logging-overhead").Should().BeEmpty();
        }
        
        [Fact]
        public void Rendered_SelectDifferentSprint_DifferentSprintOverheadEntriesQueried()
        {
            var testSprint = new Sprint()
            {
                Id = 60,
                Name = "Other sprint",
                Stage = SprintStage.Closed,
            };
            _project.Sprints.Add(testSprint);
            
            CreateComponent();

            _component.Find($"#sprint-select-{testSprint.Id}").Click();
        }
        
        [Fact]
        public void Rendered_SelectWholeProject_ProjectOverheadEntriesQueried()
        {
            CreateComponent();
            _component.Find($"#select-whole-project").Click();
        }

        [Fact]
        public void Rendered_AddSessionToFilterBy_PageRefreshed()
        {
            CreateComponent();

            _component.Find($"#tag-select-{_planning1.Id}").Click();
            
            _mockOverheadEntryRepository
                .Verify(mock =>
                    mock.GetAllPaginatedAsync(1, _pageSize,
                        It.IsAny<Func<IQueryable<OverheadEntry>, IQueryable<OverheadEntry>>[]>()));
            _mockOverheadEntryRepository
                .Verify(mock => mock.GetTotalTimeLogged(It.IsAny<Func<IQueryable<OverheadEntry>,IQueryable<OverheadEntry>>[]>()));
        }
        
        [Fact]
        public void Rendered_AddUserToFilterBy_PageRefreshed()
        {
            CreateComponent();

            _component.Find($"#filter-user-select-{_otherUser.Id}").Click();

            _mockOverheadEntryRepository
                .Verify(mock =>
                    mock.GetAllPaginatedAsync(1, _pageSize,
                        It.IsAny<Func<IQueryable<OverheadEntry>, IQueryable<OverheadEntry>>[]>()));
            _mockOverheadEntryRepository
                .Verify(mock => mock.GetTotalTimeLogged(It.IsAny<Func<IQueryable<OverheadEntry>,IQueryable<OverheadEntry>>[]>()));
        }
    }
}
