using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Inputs;

public class TaggedWorkInstanceInputComponentTest : TestContext
{
    private IRenderedComponent<TaggedWorkInstanceInput> _componentUnderTest;
    private readonly Mock<IWorklogTagService> _mockWorklogTagService = new(MockBehavior.Strict);

    private readonly WorklogTag _featureTag = FakeDataGenerator.CreateWorklogTag("Feature");
    private readonly WorklogTag _testTag = FakeDataGenerator.CreateWorklogTag("Test");

    private IElement TaggedWorklogInstancesContainer => _componentUnderTest.Find("#tagged-worklog-instance-container");
    
    private IElement WorklogTagSelector => _componentUnderTest.Find("#taggedWorkInstanceWorklogTagSelector");
    private IElement WorklogTagSelectorValidationMessage => _componentUnderTest.Find("#taggedWorkInstanceWorklogTagSelectorValidationMessage");

    private IRenderedComponent<InputDuration> DurationInput => _componentUnderTest.FindComponent<InputDuration>();
    private IElement DurationInputValidationMessage => _componentUnderTest.Find("#taggedWordInstanceDurationInputValidationMessage");

    private IElement RemoveWorkInstanceButtonForWorklogTag(ITag worklogTag) => _componentUnderTest.Find($"#removeTaggedWorkInstanceButton{worklogTag.Id}");

    private IElement EditForm => _componentUnderTest.Find("#taggedWorkInstanceInputEditForm");
    
    private readonly Mock<Action<ICollection<TaggedWorkInstanceForm>>> _valueChangedCallback = new();

    private void SetupComponent(ICollection<TaggedWorkInstanceForm> initialTaggedWorkInstanceForms=null)
    {
        _mockWorklogTagService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new[] { _featureTag, _testTag });

        Services.AddScoped(_ => _mockWorklogTagService.Object);
        
        _componentUnderTest = RenderComponent<TaggedWorkInstanceInput>(parameters => parameters
            .Add(x => x.TaggedWorkInstanceForms, initialTaggedWorkInstanceForms)
            .Add(x => x.TaggedWorkInstanceFormsChanged, _valueChangedCallback.Object)
        );
    }

    private void SelectWorklogTag(ITag worklogTag)
    {
        WorklogTagSelector.Change(worklogTag.Id);
    }
    
    private async Task SetDuration(TimeSpan duration)
    {
        await DurationInput.InvokeAsync(async () => await DurationInput.Instance.ValueChanged.InvokeAsync(duration));
    }
    
    [Fact]
    public void Rendered_NoInitialValueSet_OnlyLabelShown()
    {
        SetupComponent();
        TaggedWorklogInstancesContainer.Children.Should().ContainSingle(x => x.TextContent == "None yet created.");
    }
    
    [Fact]
    public void Rendered_InitialValueIsSet_ExistingWorkInstancesShown()
    {
        var initialWorkInstances = new[]
        {
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        };
        SetupComponent(initialWorkInstances);
        
        TaggedWorklogInstancesContainer.Children.Should().HaveCount(2);
    }
    
    [Fact]
    public void AddNewTaggedWorkInstance_NoWorklogTagSelected_ErrorMessageShown()
    {
        SetupComponent();
        
        EditForm.Submit();

        var expectedErrorText = typeof(TaggedWorkInstanceForm).GetErrorMessage<RangeAttribute>(nameof(TaggedWorkInstanceForm.WorklogTagId));
        WorklogTagSelectorValidationMessage.TextContent.Should().Be(expectedErrorText);
    }
    
    [Fact]
    public void AddNewTaggedWorkInstance_DurationEmpty_ErrorMessageShown()
    {
        SetupComponent();
        
        EditForm.Submit();
        
        DurationInputValidationMessage.TextContent.Should().Be("Must be no less than 1 minute");
    }
    
    [Fact]
    public async Task AddNewTaggedWorkInstance_DurationLessThanOneMinute_ErrorMessageShown()
    {
        SetupComponent();

        await SetDuration(TimeSpan.FromSeconds(5));
        await EditForm.SubmitAsync();
        
        DurationInputValidationMessage.TextContent.Should().Be("Must be no less than 1 minute");
    }
    
    [Fact]
    public async Task AddNewTaggedWorkInstance_DurationMoreThanOneDay_ErrorMessageShown()
    {
        SetupComponent();

        await SetDuration(TimeSpan.FromHours(25));
        await EditForm.SubmitAsync();
        
        DurationInputValidationMessage.TextContent.Should().Be("Must be no greater than 24 hours");
    }
    
    [Fact]
    public async Task AddNewTaggedWorkInstance_ValidValues_NewTaggedWorkInstanceShown()
    {
        SetupComponent();
        TaggedWorklogInstancesContainer.Children.Should().ContainSingle(x => x.TextContent == "None yet created.");

        var duration = TimeSpan.FromHours(1);
        await SetDuration(duration);
        SelectWorklogTag(_featureTag);
        await EditForm.SubmitAsync();

        TaggedWorklogInstancesContainer.FirstElementChild!.TextContent.Should().Contain(_featureTag.Name);
        TaggedWorklogInstancesContainer.FirstElementChild!.TextContent.Should().Contain($"Duration: {duration.ToString()}");
    }
    
    [Fact]
    public async Task AddNewTaggedWorkInstance_ValidValues_ValueChangedHandlerInvokedCorrectly()
    {
        SetupComponent();
        _valueChangedCallback.Verify(
            x => x(It.IsAny<ICollection<TaggedWorkInstanceForm>>()), 
            times: Times.Never
        );
        
        var duration = TimeSpan.FromHours(1);
        await SetDuration(duration);
        SelectWorklogTag(_featureTag);
        await EditForm.SubmitAsync();

        _valueChangedCallback.Verify(
            x => x(It.Is<ICollection<TaggedWorkInstanceForm>>(taggedWorkInstances =>
                taggedWorkInstances.First().WorklogTagId == _featureTag.Id 
                && taggedWorkInstances.First().Duration == duration)),
            times: Times.Once
        );
    }
    
    [Fact]
    public void RemoveTaggedWorkInstance_ValidRemoval_TaggedWorkInstanceNoLongerShown()
    {
        var initialWorkInstances = new[]
        {
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        };
        SetupComponent(initialWorkInstances);
        TaggedWorklogInstancesContainer.Children.Should().HaveCount(2);
        
        RemoveWorkInstanceButtonForWorklogTag(_featureTag).Click();

        TaggedWorklogInstancesContainer.Children.Should().ContainSingle();
        TaggedWorklogInstancesContainer.FirstElementChild!.TextContent.Should().Contain(_testTag.Name);
        TaggedWorklogInstancesContainer.FirstElementChild!.TextContent.Should().Contain($"Duration: {initialWorkInstances[1].Duration.ToString()}");
    }
    
    [Fact]
    public void RemoveTaggedWorkInstance_ValidRemoval_ValueChangedHandlerInvokedCorrectly()
    {
        var initialWorkInstances = new[]
        {
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_featureTag),
            FakeDataGenerator.CreateFakeTaggedWorkInstanceFormForDatabaseWorklogTag(_testTag)
        };
        SetupComponent(initialWorkInstances);
        
        RemoveWorkInstanceButtonForWorklogTag(_featureTag).Click();
        
        _valueChangedCallback.Verify(
            x => x(It.Is<ICollection<TaggedWorkInstanceForm>>(taggedWorkInstances =>
                taggedWorkInstances.First().WorklogTagId == _testTag.Id 
                && taggedWorkInstances.First().Duration == initialWorkInstances[1].Duration)),
            times: Times.Once
        );
    }
}