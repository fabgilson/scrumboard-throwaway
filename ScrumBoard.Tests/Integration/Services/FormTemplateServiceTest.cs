using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

public class FormTemplateServiceTest : BaseIntegrationTestFixture
{
    private readonly IDbContextFactory<DatabaseContext> _databaseContextFactory;
    private readonly IFormTemplateService _formTemplateService;
    
    private readonly FormTemplate _formTemplate = new()
    {
        Id = 1,
        Name = "Peer Feedback",
        CreatorId = FakeDataGenerator.DefaultUserId
    };

    public FormTemplateServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _formTemplateService = ActivatorUtilities.CreateInstance<FormTemplateService>(ServiceProvider);
        _databaseContextFactory = GetDbContextFactory();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        dbContext.FormTemplates.Add(_formTemplate);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CheckForDuplicateName_NameIsNotDuplicate_ReturnFalse()
    {
        var val = await _formTemplateService.CheckForDuplicateName("A different name", "What it used to be called");
        val.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForDuplicateName_NameIsSameAsCurrent_ReturnFalse()
    {
        var val = await _formTemplateService.CheckForDuplicateName(_formTemplate.Name, _formTemplate.Name);
        val.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForDuplicateName_NameIsDuplicate_ReturnTrue()
    {
        var val = await _formTemplateService.CheckForDuplicateName(_formTemplate.Name, "What it used to be called");
        val.Should().BeTrue();
    }

    [Fact]
    public async Task AddOrUpdateAsync_NewForm_FormSavedInDatabase()
    {
        int initialCount;
        
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            var count = await context.FormTemplates.CountAsync();
            initialCount = count;
        }

        const string templateName = "AddOrUpdateAsync_NewForm_FormSavedInDatabase";
        
        var newTemplate = new FormTemplate
        {
            Id = 0,
            Name = templateName,
            CreatorId = FakeDataGenerator.DefaultUserId
        };

        await _formTemplateService.AddOrUpdateAsync(newTemplate);
        
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            var count = await context.FormTemplates.CountAsync();
            count.Should().Be(initialCount + 1);
            context.FormTemplates.Count(t => t.Name == templateName).Should().Be(1);

        }
    }
    
    [Fact]
    public async Task AddOrUpdateAsync_ExistingForm_FormSavedInDatabase()
    {
        int initialCount;
        
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            var count = await context.FormTemplates.CountAsync();
            initialCount = count;
        }

        const string templateName = "We've changed the name of the template!";
        
        var newTemplate = new FormTemplate
        {
            Id = _formTemplate.Id,
            Name = templateName,
            Blocks = new List<FormTemplateBlock>(),
            CreatorId = FakeDataGenerator.DefaultUserId
        };

        await _formTemplateService.AddOrUpdateAsync(newTemplate);
        
        await using (var context = await _databaseContextFactory.CreateDbContextAsync())
        {
            var count = await context.FormTemplates.CountAsync();
            count.Should().Be(initialCount);
            (await context.FormTemplates.FindAsync(_formTemplate.Id))!.Name.Should().Be(templateName);

        }
    }
}