using System;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Services;
using Xunit;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Shared.Announcements;

namespace ScrumBoard.Tests.Blazor
{
    public class AnnouncementDisplayComponentTest : TestContext
    {
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        
        private IRenderedComponent<AnnouncementDisplay> _announcement;
        private bool _hideCallbackIsCalled;

        private readonly Announcement _ignorableAnnouncement = new ()
        {
            Content = "This announcement can be hidden",
            CanBeHidden = true
        };
        
        private readonly Announcement _notIgnorableAnnouncement = new ()
        {
            Content = "This announcement can not be hidden",
            CanBeHidden = false
        };
        
        public AnnouncementDisplayComponentTest()
        {
            _hideCallbackIsCalled = false;
            Services.AddScoped(_ => _mockJsInteropService.Object);
        }
        
        private void CreateComponent(Announcement announcement)
        {
            _announcement = RenderComponent<AnnouncementDisplay>(parameters => parameters
                .Add(p => p.Announcement, announcement)
                .Add(p => p.HideAnnouncementCallback, () => _hideCallbackIsCalled = true)
            );
        }

        [Fact]
        private void Rendered_AnnouncementCanBeHidden_HideButtonIsShown()
        {
            CreateComponent(_ignorableAnnouncement);
            _announcement.FindAll("#announcement-hide-button").Should().ContainSingle();
        }
        
        [Fact]
        private void Rendered_AnnouncementCanNotBeHidden_HideButtonIsNotShown()
        {
            CreateComponent(_notIgnorableAnnouncement);
            _announcement.FindAll("#announcement-hide-button").Should().BeEmpty();
        }
        
        [Fact]
        private void Rendered_AnnouncementHideButtonIsClicked_HideAnnouncementCallbackIsInvoked()
        {
            CreateComponent(_ignorableAnnouncement);
            _announcement.Find("#announcement-hide-button").Click();
            _hideCallbackIsCalled.Should().BeTrue();
        }
    }
}