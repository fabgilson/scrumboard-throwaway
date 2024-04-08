using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using SharedLensResources.Blazor.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class ProjectServiceTest : BaseIntegrationTestFixture
{
    private readonly IProjectService _projectService;

    public ProjectServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _projectService = ServiceProvider.GetRequiredService<IProjectService>();
    }

    private async Task<IEnumerable<Project>> AddFakeProjectsToDb(int count, string namePrefix="ProjService")
    {
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var users = await context.Users.ToListAsync();
        var projects = FakeDataGenerator.CreateMultipleFakeProjects(count, namePrefix: namePrefix).ToArray();
        await context.Projects.AddRangeAsync(projects);
        await context.SaveChangesAsync();
        return projects;
    }

    private static VirtualizationRequest<Project> MakeRequest(int startIndex, int countRequested, string searchQuery=null, IEnumerable<Project> excludedProjects=null)
    {
        return new VirtualizationRequest<Project>
        {
            SearchQuery = searchQuery ?? "",
            Excluded = excludedProjects ?? new List<Project>(),
            StartIndex = startIndex,
            Count = countRequested
        };
    }
    
    [Theory]
    [InlineData(0, 0, 5, 0)] // No projects in the database
    [InlineData(5, 0, 10, 5)] // Requesting more projects than are in the database
    [InlineData(20, 20, 5, 0)] // Start index is equal to the number of projects in the database
    [InlineData(20, 25, 5, 0)] // Start index is more than the number of projects in the database
    [InlineData(20, 15, 10, 5)] // Remaining projects are less than the requested count
    [InlineData(20, 0, 0, 0)] // Requesting zero projects
    public async Task GetVirtualizedProjectsAsync_VariousPaginationSettings_ReturnsCorrectProjects(int numProjectsInDb, int startIndex, int countRequested, int countExpected)
    {
        await AddFakeProjectsToDb(numProjectsInDb);
        var response = await _projectService.GetVirtualizedProjectsAsync(MakeRequest(startIndex, countRequested));
        response.Results.Should().HaveCount(countExpected);
        response.TotalPossibleResultCount.Should().Be(numProjectsInDb);
    }
    
    [Theory]
    [InlineData(5, "ProjService", "projservice", 5)] // Case-insensitive match
    [InlineData(5, "ProjService", "Service", 5)] // Partial match
    [InlineData(5, "ProjService", "NoMatch", 0)] // No match
    [InlineData(5, "ProjService", "", 5)] // Empty string should match all
    [InlineData(5, "ProjService", null, 5)] // Null string should match all
    public async Task GetVirtualizedProjectsAsync_VariousSearchQueries_ReturnsCorrectProjects(int numProjectsInDb, string namePrefix, string searchQuery, int countExpected)
    {
        await AddFakeProjectsToDb(numProjectsInDb, namePrefix);
        var response = await _projectService.GetVirtualizedProjectsAsync(MakeRequest(0, numProjectsInDb, searchQuery));
        response.Results.Should().HaveCount(countExpected);
        response.TotalPossibleResultCount.Should().Be(countExpected);
    }

    [Fact]
    public async Task GetVirtualizedProjectsAsync_FirstPage_ProjectsOrderedByName()
    {
        var projects = new List<Project>();
        projects.AddRange(await AddFakeProjectsToDb(1, "eA"));
        projects.AddRange(await AddFakeProjectsToDb(1, "B"));
        projects.AddRange(await AddFakeProjectsToDb(1, "A"));
        projects.AddRange(await AddFakeProjectsToDb(1, "DDD"));
        projects.AddRange(await AddFakeProjectsToDb(1, "c"));
    
        var response = await _projectService.GetVirtualizedProjectsAsync(MakeRequest(0, 5));
    
        var orderedProjectIds = projects.OrderBy(x => x.Name).Select(x => x.Id).ToList();
        var responseProjectIds = response.Results.Select(x => x.Id).ToList();
    
        responseProjectIds.Should().Contain(orderedProjectIds);
    }

    [Fact]
    public async Task GetVirtualizedProjectsAsync_NotFirstPage_ProjectsOrderedByName()
    {
        var projects = new List<Project>();
        await AddFakeProjectsToDb(5, "eA");
        await AddFakeProjectsToDb(5, "B");
        await AddFakeProjectsToDb(5, "A");
        projects.AddRange(await AddFakeProjectsToDb(5, "DDD"));
        projects.AddRange(await AddFakeProjectsToDb(5, "c"));
    
        var response = await _projectService.GetVirtualizedProjectsAsync(MakeRequest(10, 10));
    
        var orderedProjectIds = projects.OrderBy(x => x.Name).Select(x => x.Id).ToList();
        var responseProjectIds = response.Results.Select(x => x.Id).ToList();
    
        responseProjectIds.Should().Contain(orderedProjectIds);
    }
}