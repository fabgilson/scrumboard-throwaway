using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.DataAccess;
using ScrumBoard.Filters;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports.MarkingStats;

public class MarkingStatsReportTest : BaseProjectScopedComponentTestContext<Shared.Report.MarkingStats>
{
    private readonly Mock<IMarkingStatsService> _mockMarkingStatsService = new();
    private readonly Mock<IWorklogEntryService> _mockWorklogEntryService = new();
    private readonly Mock<IWorklogTagRepository> _mockWorklogTagRepository = new();
    private readonly Mock<IUserStoryTaskTagRepository> _mockUserStoryTaskTagRepository = new();
    private readonly Mock<ISortableService<TableColumnConfiguration>> _mockSortableService = new();

    public MarkingStatsReportTest()
    {
        Services.AddDbContextFactory<DatabaseContext>(options =>
            options.UseInMemoryDatabase("ScrumBoardInMemDbMyStatsTests"));

        _mockUserStoryTaskTagRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<UserStoryTaskTag>());
        _mockWorklogTagRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<WorklogTag>());
        _mockWorklogEntryService.Setup(x => 
                x.GetByProjectFilteredAndPaginatedAsync(
                    It.IsAny<long>(), 
                    It.IsAny<WorklogEntryFilter>(), 
                    It.IsAny<TableColumn>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<int>(), 
                    It.IsAny<int>(), 
                    It.IsAny<long?>()))
            .ReturnsAsync(new PaginatedList<WorklogEntry>(new List<WorklogEntry>(), 0, 0, 0));
        
        Services.AddScoped(_ => new Mock<IUserRepository>().Object);
        Services.AddScoped(_ => _mockSortableService.Object);
        Services.AddScoped(_ => new Mock<IUserStoryRepository>().Object);
        Services.AddScoped(_ => new Mock<IUserStoryTaskRepository>().Object);
        Services.AddScoped(_ => _mockUserStoryTaskTagRepository.Object);
        Services.AddScoped(_ =>_mockWorklogTagRepository.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object);
        Services.AddScoped(_ => new Mock<IScrumBoardStateStorageService>().Object);
        Services.AddScoped(_ => _mockWorklogEntryService.Object);
        Services.AddScoped(_ => _mockMarkingStatsService.Object);
    }

    [Theory]
    [InlineData(ProjectRole.Guest)]
    [InlineData(ProjectRole.Reviewer)]
    [InlineData(ProjectRole.Developer)]
    private void Rendered_UsersRoleIsNotPermittedToViewReport_ErrorMessageShown(ProjectRole role)
    {
        CreateComponentUnderTest(actingUserRoleInProject: role);
        ComponentUnderTest.FindAll("#marking-stats-report-forbidden-error-message").Count.Should().Be(1);
        ComponentUnderTest.FindAll("#marking-stats-report-container").Count.Should().Be(0);
    }

    [Fact]
    private void Rendered_UsersRoleIsPermittedToViewReport_StatisticsReportShown()
    {     
        _mockMarkingStatsService.Setup(
                x => x.CalculateDateRangesForSprintOrSprints(It.IsAny<IList<Sprint>>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<DateOnly>());
        CreateComponentUnderTest(actingUserRoleInProject: ProjectRole.Leader);
        ComponentUnderTest.FindAll("#marking-stats-report-forbidden-error-message").Count.Should().Be(0);
        ComponentUnderTest.FindAll("#marking-stats-report-container").Count.Should().Be(1);
    }
}