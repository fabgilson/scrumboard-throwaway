using System;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Services;
using ScrumBoard.Shared.FormResponseComponents;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormSubmission;

public class FormBlockTextQuestionResponseTest : BaseProjectScopedComponentTestContext<FormBlockTextQuestionResponse>
{
    private readonly Mock<IFormInstanceService> _formInstanceServiceMock = new ();

    private TextQuestion _question;
    private TextAnswer _answer;
    private TextAnswerForm _answerForm;

    private long _formInstanceId;

    private IElement TextInput => ComponentUnderTest.Find("#form-text-answer-text-input");
    
    private Func<IElement> GetSavingText => () => ComponentUnderTest.WaitForElement("#saving-text", TimeSpan.FromSeconds(3));
    private Func<IElement> GetSavedText => () => ComponentUnderTest.WaitForElement("#saved-text", TimeSpan.FromSeconds(3));
    private Func<IElement> GetUnsavedText => () => ComponentUnderTest.WaitForElement("#unsaved-text", TimeSpan.FromSeconds(3)); // Extra long timeout to account for auto-save delay

    private Func<IElement> GetSaveErrorMessage => () => ComponentUnderTest.WaitForElement("#save-error-text");
    
    private void CreateComponent(string alreadyExistingAnswerText = null)
    {
        Services.AddScoped(_ => _formInstanceServiceMock.Object);

        _formInstanceId = FakeDataGenerator.NextId;
        _question = new TextQuestion
        {
            Prompt = "Question prompt"
        };

        _answerForm = new TextAnswerForm(_question, null);
        var editContext = new EditContext(_answerForm);

        if (alreadyExistingAnswerText is not null)
        {
            _answer = new TextAnswer
            {
                Id = FakeDataGenerator.NextId,
                Answer = alreadyExistingAnswerText
            };
            _formInstanceServiceMock
                .Setup(x => x.GetAnswerByFormInstanceAndQuestionIdAsync(_formInstanceId, _question.Id))
                .ReturnsAsync(_answer);
        }
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.FormInstanceId, _formInstanceId)
            .Add(x => x.TextQuestion, _question)
            .Add(x => x.TextAnswerForm, _answerForm)
            .AddCascadingValue(editContext)
        );
    }
    
    [Fact]
    private void EnterText_SavesSuccessfully_SaveStatusIndicatorShowsSavingThenSaved()
    {
        // Configure the service method to not finish until we explicitly tell it to
        var saveCompletionSource = new TaskCompletionSource();
        _formInstanceServiceMock
            .Setup(x => x.SaveAnswerToTextFormBlock(
                It.IsAny<long>(), 
                It.IsAny<long>(), 
                It.IsAny<string>(), 
                It.IsAny<long>(),
                It.IsAny<bool>()))
            .Returns(saveCompletionSource.Task);
        CreateComponent();
        
        TextInput.Input("Some text");
        GetSavingText.Should().NotThrow();

        // Tell the service method to finish
        saveCompletionSource.SetResult();

        GetSavedText.Should().NotThrow();
    }
    
    [Fact]
    private void NewTextAnswer_FormAlreadySubmitted_ErrorShownAndSaveStatusIndicatorShowsUnsaved()
    {
        _formInstanceServiceMock
            .Setup(x => x.SaveAnswerToTextFormBlock(
                It.IsAny<long>(), 
                It.IsAny<long>(), 
                It.IsAny<string>(), 
                It.IsAny<long>(),
                It.IsAny<bool>()))
            .Throws(new InvalidOperationException());
        CreateComponent();

        TextInput.Input("Some text");
        GetUnsavedText.Should().NotThrow();
        
        var errorMessage = GetSaveErrorMessage.Should().NotThrow().Which;
        errorMessage.TextContent.Trim()
            .Should()
            .Contain("Unable to save response because this form has already been submitted. Please refresh the page to continue");
    }

    [Fact]
    private async Task NotificationReceivedThatParentFormInstanceHasChanged_IdMatchesThisForm_AnswerIsRefreshed()
    {
        CreateComponent();
        _formInstanceServiceMock.Invocations.Clear();
        
        // Invoke the handler to simulate an EntityHasChanged event being received for the parent form instance
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<FormInstance>(_formInstanceId, LiveUpdateEventType.EntityHasChanged);
        var changeEventHandler = liveUpdateHandler.GetEntityHasChangedHandler();
        await ComponentUnderTest.InvokeAsync(() => changeEventHandler.Invoke());

        _formInstanceServiceMock.Invocations.Should().ContainSingle();
        _formInstanceServiceMock.Verify(
            x => x.GetAnswerByFormInstanceAndQuestionIdAsync(_formInstanceId, _question.Id),
            Times.Once
        );
    }

    [Fact]
    private void Rendered_NoExistingAnswer_OnlyHandlerRegisteredIsForParentFormChanging()
    {
        CreateComponent();
        
        var handlers = GetAllRegisteredLiveUpdateEventHandlers();
        var onlyHandler = handlers.Should().ContainSingle().Which;
        
        onlyHandler.EntityType.Should().Be(typeof(FormInstance));
        onlyHandler.EntityId.Should().Be(_formInstanceId);
    }
    
    [Fact]
    private void Rendered_ExistingAnswer_OnlyHandlerRegisteredIsForAnswerUpdating()
    {
        CreateComponent(alreadyExistingAnswerText: "Existing answer text");
        
        var handlers = GetAllRegisteredLiveUpdateEventHandlers();
        var onlyHandler = handlers.Should().ContainSingle().Which;
        
        onlyHandler.EntityType.Should().Be(typeof(TextAnswer));
        onlyHandler.EntityId.Should().Be(_answer.Id);
    }

    [Fact]
    private async Task LiveUpdateEventReceived_NewTextAnswerValue_ValueIsUpdatedCorrectly()
    {
        CreateComponent(alreadyExistingAnswerText: "Existing answer text");
        TextInput.GetAttribute("value").Should().Be("Existing answer text");
        
        var newAnswer = new TextAnswer()
        {
            Id = FakeDataGenerator.NextId,
            Answer = "A freshly updated answer text"
        };
        
        // Invoke the handler to simulate an EntityUpdate event being received for this answer
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<TextAnswer>(_answer.Id, LiveUpdateEventType.EntityUpdated);
        var changeEventHandler = liveUpdateHandler.GetTypedEntityUpdateHandler<TextAnswer>();
        await ComponentUnderTest.InvokeAsync(() => changeEventHandler.Invoke(newAnswer, ActingUser.Id));
        
        TextInput.GetAttribute("value").Should().Be("A freshly updated answer text");
    }
}