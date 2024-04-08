using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback.Response;
using ScrumBoard.Services;
using ScrumBoard.Shared.FormResponseComponents;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormSubmission;

public class FormBlockMultiChoiceQuestionResponseTest : BaseProjectScopedComponentTestContext<FormBlockMultiChoiceQuestionResponse>
{
    private readonly Mock<IFormInstanceService> _formInstanceServiceMock = new ();

    private MultiChoiceQuestion _question;
    private readonly MultiChoiceOption _firstOption = new() { Id = FakeDataGenerator.NextId, Content = "Option 1" };
    private readonly MultiChoiceOption _secondOption = new() { Id = FakeDataGenerator.NextId, Content = "Option 2" };

    private MultiChoiceAnswer _answer;

    private MultiChoiceAnswerForm _answerForm;

    private long _formInstanceId;

    private IRenderedComponent<InputRadio<int>> InputRadioForOption(IId option) => ComponentUnderTest
        .FindComponents<InputRadio<int>>()
        .First(x => (string)x.Instance.AdditionalAttributes!["id"] == $"multi-choice-option-{option.Id.ToString()}");
    
    private Func<IElement> GetSavingText => () => ComponentUnderTest.WaitForElement("#saving-text");
    private Func<IElement> GetSavedText => () => ComponentUnderTest.WaitForElement("#saved-text");
    private Func<IElement> GetUnsavedText => () => ComponentUnderTest.WaitForElement("#unsaved-text");

    private Func<IElement> GetSaveErrorMessage => () => ComponentUnderTest.WaitForElement("#save-error-text");
    
    private void CreateComponent(MultiChoiceOption alreadySelectedOption = null)
    {
        Services.AddScoped(_ => _formInstanceServiceMock.Object);

        _formInstanceId = FakeDataGenerator.NextId;
        _question = new MultiChoiceQuestion
        {
            Prompt = "Question prompt",
            Options = [ _firstOption, _secondOption ]
        };

        _answerForm = new MultiChoiceAnswerForm(_question, null);
        var editContext = new EditContext(_answerForm);

        if (alreadySelectedOption is not null)
        {
            _answer = new MultiChoiceAnswer
            {
                Id = FakeDataGenerator.NextId,
                SelectedOptions =
                [
                    new MultichoiceAnswerMultichoiceOption
                    {
                        Id = FakeDataGenerator.NextId,
                        MultichoiceOption = alreadySelectedOption,
                        MultichoiceOptionId = alreadySelectedOption.Id
                    }
                ]
            };
            _formInstanceServiceMock
                .Setup(x => x.GetAnswerByFormInstanceAndQuestionIdAsync(_formInstanceId, _question.Id))
                .ReturnsAsync(_answer);
        }
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.FormInstanceId, _formInstanceId)
            .Add(x => x.MultiChoiceQuestion, _question)
            .Add(x => x.MultiChoiceAnswerForm, _answerForm)
            .AddCascadingValue(editContext)
        );
    }
    
    private void SetRadioSelection(MultiChoiceOption option)
    {
        ComponentUnderTest.InvokeAsync(async () =>
        {
            // Get the index of the selected option
            var index = _question.Options
                .Select((o, i) => new { Option = o, Index = i })
                .FirstOrDefault(x => x.Option == option)?.Index ?? -1;

            await ComponentUnderTest
                .FindComponent<InputRadioGroup<int?>>()
                .Instance.ValueChanged
                .InvokeAsync(index);
        });
    }
    
    [Fact]
    private void SelectOption_SavesSuccessfully_SaveStatusIndicatorShowsSavingThenSaved()
    {
        // Configure the service method to not finish until we explicitly tell it to
        var saveCompletionSource = new TaskCompletionSource();
        _formInstanceServiceMock
            .Setup(x => x.SaveAnswerToMultiChoiceFormBlock(
                It.IsAny<long>(), 
                It.IsAny<long>(), 
                It.IsAny<List<long>>(), 
                It.IsAny<long>(),
                It.IsAny<bool>()))
            .Returns(saveCompletionSource.Task);
        CreateComponent();
        
        SetRadioSelection(_firstOption);
        GetSavingText.Should().NotThrow();

        // Tell the service method to finish
        saveCompletionSource.SetResult();

        GetSavedText.Should().NotThrow();
    }
    
    [Fact]
    private void SelectOption_FormAlreadySubmitted_ErrorShownAndSaveStatusIndicatorShowsUnsaved()
    {
        _formInstanceServiceMock
            .Setup(x => x.SaveAnswerToMultiChoiceFormBlock(
                It.IsAny<long>(), 
                It.IsAny<long>(), 
                It.IsAny<List<long>>(), 
                It.IsAny<long>(),
                It.IsAny<bool>()))
            .Throws(new InvalidOperationException());
        CreateComponent();

        SetRadioSelection(_firstOption);
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
        CreateComponent(alreadySelectedOption: _firstOption);
        
        var handlers = GetAllRegisteredLiveUpdateEventHandlers();
        var onlyHandler = handlers.Should().ContainSingle().Which;
        
        onlyHandler.EntityType.Should().Be(typeof(MultiChoiceAnswer));
        onlyHandler.EntityId.Should().Be(_answer.Id);
    }

    [Fact]
    private async Task LiveUpdateEventReceived_NewValueOfSelection_ValueIsUpdatedCorrectly()
    {
        CreateComponent(alreadySelectedOption: _firstOption);
        // Check that first option is selected, and second isn't
        InputRadioForOption(_firstOption).Markup.Should().Contain("checked");
        InputRadioForOption(_secondOption).Markup.Should().NotContain("checked");
        
        var newAnswer = new MultiChoiceAnswer
        {
            Id = FakeDataGenerator.NextId,
            SelectedOptions =
            [
                new MultichoiceAnswerMultichoiceOption
                {
                    Id = FakeDataGenerator.NextId,
                    MultichoiceOption = _secondOption,
                    MultichoiceOptionId = _secondOption.Id
                }
            ]
        };
        
        // Invoke the handler to simulate an EntityUpdate event being received for this answer
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<MultiChoiceAnswer>(_answer.Id, LiveUpdateEventType.EntityUpdated);
        var changeEventHandler = liveUpdateHandler.GetTypedEntityUpdateHandler<MultiChoiceAnswer>();
        await ComponentUnderTest.InvokeAsync(() => changeEventHandler.Invoke(newAnswer, ActingUser.Id));
        
        // Now the second option should be selected, and the first should not 
        InputRadioForOption(_firstOption).Markup.Should().NotContain("checked");
        InputRadioForOption(_secondOption).Markup.Should().Contain("checked");
    }
}