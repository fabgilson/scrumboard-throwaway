using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Pages;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.StandUps;

public class AdminStandUpSchedulePageTest : TestContext
{
    private readonly Mock<IStandUpMeetingService> _standUpMeetingServiceMock = new();
    private readonly Mock<IProjectService> _projectServiceMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public AdminStandUpSchedulePageTest()
    {
        Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
        Services.AddScoped(_ => _standUpMeetingServiceMock.Object);
        Services.AddScoped(_ => _projectServiceMock.Object);
        Services.AddScoped(_ => _clockMock.Object);
    }

    /// <summary>
    /// Creates and renders the AdminStandUpSchedule component under test, with specified parameters and mock services.
    /// </summary>
    /// <param name="delayBeforeLoadingStandUps">Optional delay before loading stand-ups to simulate asynchronous operations.</param>
    /// <param name="upcomingStandUps">Collection of upcoming stand-up meetings to be returned by the mocked service.</param>
    /// <param name="upcomingStandUpsPageCount">Number of pages that the upcoming stand-ups should be divided into.</param>
    /// <param name="currentPageNumber">Current page number to be displayed.</param>
    /// <param name="searchableProjects">Collection of projects that can be searched and filtered in the component.</param>
    /// <returns>A rendered component of type AdminStandUpSchedule with the specified setup and dependencies.</returns>
    private IRenderedComponent<AdminStandUpSchedule> CreateComponentUnderTest(
        TimeSpan? delayBeforeLoadingStandUps = null,
        IEnumerable<StandUpMeeting> upcomingStandUps = null,
        int upcomingStandUpsPageCount = 1,
        int currentPageNumber = 1,
        IEnumerable<Project> searchableProjects = null
    ) {
        _projectServiceMock.Setup(x => x.GetVirtualizedProjectsAsync(It.IsAny<VirtualizationRequest<Project>>()))
            .ReturnsAsync(new VirtualizationResponse<Project>
            {
                Results = searchableProjects ?? new[] { FakeDataGenerator.CreateFakeProject() },
                TotalPossibleResultCount = searchableProjects?.Count() ?? 1
            });
        
        _standUpMeetingServiceMock.Setup(x => x.GetPaginatedAllUpcomingStandUpsForProjects(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<Project>>(), It.IsAny<bool>())
        ).Returns(
            async (int pageNum, int pageSize, IEnumerable<Project> filteredProjects, bool onlyActiveSprints) =>
            {
                if(delayBeforeLoadingStandUps is not null) await Task.Delay(delayBeforeLoadingStandUps.Value);
                return new PaginatedList<StandUpMeeting>(
                    upcomingStandUps ?? new List<StandUpMeeting>(),
                    (upcomingStandUpsPageCount - 1) * pageSize + (upcomingStandUps?.Count() ?? 0),
                    currentPageNumber,
                    pageSize
                );
            }
        );
        
        return RenderComponent<AdminStandUpSchedule>();
    }

    /// <summary>
    /// Verifies that the GetPaginatedAllUpcomingStandUpsForProjects method of the stand-up meeting service is invoked with the expected parameters.
    /// </summary>
    /// <param name="times">Specifies the number of times the method is expected to be called.</param>
    /// <param name="pageNum">The expected page number parameter. If null, any value is accepted.</param>
    /// <param name="pageSize">The expected page size parameter. If null, any value is accepted.</param>
    /// <param name="filteredProjects">The expected collection of filtered projects. If null, any value is accepted.</param>
    /// <param name="limitedToActiveSprints">The expected boolean indicating whether to limit to active sprints. If null, any value is accepted.</param>
    private void VerifyStandUpsRefreshed(Times times, int? pageNum=null, int? pageSize=null, IEnumerable<Project> filteredProjects=null, bool? limitedToActiveSprints=null)
    {
        _standUpMeetingServiceMock.Verify(x => x.GetPaginatedAllUpcomingStandUpsForProjects(
            It.Is<int>(p => !pageNum.HasValue || p == pageNum.Value),
            It.Is<int>(p => !pageSize.HasValue || p == pageSize.Value),
            It.Is<IEnumerable<Project>>(p => filteredProjects == null || p == filteredProjects),
            It.Is<bool>(p => !limitedToActiveSprints.HasValue || p == limitedToActiveSprints.Value)
        ), times);
    }

    [Fact]
    private void PageLoading_StandUpsHaveNotYetLoaded_LoadingSpinnerShown()
    {
        var cut = CreateComponentUnderTest(delayBeforeLoadingStandUps: new TimeSpan(0, 0, 5));
        cut.FindAll("#loading-spinner").Should().ContainSingle();
        cut.FindAll("#admin-stand-up-schedule-container").Should().BeEmpty();
    }
    
    [Fact]
    private void PageLoaded_NoStandUpsReturned_NoStandUpsMessageShown()
    {
        var cut = CreateComponentUnderTest(upcomingStandUps: new List<StandUpMeeting>());
        cut.FindAll("#loading-spinner").Should().BeEmpty();
        cut.FindAll("#admin-stand-up-schedule-container").Should().ContainSingle();
        cut.FindAll("#upcoming-stand-ups-container").Should().BeEmpty();
        cut.FindAll("#no-upcoming-stand-ups-label").Should().ContainSingle();
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    private void PageLoaded_StandUpsReturned_AllStandUpsShownInTable(int standUpCount)
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(standUpCount, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps);
        
        cut.FindAll("#loading-spinner").Should().BeEmpty();
        cut.FindAll("#upcoming-stand-ups-container").Should().ContainSingle();
        
        // Ensure that one row exists for each upcoming stand-up
        cut.FindAll("*[id^='upcoming-stand-up-row-']")
            .Select(x => x.Id!.Replace("upcoming-stand-up-row-", ""))
            .Should().Contain(upcomingStandUps.Select(x => x.Id.ToString()));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    private void PageLoaded_MultiplePagesReturned_CorrectPageNumbersShown(int pageCount)
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps, upcomingStandUpsPageCount: pageCount);

        var pageButtonsComponent = cut.FindComponent<PageButtons>();
        pageButtonsComponent.Instance.TotalPages.Should().Be(pageCount);
    }

    [Fact]
    private void MultiplePages_PageChanged_StandUpsRefreshed()
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps, upcomingStandUpsPageCount: 5);

        var pageButtonsComponent = cut.FindComponent<PageButtons>();

        VerifyStandUpsRefreshed(Times.Once());
        pageButtonsComponent.Find("#next-page-button").Click();
        VerifyStandUpsRefreshed(Times.Exactly(2));
    }
    
    [Fact]
    private void PageLoaded_ShowActiveSprintsOnlyCheckBoxChanged_StandUpsRefreshed()
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps);

        VerifyStandUpsRefreshed(Times.Once());
        cut.Find("#show-in-active-sprints-only-checkbox").Change(false);
        VerifyStandUpsRefreshed(Times.Exactly(2));
        _standUpMeetingServiceMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    private void NotOnFirstPage_ShowActiveSprintsOnlyCheckBoxChanged_ReturnedToFirstPage()
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps, currentPageNumber: 2);

        _standUpMeetingServiceMock.Invocations.Clear();
        cut.Find("#show-in-active-sprints-only-checkbox").Change(false);
        VerifyStandUpsRefreshed(Times.Once(), pageNum: 1);
    }

    [Fact]
    private void PageLoaded_ProjectFilterChanged_StandUpsRefreshed()
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps);

        VerifyStandUpsRefreshed(Times.Once());

        var projectSelector = cut.FindComponent<SearchableDropDown<Project>>();
        
        // Open dropdown
        projectSelector.Find("button").Click();
        
        // Select first result
        projectSelector.Find("#search-results-container")
            .Children.First(x => x.ClassName?.Contains("dropdown-item") ?? false)
            .Click();
        
        VerifyStandUpsRefreshed(Times.Exactly(2));
    }

    [Fact]
    private void NotOnFirstPage_ProjectFilterChanged_ReturnedToFirstPage()
    {
        var fakeSprint = FakeDataGenerator.CreateFakeSprint(FakeDataGenerator.CreateFakeProject());
        var upcomingStandUps = FakeDataGenerator.CreateMultipleFakeStandUps(5, fakeSprint).ToArray();
        var cut = CreateComponentUnderTest(upcomingStandUps: upcomingStandUps, currentPageNumber: 2);

        _standUpMeetingServiceMock.Invocations.Clear();
        
        var projectSelector = cut.FindComponent<SearchableDropDown<Project>>();
        projectSelector.Find("button").Click();
        projectSelector.Find("#search-results-container")
            .Children.First(x => x.ClassName?.Contains("dropdown-item") ?? false)
            .Click();
        
        VerifyStandUpsRefreshed(Times.Once(), pageNum: 1);
        _standUpMeetingServiceMock.VerifyNoOtherCalls();
    }
}