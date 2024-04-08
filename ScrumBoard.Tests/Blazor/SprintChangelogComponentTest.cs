

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Events;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Messages;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Widgets.Messages;
using ScrumBoard.Utils;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class SprintChangelogComponentTest : TestContext
    {
        private IRenderedComponent<SprintChangelog> _component;
        private readonly Sprint _sprint;

        private readonly Mock<IJsInteropService> _mockJSInteropService = new(MockBehavior.Strict);

        private readonly Mock<ISprintChangelogRepository> _mockSprintChangelogRepository = new(MockBehavior.Strict);

        public SprintChangelogComponentTest()
        {
            _sprint = new Sprint() { 
                Id = 32, 
                Created = DateTime.Now, 
                Creator = new User() { FirstName = "Test", LastName = "User" } 
            };
        }
        
        private void CreateComponent(List<SprintChangelogEntry> changes) {
            _mockSprintChangelogRepository
                .Setup(x => x.GetBySprintAsync(_sprint, SprintChangelogIncludes.Creator, SprintChangelogIncludes.UserStory))
                .ReturnsAsync(changes);
            Services.AddScoped(_ => _mockSprintChangelogRepository.Object);
            Services.AddScoped(_ => _mockJSInteropService.Object);

            _component = RenderComponent<SprintChangelog>(parameters => parameters
                .Add(p => p.Sprint, _sprint)
            );
        }

        private async Task OpenChangelog(long sprintId)
        {
            await _component.Find($"#collapse-sprint-{sprintId}-changelog").TriggerEventAsync("oncollapseshow", new CollapseEventArgs());
        }

        [Fact]
        public void ComponentRendered_NotYetOpened_LoadingMessageShown()
        {
            CreateComponent(new List<SprintChangelogEntry>());
            var textContent = _component.Find($"#collapse-sprint-{_sprint.Id}-changelog").TextContent;
            textContent.Should().Contain("Loading...");
        }

        [Fact]
        public async Task ComponentRendered_OpenedWithNoChangelogItems_RendersOnlyCreatedMessage() {
            CreateComponent(new List<SprintChangelogEntry>());
            await OpenChangelog(_sprint.Id);
            var messages = _component.FindComponents<MessageListItem>();
            messages.Should().HaveCount(1);
            messages.Last().Instance.Message.Should().BeOfType<CreatedMessage>();
        }

        [Fact]
        public async Task ComponentRendered_OpenedWithOneChangelogItems_RendersChangelogItemAndCreatedMessage()
        {
            var user = new User() { Id = 13, FirstName = "Tim", LastName = "Tam"};
            var changelogEntry = new SprintChangelogEntry(user, _sprint, nameof(Sprint.Name),
                Change<object>.Update("this", "that"))
            {
                Creator = user,
            };
            
            CreateComponent(new List<SprintChangelogEntry>() { changelogEntry });
            await OpenChangelog(_sprint.Id);
            var messages = _component.FindComponents<MessageListItem>();
            messages.Should().HaveCount(2);
            messages.Last().Instance.Message.Should().BeOfType<CreatedMessage>();
        }
    }
}