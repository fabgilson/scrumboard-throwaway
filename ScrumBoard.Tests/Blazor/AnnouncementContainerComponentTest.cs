using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using Xunit;
using ScrumBoard.DataAccess;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Services.UsageData;
using System.IdentityModel.Tokens.Jwt;
using System;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Shared.Announcements;

namespace ScrumBoard.Tests.Blazor
{
    public class AnnouncementContainerComponentTest : TestContext
    {
        private readonly User _actingUser;
        private readonly AuthenticationState _authState;
        
        private readonly Mock<IAnnouncementService> _mockAnnouncementService = new(MockBehavior.Strict);
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        
        private IRenderedComponent<AnnouncementContainer> _announcementContainer;

        public AnnouncementContainerComponentTest()
        {
            _actingUser = new User() { Id = 33, FirstName = "Jeff", LastName="Geoff"};
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.NameId, _actingUser.Id.ToString()));   
            _authState = new AuthenticationState(new ClaimsPrincipal(claimsIdentity));

            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockAnnouncementService.Object);
        }
        
        private void CreateComponent()
        {
            _announcementContainer = RenderComponent<AnnouncementContainer>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue(Task.FromResult(_authState))
            );
        }

        [Fact]
        private void Rendered_NoAnnouncementsFound_NothingIsShown()
        {
            _mockAnnouncementService
                .Setup(x => x.GetActiveAnnouncementsForUserAsync(_actingUser.Id))
                .Returns<long>(_ => Task.FromResult<ICollection<Announcement>>(Array.Empty<Announcement>()));
            CreateComponent();
            
            _announcementContainer.Find("#announcements-container").Children.Should().BeEmpty();
        }
        
        [Fact]
        private void Rendered_OneAnnouncementFound_AnnouncementIsShown()
        {
            var shownAnnouncements = new Announcement[] { new() { Content = "Example announcement content" } };
            
            _mockAnnouncementService
                .Setup(x => x.GetActiveAnnouncementsForUserAsync(_actingUser.Id))
                .Returns<long>(_ => Task.FromResult<ICollection<Announcement>>(shownAnnouncements));
            CreateComponent();
            
            _announcementContainer.Find("#announcements-container").Children.Should().ContainSingle();
        }
        
        [Fact]
        private void Rendered_OneAnnouncementFound_AnnouncementHasCorrectContent()
        {
            var shownAnnouncements = new Announcement[] { new() { Content = "Example announcement content" } };
            
            _mockAnnouncementService
                .Setup(x => x.GetActiveAnnouncementsForUserAsync(_actingUser.Id))
                .Returns<long>(_ => Task.FromResult<ICollection<Announcement>>(shownAnnouncements));
            CreateComponent();
            
            _announcementContainer.Find("#announcements-container").Children.First().TextContent.Trim().Should().Be(shownAnnouncements[0].Content);
        }
        
        [Fact]
        private void Rendered_MultipleAnnouncementsFound_AllAnnouncementsAreShown()
        {
            var shownAnnouncements = new Announcement[]
            {
                new() { Content = "Example announcement content 1" },
                new() { Content = "Example announcement content 2" },
                new() { Content = "Example announcement content 3" },
                new() { Content = "Example announcement content 4" },
            };
            
            _mockAnnouncementService
                .Setup(x => x.GetActiveAnnouncementsForUserAsync(_actingUser.Id))
                .Returns<long>(_ => Task.FromResult<ICollection<Announcement>>(shownAnnouncements));
            CreateComponent();

            _announcementContainer.Find("#announcements-container").Children.Should()
                .HaveCount(shownAnnouncements.Length);
        }
    }
}