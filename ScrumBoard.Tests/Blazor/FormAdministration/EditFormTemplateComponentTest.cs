using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using ScrumBoard.Tests.Util;
using Xunit;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using ScrumBoard.Validators;
using ScrumBoard.Shared;
using ScrumBoard.Repositories;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;
using ScrumBoard.Services;
using ScrumBoard.Shared.FormAdministration;

namespace ScrumBoard.Tests.Blazor
{
    public class EditFeedbackFormComponentTest : TestContext
    {
        private readonly User _actingUser = new() {Id = 33, FirstName = "Jeff", LastName = "Jefferson" };
        
        private IRenderedComponent<EditFormTemplate> _component;

        private readonly FormTemplate _formTemplate;

        private readonly Mock<IFormTemplateRepository> _mockFeedbackFormRepository = new(MockBehavior.Strict);
        private readonly Mock<IFormTemplateService> _mockFormTemplateService = new(MockBehavior.Strict);

        private readonly Mock<Action> _onSave = new();
        private readonly Mock<Action> _onCancel = new();

        private bool _previewed = false;

        private const long TextBlockId = 1;
        private const long QuestionBlockId = 2;

        public EditFeedbackFormComponentTest()
        {
            _formTemplate = new FormTemplate();
            Services.AddScoped(_ => new Mock<IJsInteropService>().Object);
            Services.AddScoped(_ => _mockFeedbackFormRepository.Object);
            Services.AddScoped(_ => _mockFormTemplateService.Object);
            ComponentFactories.AddDummyFactoryFor<FormResponse>();
        }
        
        private void CreateComponent(bool addSampleQuestions=false)
        {
            _previewed = false;
            if (addSampleQuestions)
            {
                _formTemplate.Name = "Pacer test";
                _formTemplate.Blocks = new List<FormTemplateBlock>
                {
                    new TextBlock
                    {
                        Id= TextBlockId,
                        Content =
                            "The FitnessGram Pacer Test is a multistage aerobic capacity test that progressively gets more difficult as it continues.",
                        FormPosition = 0
                    },
                    
                    new PageBreak
                    {
                        FormPosition = 1
                    },
                    new TextQuestion
                    {
                        Id=QuestionBlockId,
                        Prompt = "What was your score?",
                        MaxResponseLength = 20,
                        FormPosition = 2
                    }
                };
                _formTemplate.Id = 1; // So that the page thinks this a pre-existing form
            }

            _component = RenderComponent<EditFormTemplate>(parameters => parameters
                .AddCascadingValue("Self", _actingUser)
                .Add(cut => cut.FormTemplate, _formTemplate)
                .Add(cut => cut.OnSave, _onSave.Object)
                .Add(cut => cut.OnCancel, _onCancel.Object)
                .Add(cut => cut.OnPreview, () => _previewed = true)
            );
        }

        [Theory]
        [InlineData("text")]
        [InlineData("multichoice")]
        [InlineData("page-break")]
        [InlineData("question")]
        private void AddBlock_Pressed_BlockOfTypeAdded(string blockType)
        {
            CreateComponent();
            
            _component.FindAll($".block-{blockType}").Should().BeEmpty();
            _component.Find($"#add-{blockType}").Click();
            _component.FindAll($".block-{blockType}").Should().ContainSingle();
        }
        
        [Theory]
        [InlineData("text")]
        [InlineData("multichoice")]
        [InlineData("page-break")]
        [InlineData("question")]
        private void DeleteBlock_Pressed_BlockRemoved(string blockType)
        {
            CreateComponent();
            
            _component.Find($"#add-{blockType}").Click();
            _component.Find($".block-{blockType}").QuerySelector(".delete-block")!.Click();
            
            _component.FindAll($".block-{blockType}").Should().BeEmpty();
        }

        [Fact]
        public void MoveBlock_MoveTopDown_MovesSuccessfully()
        {
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            CreateComponent(true);

            _component.Find("#move-down-button").Click();
            _component.Find("#edit-feedback-form-form").Submit();

            var captor = new ArgumentCaptor<FormTemplate>();
            _mockFormTemplateService.Verify(x => x.AddOrUpdateAsync(captor.Capture()));
            
            captor.Value.Blocks.First(block => block.FormPosition == 1).Id.Should().Be(TextBlockId);
        }
        
        [Fact]
        public void MoveBlock_MoveBottomUp_MovesSuccessfully()
        {
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            CreateComponent(true);
            
            _component.FindAll("#move-up-button").Last().Click();
            _component.Find("#edit-feedback-form-form").Submit();
            
            var captor = new ArgumentCaptor<FormTemplate>();
            _mockFormTemplateService.Verify(x => x.AddOrUpdateAsync(captor.Capture()));

            captor.Value.Blocks.First(block => block.FormPosition == 1).Id.Should().Be(QuestionBlockId);
        }
        
        [Fact]
        public void MoveBlock_AttemptToMoveTopUp_ButtonDisabled()
        {
            CreateComponent(true);
            _component.FindAll("#move-up-button").Count.Should().Be(2);
        }
        
        [Fact]
        public void MoveBlock_AttemptToMoveBottomDown_ButtonDisabled()
        {
            CreateComponent(true);
            _component.FindAll("#move-down-button").Count.Should().Be(2);
        }
        
        [Fact]
        private void Submit_NoQuestionBlocks_AtLeastOneQuestionMessageShown()
        {
            CreateComponent();
            
            _component.Find("#add-text").Click();
            _component.Find("#add-page-break").Click();
            
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#blocks-validation-message").TextContent.Should().Contain("At least one question must be provided");
        }
        
        [Fact]
        private void Submit_EmptyFeedbackFormName_EmptyNameMessageShown()
        {
            var attribute = typeof(FormTemplateForm).GetAttribute<RequiredAttribute>(nameof(FormTemplateForm.Name));
            
            CreateComponent();
            
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#feedback-form-name-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        private void Submit_FeedbackFormNameTooLong_EmptyNameMessageShown()
        {
            var attribute = typeof(FormTemplateForm).GetAttribute<MaxLengthAttribute>(nameof(FormTemplateForm.Name));
            CreateComponent();
            
            _component.Find("#feedback-form-name-input").Change(new string('a', attribute.Length + 1));
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#feedback-form-name-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        private void Submit_FeedbackFormNameOnlySpecialCharacters_SpecialCharactersMessageShown()
        {
            var attribute = typeof(FormTemplateForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(FormTemplateForm.Name));
            CreateComponent();
            
            _component.Find("#feedback-form-name-input").Change("*+-0");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#feedback-form-name-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }

        [Theory]
        [InlineData("multichoice")]
        [InlineData("question")]
        public void Submit_QuestionPromptEmpty_EmptyMessageShown(string blockType)
        {
            var attribute =
                typeof(QuestionForm).GetAttribute<RequiredAttribute>(nameof(QuestionForm.Prompt));
            
            CreateComponent();
            
            _component.Find($"#add-{blockType}").Click();
            
            _component.Find(".prompt-input").Change("");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#prompt-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Theory]
        [InlineData("multichoice")]
        [InlineData("question")]
        public void Submit_QuestionPromptTooLong_TooLongMessageShown(string blockType)
        {
            var attribute =
                typeof(QuestionForm).GetAttribute<MaxLengthAttribute>(nameof(QuestionForm.Prompt));
            
            CreateComponent();
            
            _component.Find($"#add-{blockType}").Click();
            
            _component.Find(".prompt-input").Change(new string('a', attribute.Length + 1));
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#prompt-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Theory]
        [InlineData("multichoice")]
        [InlineData("question")]
        public void Submit_QuestionPromptOnlySpecialCharacters_OnlySpecialCharactersMessageShown(string blockType)
        {
            var attribute =
                typeof(QuestionForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(QuestionForm.Prompt));
            
            CreateComponent();
            
            _component.Find($"#add-{blockType}").Click();
            
            _component.Find(".prompt-input").Change("*+-0");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#prompt-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_TextPromptEmpty_EmptyMessageShown()
        {
            var attribute =
                typeof(TextBlockForm).GetAttribute<RequiredAttribute>(nameof(TextBlockForm.Content));
            
            CreateComponent();
            
            _component.Find($"#add-text").Click();
            
            _component.Find(".text-input").Change("");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#text-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_TextPromptTooLong_TooLongMessageShown()
        {
            var attribute =
                typeof(TextBlockForm).GetAttribute<MaxLengthAttribute>(nameof(TextBlockForm.Content));
            
            CreateComponent();
            
            _component.Find($"#add-text").Click();
            
            _component.Find(".text-input").Change(new string('a', attribute.Length + 1));
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#text-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_TextOnlySpecialCharacters_OnlySpecialCharactersMessageShown()
        {
            var attribute =
                typeof(TextBlockForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(TextBlockForm.Content));
            
            CreateComponent();
            
            _component.Find($"#add-text").Click();
            
            _component.Find(".text-input").Change("*+-0");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#text-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Theory]
        [InlineData(9)]
        [InlineData(10_001)]
        public void Submit_TextQuestionMaxResponseLengthOutsideRange_RangeValidationMessageShown(int value)
        {
            var attribute =
                typeof(TextQuestionForm).GetAttribute<RangeAttribute>(nameof(TextQuestionForm.MaxResponseLength));
            
            CreateComponent();
            
            _component.Find($"#add-question").Click();
            
            _component.Find(".max-response-length-input").Change($"{value}");
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#max-response-length-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }

        [Fact]
        public void Submit_MultichoiceDeleteOption_MinOptionsLengthMessageShown()
        {
            var attribute =
                typeof(MultiChoiceQuestionForm).GetAttribute<MinLengthAttribute>(nameof(MultiChoiceQuestionForm.Options));
            
            CreateComponent();
            
            _component.Find($"#add-multichoice").Click();
            
            _component.Find(".btn-delete-option").Click();
            _component.Find("#edit-feedback-form-form").Submit();

            _component.Find("#options-validation-message").TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_MultichoiceOptionEmpty_EmptyMessageShown()
        {
            var attribute =
                typeof(MultichoiceOptionForm).GetAttribute<RequiredAttribute>(nameof(MultichoiceOptionForm.Content));

            CreateComponent();
            
            _component.Find($"#add-multichoice").Click();
            _component.Find("#edit-feedback-form-form").Submit();
            _component.Find(".option").QuerySelector(".validation-message")!.TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_MultichoiceOptionTooLong_TooLongMessageShown()
        {
            var attribute =
                typeof(MultichoiceOptionForm).GetAttribute<MaxLengthAttribute>(nameof(MultichoiceOptionForm.Content));

            CreateComponent();
            
            _component.Find($"#add-multichoice").Click();
            
            _component.Find(".option").QuerySelector("input")!.Change(new string('a', attribute.Length + 1));
            
            _component.Find("#edit-feedback-form-form").Submit();
            _component.Find(".option").QuerySelector(".validation-message")!.TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_MultichoiceOptionEntirelySpecialCharacters_EntirelySpecialCharactersMessageShown()
        {
            var attribute =
                typeof(MultichoiceOptionForm).GetAttribute<NotEntirelyNumbersOrSpecialCharactersAttribute>(nameof(MultichoiceOptionForm.Content));

            CreateComponent();
            
            _component.Find($"#add-multichoice").Click();
            
            _component.Find(".option").QuerySelector("input")!.Change("*+-0");
            
            _component.Find("#edit-feedback-form-form").Submit();
            _component.Find(".option").QuerySelector(".validation-message")!.TextContent.Should().Contain(attribute.ErrorMessage);
        }
        
        [Fact]
        public void Submit_FeedbackFormNameInUse_NameInUseErrorShown()
        {
            
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            CreateComponent();

            var feedbackFormName = "test feedback form name";
            _component.Find("#feedback-form-name-input").Change(feedbackFormName);

            var questionPrompt = "test question prompt";
            var questionMaxResponseLength = 600;
            _component.Find("#add-question").Click();
            _component.Find(".block-question").QuerySelector(".prompt-input")!.Change(questionPrompt);
            _component.Find(".block-question").QuerySelector(".max-response-length-input")!.Change($"{questionMaxResponseLength}");

            _mockFeedbackFormRepository
                .Setup(mock => mock.GetByNameAsync(feedbackFormName))
                .ReturnsAsync(new FormTemplate());
            
            _component.Find("#edit-feedback-form-form").Submit();
            
            _component
                .Find("#feedback-form-name-validation-message")
                .TextContent
                .Should()
                .Contain("Name already in use");
        }

        [Fact]
        public void Submit_ValidData_FeedbackFormAdded()
        {
            
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            CreateComponent();

            var feedbackFormName = "test feedback form name";
            _component.Find("#feedback-form-name-input").Change(feedbackFormName);

            var textContent = "test text content";
            _component.Find("#add-text").Click();
            _component.Find(".block-text").QuerySelector(".text-input")!.Change(textContent);
            
            _component.Find("#add-page-break").Click();

            var questionPrompt = "test question prompt";
            var questionMaxResponseLength = 600;
            _component.Find("#add-question").Click();
            _component.Find(".block-question").QuerySelector(".prompt-input")!.Change(questionPrompt);
            _component.Find(".block-question").QuerySelector(".max-response-length-input")!.Change($"{questionMaxResponseLength}");

            var multichoicePrompt = "test multichoice prompt";
            var multichoiceOptions = new[] {"test option 1", "test option 2" };
            _component.Find("#add-multichoice").Click();
            _component.Find(".block-multichoice").QuerySelector(".prompt-input")!.Change(multichoicePrompt);
            _component
                .Find(".block-multichoice")
                .QuerySelectorAll(".option")
                .First()
                .QuerySelector("input")!
                .Change(multichoiceOptions.First());
            _component
                .Find(".block-multichoice")
                .QuerySelectorAll(".option")
                .Last()
                .QuerySelector("input")!
                .Change(multichoiceOptions.Last());
            
            _component.Find("#edit-feedback-form-form").Submit();

            var captor = new ArgumentCaptor<FormTemplate>();
            _mockFormTemplateService
                .Verify(mock => mock.AddOrUpdateAsync(captor.Capture()), Times.Once);
            var feedbackForm = captor.Value;

            using (new AssertionScope())
            {
                feedbackForm.Name.Should().Be(feedbackFormName);
                feedbackForm.Blocks.Should().HaveCount(4);
                var blocks = feedbackForm.Blocks.ToList();

                var textBlock = blocks[0].Should().BeOfType<TextBlock>().Which;
                textBlock.Content.Should().Be(textContent);
            
                blocks[1].Should().BeOfType<PageBreak>();
            
                var questionBlock = blocks[2].Should().BeOfType<TextQuestion>().Which;
                questionBlock.Prompt.Should().Be(questionPrompt);
                questionBlock.MaxResponseLength.Should().Be(questionMaxResponseLength);
            
                var multichoiceBlock = blocks[3].Should().BeOfType<MultiChoiceQuestion>().Which;
                multichoiceBlock.Prompt.Should().Be(multichoicePrompt);
                multichoiceBlock.Options.Select(option => option.Content).Should().Equal(multichoiceOptions);
            }
        }

        [Fact]
        public void OpenPreview_Clicked_PreviewShown()
        {
            CreateComponent();

            _previewed.Should().BeFalse();
            
            const string feedbackFormName = "test feedback form name";
            _component.Find("#feedback-form-name-input").Change(feedbackFormName);
            
            _component.Find("#open-preview-button").Click();
            

            _previewed.Should().BeTrue();
        }
        
        [Fact]
        public async Task ExistingForm_SavedWithoutChanges_UpdateWithEquivalentEntity()
        {
            
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            _formTemplate.Id = 42;
            _formTemplate.Name = "Test name";
            _formTemplate.CreatorId = 8;
            _formTemplate.Created = DateTime.Now;
            
            var textBlock = new TextBlock() {Id = 4, Content = "Test content"};
            var questionBlock = new TextQuestion() {Id = 5, Prompt = "Test content", MaxResponseLength = 50};
            _formTemplate.Blocks = new List<FormTemplateBlock>()
            {
                textBlock,
                questionBlock,
            };
            foreach (var block in _formTemplate.Blocks) block.FormTemplateId = _formTemplate.Id;
            CreateComponent();
            
            
            await _component.Find("#edit-feedback-form-form").SubmitAsync();

            var captor = new ArgumentCaptor<FormTemplate>();
            _mockFormTemplateService
                .Verify(mock => mock.AddOrUpdateAsync(captor.Capture()), Times.Once);
            var saved = captor.Value;

            using (new AssertionScope())
            {
                saved.Id.Should().Be(_formTemplate.Id);
                saved.CreatorId.Should().Be(_formTemplate.CreatorId);
                saved.Created.Should().Be(_formTemplate.Created);
                saved.Name.Should().Be(_formTemplate.Name);
                saved.Blocks.Should().HaveCount(2);
                
                foreach (var (originalBlock, savedBlock) in _formTemplate.Blocks.Zip(saved.Blocks))
                {
                    originalBlock.Id.Should().Be(savedBlock.Id);
                    originalBlock.FormTemplateId.Should().Be(savedBlock.FormTemplateId);
                }

                var savedTextBlock = saved.Blocks.First().Should().BeOfType<TextBlock>().Which;
                savedTextBlock.Content.Should().Be(textBlock.Content);
                var savedQuestionBlock = saved.Blocks.Last().Should().BeOfType<TextQuestion>().Which;
                savedQuestionBlock.Prompt.Should().Be(savedQuestionBlock.Prompt);
                savedQuestionBlock.MaxResponseLength.Should().Be(savedQuestionBlock.MaxResponseLength);
            }
        }
        
        [Fact]
        public void ExistingForm_SavedWithChangedName_CheckNameCalled()
        {
            
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            _formTemplate.Id = 42;
            _formTemplate.Name = "Test name";
            _formTemplate.CreatorId = 8;
            _formTemplate.Created = DateTime.Now;

            var questionBlock = new TextQuestion() {Id = 5, Prompt = "Test content", MaxResponseLength = 50};
            _formTemplate.Blocks = new List<FormTemplateBlock>()
            {
                questionBlock,
            };
            foreach (var block in _formTemplate.Blocks) block.FormTemplateId = _formTemplate.Id;
            CreateComponent();
            
            var updatedFeedbackFormName = "test feedback form name";
            _component.Find("#feedback-form-name-input").Change(updatedFeedbackFormName);

            _component.Find("#edit-feedback-form-form").Submit();

            var captor = new ArgumentCaptor<FormTemplate>();
            _mockFormTemplateService
                .Verify(mock => mock.AddOrUpdateAsync(captor.Capture()), Times.Once);
            captor.Value.Name.Should().Be(updatedFeedbackFormName);
            
            _mockFormTemplateService
                .Verify(mock => mock.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public void ExistingForm_SavedWithChangedExistingName_CheckNameCalledAndNotUpdated()
        {
            _mockFormTemplateService.Setup(x => x.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFormTemplateService.Setup(x => x.AddOrUpdateAsync(It.IsAny<FormTemplate>()))
                .Returns(Task.CompletedTask);
            
            _formTemplate.Id = 42;
            _formTemplate.Name = "Test name";
            _formTemplate.CreatorId = 8;
            _formTemplate.Created = DateTime.Now;

            var questionBlock = new TextQuestion() {Id = 5, Prompt = "Test content", MaxResponseLength = 50};
            _formTemplate.Blocks = new List<FormTemplateBlock>()
            {
                questionBlock,
            };
            foreach (var block in _formTemplate.Blocks) block.FormTemplateId = _formTemplate.Id;
            CreateComponent();
            
            var updatedFeedbackFormName = "test feedback form name";
            _component.Find("#feedback-form-name-input").Change(updatedFeedbackFormName);

            _component.Find("#edit-feedback-form-form").Submit();

            _mockFormTemplateService
                .Verify(mock => mock.CheckForDuplicateName(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
