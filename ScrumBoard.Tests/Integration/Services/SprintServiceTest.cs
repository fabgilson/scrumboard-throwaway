using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class SprintServiceTest : BaseIntegrationTestFixture
{
    private readonly ISprintService _sprintService;

    private Sprint _sprint;
    
    public SprintServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _sprintService = ServiceProvider.GetRequiredService<ISprintService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        var project = FakeDataGenerator.CreateFakeProject();
        _sprint = FakeDataGenerator.CreateFakeSprint(project);

        await dbContext.AddAsync(_sprint);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSprintById_SprintDoesNotExist_NullReturned()
    {
        var sprint = await _sprintService.GetByIdAsync(12345);
        sprint.Should().BeNull();
    }
    
    [Fact]
    public async Task GetSprintById_SprintDoesExist_CorrectSprintReturned()
    {
        var sprint = await _sprintService.GetByIdAsync(_sprint.Id);
        sprint.Should().BeEquivalentTo(_sprint, options => options
            .Excluding(x => x.Project)
            .Excluding(x => x.OverheadEntries)
            .Excluding(x => x.Stories)
            .Excluding(x => x.Creator)
        );
    }
}