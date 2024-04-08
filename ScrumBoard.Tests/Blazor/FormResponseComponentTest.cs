using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Blazor.Modals;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class FormResponseComponentTest : BaseProjectScopedComponentTestContext<FormResponse>
{
    private bool _isPreview;

    private readonly FormTemplate _formTemplate;

    private readonly Mock<Action> _onClose = new();

    private readonly Mock<IFormInstanceService> _mockFormInstanceService = new();

    private readonly FormInstance _formInstance;

    public FormResponseComponentTest()
    {
        _formTemplate = new FormTemplate
        {
            Id = 1,
            Name = "Test form name",
            Blocks = new List<FormTemplateBlock>(),
        };
        var assignment = new Assignment
        {
            Id = 1,
            Name = "Assignment 1",
            FormTemplate = _formTemplate,
            FormTemplateId = _formTemplate.Id
        };
        _formInstance = new FormInstance
        {
            Id = 1,
            Assignment = assignment
        };

        Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
        Services.AddScoped(_ => _mockFormInstanceService.Object);
    }
        
    private void CreateComponent(bool addInstance = false) 
    {
        // Add dummy ModalTrigger
        ComponentFactories.Add(new ModalTriggerComponentFactory());
            
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(cut => cut.FormTemplate, _formTemplate)
            .Add(cut => cut.IsPreview, _isPreview)
            .Add(cut => cut.OnClose, _onClose.Object)
            .Add(cut => cut.FormInstance, addInstance ? _formInstance : null)
        );
    }

    [Fact]
    public void IsPreview_True_CanExitPreview()
    {
        _isPreview = true;
        CreateComponent();

        _onClose.Setup(mock => mock());
        ComponentUnderTest.Find("#exit-preview-button").Click();
        _onClose.Verify(mock => mock(), Times.Once);
    }
        
    [Fact]
    public void IsPreview_True_FinishButtonDisabled()
    {
        _isPreview = true;
        CreateComponent();
        ComponentUnderTest.Find("#finish-response-button").GetAttribute("disabled").Should().Be("");
    }
        
    [Fact]
    public void IsPreview_False_FinishButtonEnabled()
    {
        _isPreview = false;
        CreateComponent();
        ComponentUnderTest.Find("#finish-response-button").GetAttribute("disabled").Should().Be(null);
    }
        
    [Fact]
    public void IsPreview_False_CannotExitPreview()
    {
        _isPreview = false;
        CreateComponent();
        ComponentUnderTest.FindAll("#exit-preview-button").Should().BeEmpty();
    }

    [Fact]
    public void Rendered_MultiplePages_ProgressBarShown()
    {
        _formTemplate.Blocks.Add(new TextBlock());
        _formTemplate.Blocks.Add(new PageBreak());
        _formTemplate.Blocks.Add(new TextBlock());
            
        CreateComponent();

        ComponentUnderTest.FindAll(".progress").Should().ContainSingle();
    }
        
    [Fact]
    public void Rendered_SinglePage_ProgressBarHidden()
    {
        _formTemplate.Blocks.Add(new TextBlock());
        _formTemplate.Blocks.Add(new TextBlock());
            
        CreateComponent();

        ComponentUnderTest.FindAll(".progress").Should().BeEmpty();
    }
        
    [Fact]
    public void Rendered_MultiplePages_CanNavigate()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock { Content = firstPageContent });
        _formTemplate.Blocks.Add(new PageBreak());
        const string secondPageContent = "Second page content";
        _formTemplate.Blocks.Add(new TextBlock { Content = secondPageContent });
            
        CreateComponent();

        ComponentUnderTest.FindAll(".card").Should().ContainSingle().Which.TextContent.Should().Contain(firstPageContent);

        ComponentUnderTest.FindAll("#previous-page-button").Should().BeEmpty();
        ComponentUnderTest.Find("#next-page-button").Click();
            
        ComponentUnderTest.FindAll(".card").Should().ContainSingle().Which.TextContent.Should().Contain(secondPageContent);
            
        ComponentUnderTest.FindAll("#next-page-button").Should().BeEmpty();
        ComponentUnderTest.Find("#previous-page-button").Click();
            
        ComponentUnderTest.FindAll(".card").Should().ContainSingle().Which.TextContent.Should().Contain(firstPageContent);
    }

    [Fact]
    public void Rendered_ChangeTextInput_AutoSaveTriggered()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock { Content = firstPageContent });
        var textQuestion = new TextQuestion { Id = FakeDataGenerator.NextId, MaxResponseLength = 100 };
        _formTemplate.Blocks.Add(textQuestion);
            
        CreateComponent(addInstance: true);
            
        ComponentUnderTest.Find(".question-input").Input("New content");
            
        ComponentUnderTest.WaitForState(
            () => ComponentUnderTest.Find("#text-form-response-save-status-indicator").InnerHtml.Contains("Saved"), 
            TimeSpan.FromSeconds(5)
        );

        _mockFormInstanceService.Verify(m => 
            m.SaveAnswerToTextFormBlock(
                _formInstance.Id,
                textQuestion.Id, 
                "New content",
                ActingUser.Id, 
                It.IsAny<bool>()
            ),
            Times.Once
        );
    }
        
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Rendered_ClickRadioButton_AutoSaveTriggered(bool allowMultiple)
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock { Content = firstPageContent });
        var multiChoiceQuestion = new MultiChoiceQuestion
        {
            Id = FakeDataGenerator.NextId,
            AllowMultiple = allowMultiple,
            Options = new List<MultiChoiceOption>
            {
                new() { Id = 1, Content = "1" },
                new() { Id = 2, Content = "2" }
            }
        };
        _formTemplate.Blocks.Add(multiChoiceQuestion);
            
        CreateComponent(addInstance: true);
        
        if(!allowMultiple) ComponentUnderTest.FindAll(".form-check-input")[1].Change(1);
        else ComponentUnderTest.FindAll(".form-check-input")[1].Change(true);
        
        ComponentUnderTest.WaitForAssertion(
            () => ComponentUnderTest.Find("#multi-choice-form-response-save-status-indicator").InnerHtml.Should().Contain("Saved"), 
            TimeSpan.FromSeconds(5)
        );

        _mockFormInstanceService.Verify(m => 
            m.SaveAnswerToMultiChoiceFormBlock(
                _formInstance.Id, 
                multiChoiceQuestion.Id, 
                new List<long> {2}, 
                ActingUser.Id, 
                It.IsAny<bool>()
            ),
            Times.Once
        );
    }
        
    [Fact]
    public void SubmitForm_NotAllAnswersValid_FormNotSubmitted()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock() { Content = firstPageContent });
        _formTemplate.Blocks.Add(new MultiChoiceQuestion
        {
            AllowMultiple = true,
            Options = new List<MultiChoiceOption>
            {
                new() { Content = "1" },
                new() { Content = "2" }
            }
        });
        _formTemplate.Blocks.Add(new TextQuestion() { MaxResponseLength = 10 });
            
        CreateComponent(addInstance: true);
            
        // Nothing has been selected and there is no input for the text question
            
        ComponentUnderTest.Find("#finish-response-button").Click();

        _mockFormInstanceService.Verify(m => m.SubmitForm(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public void Rendered_FormIsUpcoming_SubmitButtonDisabled()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock() { Content = firstPageContent });
        _formTemplate.Blocks.Add(new TextQuestion() { MaxResponseLength = 10 });
        _formInstance.Assignment.StartDate = DateTime.Now.AddDays(1);
        CreateComponent(addInstance: true);
            
        ComponentUnderTest.Find($"#finish-response-button").HasAttribute("disabled").Should().BeTrue();
    }
        
    [Fact]
    public void Rendered_FormIsNotUpcoming_SubmitButtonEnabled()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock() { Content = firstPageContent });
        _formTemplate.Blocks.Add(new TextQuestion() { MaxResponseLength = 10 });
        CreateComponent(addInstance: true);
            
        ComponentUnderTest.Find($"#finish-response-button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Rendered_NoTextInInput_WordCountShowsZero()
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock() { Content = firstPageContent });
        _formTemplate.Blocks.Add(new TextQuestion() { MaxResponseLength = 100 });
            
        CreateComponent(addInstance: true);
        ComponentUnderTest.Find("#word-count").TextContent.Should().Be("Word count: 0");
    }
        
    [Theory]
    [InlineData("", 0)]
    [InlineData(" ", 0)]
    [InlineData(" \n \t", 0)]
    [InlineData("hello world", 2)]
    [InlineData(" hello \nworld\t ", 2)]
    [InlineData("1 2 3 ", 3)]
    [InlineData(" $ % []", 3)]
    public void Rendered_ChangeTextInput_WordCountUpdatesCorrectly(string textContent, int expectedCount)
    {
        const string firstPageContent = "First page content";
        _formTemplate.Blocks.Add(new TextBlock() { Content = firstPageContent });
        _formTemplate.Blocks.Add(new TextQuestion() { MaxResponseLength = 100 });
            
        CreateComponent(addInstance: true);
            
        ComponentUnderTest.Find(".question-input").Input(textContent);
        ComponentUnderTest.Find("#word-count").TextContent.Should().Be($"Word count: {expectedCount}");
    }
}