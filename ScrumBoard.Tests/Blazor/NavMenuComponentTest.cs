using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public partial class NavMenuComponentTest : TestContext
    {
        private readonly Project _project = new() { Id= 100, Name="Test Project" };

        private readonly User _actingUser;

        private IRenderedComponent<NavMenu> _component;

        private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
        private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new();
        private readonly Mock<IAuthorizationPolicyProvider> _mockAuthPolicyProvider = new(MockBehavior.Strict);
        private readonly Mock<IAuthorizationService> _mockAuthService = new(MockBehavior.Strict);
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        private readonly Mock<IConfigurationService> _mockConfigurationService = new(MockBehavior.Strict);
        private readonly Mock<IProjectFeatureFlagService> _mockProjectFeatureFlagService = new(MockBehavior.Strict);
        private readonly Mock<IStandUpMeetingService> _mockStandUpMeetingService = new(MockBehavior.Strict);
        private readonly Mock<IWeeklyReflectionCheckInService> _mockWeeklyReflectionCheckInService = new(MockBehavior.Strict);
        private readonly Mock<IEntityLiveUpdateService> _mockEntityLiveUpdateService = new (MockBehavior.Loose);
        
        private readonly Mock<IClock> _clockMock = new();
        
        private readonly AuthenticationState _authState;
        
        private bool NextStandUpLabelExists() => _component.FindAll("#next-stand-up-label").Any(); 
        private bool StandUpRightNowLabelExists() => _component.FindAll("#stand-up-right-now-label").Any(); 
        private bool TimeRemainingLabelExists() => _component.FindAll("#time-remaining-in-stand-up-label").Any(); 
        
        [GeneratedRegex(@"\s+")]
        private static partial Regex ExtraWhiteSpace();
        private string TimeToNextStandUpText() => ExtraWhiteSpace().Replace(_component.Find("#next-stand-up-label").TextContent.Trim(), " ");
        private string TimeRemainingText() => ExtraWhiteSpace().Replace(_component.Find("#time-remaining-in-stand-up-label").TextContent.Trim(), " ");

        public NavMenuComponentTest()
        {
            _actingUser = new User() { Id = 33, FirstName = "Jeff", LastName="Geoff"};
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.NameId, _actingUser.Id.ToString()));   
            _authState = new AuthenticationState(new ClaimsPrincipal(claimsIdentity));

            _mockAuthService
                .Setup(mock => mock.AuthorizeAsync(_authState.User, null, It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success);

            _mockConfigurationService
                .SetupGet(mock => mock.FeedbackFormsEnabled)
                .Returns(true);
            _mockConfigurationService
                .SetupGet(mock => mock.StudentGuideEnabled)
                .Returns(true);

            Services.AddDbContextFactory<UsageDataDbContext>(options =>
                options.UseInMemoryDatabase("LoginTestInMemDb")
            );

            Services.AddScoped<IUsageDataService, UsageDataService>();
            
            Services.AddScoped(_ => _clockMock.Object);
            Services.AddScoped(_ => _mockProjectRepository.Object);
            Services.AddScoped(_ => _mockStateStorageService.Object);
            Services.AddScoped(_ => _mockAuthPolicyProvider.Object);
            Services.AddScoped(_ => _mockAuthService.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockConfigurationService.Object);
            Services.AddScoped(_ => _mockProjectFeatureFlagService.Object);
            Services.AddScoped(_ => _mockStandUpMeetingService.Object);
            Services.AddScoped(_ => _mockWeeklyReflectionCheckInService.Object);
            Services.AddScoped(_ => _mockEntityLiveUpdateService.Object);
        }
        
        private void CreateComponent(
            ProjectRole? projectRole, 
            FeatureFlagDefinition[] enabledFeatureFlags = null, 
            StandUpMeeting upcomingStandUp = null)
        {
            var selectedProjectId = projectRole == null ? (long?)null : _project.Id;
            enabledFeatureFlags ??= Array.Empty<FeatureFlagDefinition>();
            
            _mockStateStorageService.Setup(x => x.GetSelectedProjectIdAsync()).ReturnsAsync(selectedProjectId);
            var projectState = new ProjectState
            {
                ProjectRole = projectRole ?? ProjectRole.Developer,
                ProjectId = _project.Id,
                Project = _project
            };
            if (projectRole is not null)
            {
                _project.MemberAssociations.Add(new ProjectUserMembership {User = _actingUser, UserId = _actingUser.Id, Role = projectRole.Value});
            }
            
            _mockProjectRepository.Setup(x => x.GetByIdAsync(_project.Id)).ReturnsAsync(_project);
            _mockProjectRepository.Setup(x => x.GetByIdAsync
                (_project.Id, It.IsAny<Func<IQueryable<Project>,IQueryable<Project>>[]>())).ReturnsAsync(_project);

            _mockProjectFeatureFlagService.Setup(x => x
                .ProjectHasFeatureFlagAsync(_project, It.IsAny<FeatureFlagDefinition>())
            ).Returns<Project, FeatureFlagDefinition>((_, flag) => Task.FromResult(enabledFeatureFlags.Contains(flag)));

            _mockStandUpMeetingService.Setup(x => x.GetUpcomingStandUpIfPresentAsync(_actingUser, _project))
                .ReturnsAsync(upcomingStandUp);

            _mockWeeklyReflectionCheckInService.Setup(x => x.GetCheckInForUserForIsoWeekAndYear(
                It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())
            ).ReturnsAsync((WeeklyReflectionCheckIn)null);
            
            _component = RenderComponent<NavMenu>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .AddCascadingValue(Task.FromResult(_authState))
                .AddCascadingValue("ProjectState", projectState)
            );              
        }

        [Theory]
        [InlineData(null)]
        [InlineData(ProjectRole.Guest)]
        [InlineData(ProjectRole.Reviewer)]
        [InlineData(ProjectRole.Developer)]
        [InlineData(ProjectRole.Leader)]
        private void Rendered_AnyRole_SelectProjectAndLoginShown(ProjectRole? role)
        {
            CreateComponent(role);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains("Select Project")).Should().ContainSingle();
            links.Where(link => link.TextContent.Contains("Logout")).Should().ContainSingle();
        }
        
        [Theory]
        [InlineData("Sprint Board")]
        [InlineData("Backlog")]
        [InlineData("Report")]
        private void Rendered_DeveloperProjectSelected_HasExpectedLinksShown(string itemContent)
        {
            CreateComponent(ProjectRole.Developer);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains(itemContent)).Should().ContainSingle();
        }
        
        [Theory]
        [InlineData("Sprint Board")]
        [InlineData("Backlog")]
        [InlineData("Sprint Review")]
        [InlineData("Report")]
        private void Rendered_LeaderProjectSelected_HasExpectedLinksShown(string itemContent)
        {
            CreateComponent(ProjectRole.Leader);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains(itemContent)).Should().ContainSingle();
        }
        
        [Theory]
        [InlineData("Sprint Board")]
        [InlineData("Sprint Review")]
        [InlineData("Report")]
        private void Rendered_ReviewerProjectSelected_HasExpectedLinksShown(string itemContent)
        {
            CreateComponent(ProjectRole.Reviewer);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains(itemContent)).Should().ContainSingle();
        }

        [Fact]
        private void Rendered_AuthorisationFails_AdministrationLinkHidden()
        {
            _mockAuthService
                .Setup(mock => mock.AuthorizeAsync(_authState.User, null, It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Failed);
            CreateComponent(ProjectRole.Leader);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains("Administration")).Should().BeEmpty();
        }
        
        [Fact]
        private void Rendered_AuthorisationSucceeds_AdministrationLinkVisible()
        {
            _mockAuthService
                .Setup(mock => mock.AuthorizeAsync(_authState.User, null, It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success);
            CreateComponent(ProjectRole.Leader);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Contains("Administration")).Should().ContainSingle();
        }

        [Fact]
        private async Task RenderedAndLoggedIn_ClicksLogOut_UsageDataEventAddedToDatabaseAsync()
        {
            CreateComponent(ProjectRole.Developer);
            _component.Find("#logout").Click();

            using var context = Services.GetRequiredService<UsageDataDbContext>();

            // Give some time for fire-and-forget method to save
            var timeoutCounter = 0;
            var maxAttempts = 5;
            while(!context.UsageDataEvents.Any() && timeoutCounter < maxAttempts) {
                await Task.Delay(500);
                timeoutCounter++;
            }

            context.UsageDataEvents.Should().HaveCount(1);
            context.UsageDataEvents.First().Should().Match<AuthenticationUsageEvent>(e => 
                (e.Occurred > DateTime.Now.AddSeconds(-30)) &&  // Give 30 seconds of grace for time, race conditions are bad
                (e.UserId == _actingUser.Id) &&
                (e.AuthenticationEventType == AuthenticationUsageEventType.LogOut)
            );
        }
        
        [Fact]
        private void Rendered_StandUpsFeatureFlagNotEnabled_NoStandUpsEntryShown()
        {
            CreateComponent(ProjectRole.Developer);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Trim() == "Daily Scrums").Should().BeEmpty();
        }
        
        [Fact]
        private void Rendered_StandUpsFeatureFlagIsEnabled_StandUpsEntryIsShown()
        {
            CreateComponent(ProjectRole.Developer, [FeatureFlagDefinition.StandUpMeetingSchedule]);
            var links = _component.FindAll(".nav-link");
            links.Where(link => link.TextContent.Trim() == "Daily Scrums").Should().ContainSingle();
        }
        
        private List<ReportType> GetListedReportTypesFromDropdownMenu()
        {
            var button = _component.Find("#navmenu-reports-button");
            button.ClassList.Add("report-dropdown-menu-always-open");
            IRefreshableElementCollection<IElement> reportOptions = null;
            _component.WaitForState(() =>
            {
                reportOptions = _component.FindAll("div#project-report-selection-container > *");
                return reportOptions.Any();
            }, TimeSpan.FromSeconds(2));

            var foundEnums = reportOptions
                .Select(x => x.As<IHtmlAnchorElement>().Href.Split('/').Last())
                .Select(intValue => (ReportType)Convert.ToInt32(intValue)) // Cast report type param from route to enum
                .ToList();
            return foundEnums;
        }
        
        [Theory]
        [InlineData(ProjectRole.Guest, false)]
        [InlineData(ProjectRole.Guest, true)]
        [InlineData(ProjectRole.Developer, false)]
        [InlineData(ProjectRole.Developer, true)]
        [InlineData(ProjectRole.Reviewer, false)]
        [InlineData(ProjectRole.Reviewer, true)]
        [InlineData(ProjectRole.Leader, false)]
        [InlineData(ProjectRole.Leader, true)]
        public void Rendered_MultipleRoles_UserSeesCorrectReportTypesBasedOnRole(ProjectRole role, bool isCheckInsFeatureFlagSet)
        {
            CreateComponent(role, isCheckInsFeatureFlagSet ? new [] { FeatureFlagDefinition.WeeklyReflectionCheckInReportPage } : Array.Empty<FeatureFlagDefinition>());
            var foundReportTypes = GetListedReportTypesFromDropdownMenu();
            var expectedReportTypes = ReportTypeUtils.GetAllowedReportTypesForRole(role).ToList();
            if (!isCheckInsFeatureFlagSet) expectedReportTypes.Remove(ReportType.MyWeeklyReflections);
            using (new AssertionScope())
            {
                foundReportTypes.Should().ContainInOrder(expectedReportTypes);
                foundReportTypes.Should().HaveCount(expectedReportTypes.Count);
            } 
        }

        public static TheoryData<TimeSpan, string> CountdownForStandUpTheoryData => new ()
        {
            { new TimeSpan(0, 0, 0, 1), "Next Daily Scrum in: 1 second"},
            { new TimeSpan(0, 0, 0, 2), "Next Daily Scrum in: 2 seconds"},
            { new TimeSpan(0, 0, 1, 1), "Next Daily Scrum in: 1 minute, 1 second"},
            { new TimeSpan(0, 0, 2, 2), "Next Daily Scrum in: 2 minutes, 2 seconds"},
            { new TimeSpan(0, 1, 1, 0), "Next Daily Scrum in: 1 hour, 1 minute"},
            { new TimeSpan(0, 2, 2, 0), "Next Daily Scrum in: 2 hours, 2 minutes"},
            { new TimeSpan(1, 0, 0, 0), "Next Daily Scrum in: 1 day"},
            { new TimeSpan(1, 0, 0, 1), "Next Daily Scrum in: 1 day, 1 second"},
            { new TimeSpan(1, 0, 1, 0), "Next Daily Scrum in: 1 day, 1 minute"},
            { new TimeSpan(1, 1, 0, 0), "Next Daily Scrum in: 1 day, 1 hour"},
            { new TimeSpan(2, 0, 0, 0), "Next Daily Scrum in: 2 days"},
            { new TimeSpan(2, 0, 0, 2), "Next Daily Scrum in: 2 days, 2 seconds"},
            { new TimeSpan(2, 0, 2, 0), "Next Daily Scrum in: 2 days, 2 minutes"},
            { new TimeSpan(2, 2, 0, 0), "Next Daily Scrum in: 2 days, 2 hours"},
        };

        [Theory]
        [MemberData(nameof(CountdownForStandUpTheoryData))]
        public void TimerForStandUpRendered_StandUpInFuture_CorrectTimeShown(TimeSpan timeBeforeStandUpStart, string expectedTimeText)
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting { ScheduledStart = clockNow.Subtract(timeBeforeStandUpStart) };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );
            TimeToNextStandUpText().Should().Be(expectedTimeText);
        }
        
        [Fact]
        public void TimerForStandUpRendered_StandUpHappeningNow_StandUpHappeningNowLabelShown()
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting
            {
                ScheduledStart = clockNow.Subtract(new TimeSpan(0, 0, 5, 0)), 
                Duration = new TimeSpan(0, 15, 0)
            };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );
            _component.Find("#stand-up-right-now-label").Text().
                Should().Be("Daily Scrum happening right now!");
        }
        
        public static TheoryData<TimeSpan, string> TimeRemainingForStandUpTheoryData => new ()
        {
            { new TimeSpan(0, 0, 0, 1), "Ends in 1 second"},
            { new TimeSpan(0, 0, 0, 2), "Ends in 2 seconds"},
            { new TimeSpan(0, 0, 1, 1), "Ends in 1 minute, 1 second"},
            { new TimeSpan(0, 0, 2, 2), "Ends in 2 minutes, 2 seconds"},
        };

        [Theory]
        [MemberData(nameof(TimeRemainingForStandUpTheoryData))]
        public void TimerForStandUpRendered_StandUpHappeningNow_CorrectTimeRemainingShown(TimeSpan timeRemainingInStandUp, string expectedTimeText)
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting
            {
                ScheduledStart = clockNow.AddMinutes(-15).Add(timeRemainingInStandUp), 
                Duration = new TimeSpan(0, 15, 0)
            };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );
            TimeRemainingText().Should().Be(expectedTimeText);
        }

        [Fact]
        public void TimerForStandUpRendered_TimerChanges_LabelUpdates()
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting { ScheduledStart = clockNow.AddMinutes(5) };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );
            
            TimeToNextStandUpText().Should().Be("Next Daily Scrum in: 5 minutes");
            _clockMock.Setup(x => x.Now).Returns(clockNow.AddSeconds(5));
            
            _component.Render();
            
            TimeToNextStandUpText().Should().Be("Next Daily Scrum in: 4 minutes, 55 seconds");
        }
        
        [Fact]
        public void TimerForStandUpRendered_StandUpStarts_LabelUpdates()
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting
            {
                ScheduledStart = clockNow.AddSeconds(5),
                Duration = new TimeSpan(0, 15, 0)
            };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );

            using (new AssertionScope())
            {
                StandUpRightNowLabelExists().Should().BeFalse();
                TimeRemainingLabelExists().Should().BeFalse();
                NextStandUpLabelExists().Should().BeTrue();
                TimeToNextStandUpText().Should().Be("Next Daily Scrum in: 5 seconds");
            }
            
            _clockMock.Setup(x => x.Now).Returns(clockNow.AddSeconds(15));
            _component.Render();
            _component.WaitForElement("#time-remaining-in-stand-up-label", TimeSpan.FromSeconds(5));

            using (new AssertionScope())
            {
                StandUpRightNowLabelExists().Should().BeTrue();
                TimeRemainingLabelExists().Should().BeTrue();
                NextStandUpLabelExists().Should().BeFalse();
                TimeRemainingText().Should().Be("Ends in 14 minutes, 50 seconds");
            }
        }
        
        [Fact]
        public void TimerForStandUpRendered_StandUpEnds_LabelUpdates()
        {
            var clockNow = DateTime.Now;
            _clockMock.Setup(x => x.Now).Returns(clockNow);
            var upcoming = new StandUpMeeting
            {
                ScheduledStart = clockNow.AddMinutes(-14),
                Duration = new TimeSpan(0, 15, 0)
            };
            var afterUpcoming = new StandUpMeeting
            {
                ScheduledStart = clockNow.AddDays(2).AddMinutes(1),
                Duration = new TimeSpan(0, 15, 0)
            };
            CreateComponent(
                ProjectRole.Developer, 
                new[] { FeatureFlagDefinition.StandUpMeetingSchedule, FeatureFlagDefinition.WeeklyReflectionCheckIn },
                upcoming
            );

            using (new AssertionScope())
            {
                StandUpRightNowLabelExists().Should().BeTrue();
                TimeRemainingLabelExists().Should().BeTrue();
                NextStandUpLabelExists().Should().BeFalse();
                TimeRemainingText().Trim().Should().Be("Ends in 1 minute");
            }

            _mockStandUpMeetingService
                .Setup(x => x.GetUpcomingStandUpIfPresentAsync(It.IsAny<User>(), It.IsAny<Project>()))
                .ReturnsAsync(afterUpcoming);
            _clockMock.Setup(x => x.Now).Returns(clockNow.AddMinutes(2));
            _component.Render();

            using (new AssertionScope())
            {
                StandUpRightNowLabelExists().Should().BeFalse();
                TimeRemainingLabelExists().Should().BeFalse();
                NextStandUpLabelExists().Should().BeTrue();
                TimeToNextStandUpText().Should().Be("Next Daily Scrum in: 1 day, 23 hours");
            }
        }
    }
}