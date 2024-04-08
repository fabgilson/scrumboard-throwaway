using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormAdministration;

public class AdminFormManagementComponentTest : TestContext
{
    private IRenderedFragment _component;

    private Mock<IConfigurationService> _mockConfigurationService = new(MockBehavior.Strict);
    
    private readonly Mock<IFormTemplateService> _mockTemplateService = new(MockBehavior.Strict);

    private Mock<IFormTemplateRepository> _mockFormTemplateRepository = new(MockBehavior.Strict);
    
    private readonly Mock<IFormInstanceService> _mockFormInstanceService = new();

    private Mock<ILogger<AdminFormManagement>> _mockLogger = new();
    
    private static readonly User RecipientUser = new() { Id = 1 };
    private static readonly User NonRecipientUser = new() { Id = 2 };

    private static readonly Project Project1 = new()
    {
        Id = 1,
        Name = "Team 600",
        MemberAssociations = new List<ProjectUserMembership>
        {
            new() { UserId = RecipientUser.Id, User = RecipientUser },
            new() { UserId = NonRecipientUser.Id, User = NonRecipientUser }
        }
    };

    private static FormTemplate FormTemplate = new()
    {
        Id = 1,
        Name = "Cool template",
        CreatorId = RecipientUser.Id,
        Blocks = new List<FormTemplateBlock>()
        {
            new TextQuestion() { Id = 1, FormTemplateId = 1, MaxResponseLength = 50, Prompt = "Original prompt"}
        }
    };

    public AdminFormManagementComponentTest()
    {
        _mockFormTemplateRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<FormTemplate> { FormTemplate });
        _mockFormTemplateRepository
            .Setup(x => x.GetByIdAsync(FormTemplate.Id, It.IsAny<Func<IQueryable<FormTemplate>, IQueryable<FormTemplate>>>()))
            .ReturnsAsync(FormTemplate);
        
        _mockConfigurationService.Setup(x => x.FeedbackFormsEnabled).Returns(true);

        _mockFormInstanceService.Setup(x => x.GetPaginatedAssignments(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedList<Assignment>(new List<Assignment>(), 0, 0, 0));
        
        Services.AddScoped(_ => _mockFormInstanceService.Object);
        Services.AddScoped(_ => _mockConfigurationService.Object);
        Services.AddScoped(_ => _mockFormTemplateRepository.Object);
        Services.AddScoped(_ => _mockTemplateService.Object);
        Services.AddScoped(_ => _mockLogger.Object);
        Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
    }

    private void CreateComponent()
    {
        _component = RenderComponent<AdminFormManagement>();
    }

    [Fact]
    public void EditFormTemplate_ShowPreview_ReturnToEdit_ChangesStillPresent()
    {
        Services.AddScoped(_ => new Mock<IProjectRepository>().Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);

        CreateComponent();
        const string updatedPrompt = "This has been changed";
        
        _component.Find("#edit-button").Click();
        _component.Find(".prompt-input").Attributes.GetNamedItem("value")!.NodeValue.Should().Be("Original prompt");
        _component.Find(".prompt-input").Change(updatedPrompt);

        _component.Find("#open-preview-button").Click();
        _component.Find("#exit-preview-button").Click();
        
        _component.Find(".prompt-input").Attributes.GetNamedItem("value")!.NodeValue.Should().Be(updatedPrompt);
    }
}