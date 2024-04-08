using System;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Moq;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class LiveUpdateNotifierComponentTest : BaseProjectScopedComponentTestContext<LiveUpdateNotifier<IId>>
{
    /// <summary>
    /// Concrete type of this entity doesn't matter, so we just pick something like <see cref="AcceptanceCriteria"/>
    /// </summary>
    private readonly IId _entityBeingUpdated = FakeDataGenerator.CreateAcceptanceCriteria();

    private Func<IElement> GetEditingBegunDisplay => () => ComponentUnderTest.Find("#editing-started-display");
    private Func<IElement> GetEntityUpdatedDisplay => () => ComponentUnderTest.Find("#entity-updated-display");

    private readonly User _differentUser = FakeDataGenerator.CreateFakeUser();
    
    private async Task InvokeEditingBegunHandler(User editingUser)
    {
        var handlerRegistration = GetMostRecentLiveUpdateHandlerForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EditingBegunOnEntity);
        await ComponentUnderTest.InvokeAsync(() => handlerRegistration.GetEditingStatusChangedHandler().Invoke(editingUser.Id));
    }

    private async Task InvokeEditingEndedHandler(User editingUser)
    {
        var handlerRegistration = GetMostRecentLiveUpdateHandlerForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EditingEndedOnEntity);
        await ComponentUnderTest.InvokeAsync(() => handlerRegistration.GetEditingStatusChangedHandler().Invoke(editingUser.Id));
    }
    
    private async Task InvokeEntityUpdateHandler(User editingUser, IId newValue)
    {
        var handlerRegistration = GetMostRecentLiveUpdateHandlerForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EntityUpdated);
        await ComponentUnderTest.InvokeAsync(() => handlerRegistration.GetTypedEntityUpdateHandler<IId>().Invoke(newValue, editingUser.Id));
    }

    private void CreateComponent()
    {
        UserRepositoryMock.Setup(x => x.GetByIdAsync(ActingUser.Id)).ReturnsAsync(ActingUser);
        UserRepositoryMock.Setup(x => x.GetByIdAsync(_differentUser.Id)).ReturnsAsync(_differentUser);
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.EntityId, _entityBeingUpdated.Id)
        );
    }

    [Fact]
    public void Rendered_ParametersSet_EntityUpdateHandlerRegisteredOnce()
    {
        CreateComponent();

        var entityUpdatedListeners = GetLiveUpdateEventsForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EntityUpdated);

        entityUpdatedListeners.Should().ContainSingle();
    }

    [Fact]
    public void Rendered_ParametersSet_ListenerForUpdateBegunRegisteredOnce()
    {
        CreateComponent();

        var editingBegunListeners = GetLiveUpdateEventsForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EditingBegunOnEntity);

        editingBegunListeners.Should().ContainSingle();
    }

    [Fact]
    public void Rendered_ParametersSet_ListenerForUpdateEndedRegisteredOnce()
    {
        CreateComponent();

        var editingEndedListeners = GetLiveUpdateEventsForEntity<IId>(_entityBeingUpdated.Id, LiveUpdateEventType.EditingEndedOnEntity);

        editingEndedListeners.Should().ContainSingle();
    }

    [Fact]
    public async Task EditingBegunByCurrentUser_FirstEvent_NothingShown()
    {
        CreateComponent();

        await InvokeEditingBegunHandler(ActingUser);

        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }
    
    [Fact]
    public async Task EditingBegunByDifferentUser_FirstEvent_EditingBegunDisplayShownCorrectly()
    {
        CreateComponent();

        await InvokeEditingBegunHandler(_differentUser);

        GetEntityUpdatedDisplay.Should().Throw<ElementNotFoundException>();
        GetEditingBegunDisplay().TextContent.Should().Contain($"{_differentUser.GetFullName()} is editing");
    }
    
    [Fact]
    public async Task EditingStoppedByCurrentUser_FirstEvent_NothingDisplayed()
    {
        CreateComponent();

        await InvokeEditingEndedHandler(ActingUser);

        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }
    
    [Fact]
    public async Task EditingStoppedByDifferentUser_FirstEvent_NothingDisplayed()
    {
        CreateComponent();

        await InvokeEditingEndedHandler(_differentUser);

        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }
    
    [Fact]
    public async Task EntityUpdatedByCurrentUser_FirstEvent_NothingDisplayed()
    {
        CreateComponent();

        await InvokeEntityUpdateHandler(ActingUser, _entityBeingUpdated);

        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }
    
    [Theory]
    [InlineData(0, "just now")]
    [InlineData(5, "just now")]
    [InlineData(9.9, "just now")]
    [InlineData(10, "10s ago")]
    [InlineData(59.9, "59s ago")]
    [InlineData(60, "1m ago")]
    [InlineData(65, "1m ago")]
    [InlineData(60 * 60, "1h ago")]
    [InlineData(60 * 60 + 5, "1h ago")]
    [InlineData(60 * 60 * 24, "1d ago")]
    [InlineData(60 * 60 * 24 + 5, "1d ago")]
    public async Task EntityUpdatedByDifferentUser_FirstEvent_EntityUpdateDisplayShownCorrectly(double secondsSinceUpdate, string expectedTimeString)
    {
        CreateComponent();

        var startTime = DateTime.Now;
        ClockMock.Setup(x => x.Now).Returns(startTime);
        await InvokeEntityUpdateHandler(_differentUser, _entityBeingUpdated);

        ClockMock.Setup(x => x.Now).Returns(startTime.Add(TimeSpan.FromSeconds(secondsSinceUpdate)));
        ComponentUnderTest.Render();
        
        GetEditingBegunDisplay.Should().Throw<ElementNotFoundException>();
        GetEntityUpdatedDisplay().TextContent.Should().Contain($"Updated by {_differentUser.GetFullName()}");
        GetEntityUpdatedDisplay().TextContent.Should().EndWith(expectedTimeString);
    }

    [Theory]
    [EnumData(typeof(LiveUpdateEventType))]
    public async Task EditingBegun_AfterAnyEvent_EditingBegunDisplayShown(LiveUpdateEventType eventType)
    {
        CreateComponent();
        
        switch (eventType)
        {
            case LiveUpdateEventType.EntityUpdated:
                await InvokeEntityUpdateHandler(_differentUser, _entityBeingUpdated);
                break;
            case LiveUpdateEventType.EditingBegunOnEntity:
                await InvokeEditingBegunHandler(_differentUser);
                break;
            case LiveUpdateEventType.EditingEndedOnEntity:
                await InvokeEditingEndedHandler(_differentUser);
                break;
            case LiveUpdateEventType.ConnectionError:
            case LiveUpdateEventType.ConnectionSuccess:
            case LiveUpdateEventType.EntityHasChanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
        }

        await InvokeEditingBegunHandler(_differentUser);

        GetEditingBegunDisplay.Should().NotThrow();
        GetEntityUpdatedDisplay.Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    public async Task EditingEnded_AfterEditingBegun_NothingDisplayed()
    {
        CreateComponent();
        
        await InvokeEditingBegunHandler(_differentUser);
        await InvokeEditingEndedHandler(_differentUser);

        ComponentUnderTest.Markup.Trim().Should().BeEmpty();
    }
    
    [Fact]
    public async Task EditingEnded_AfterEntityUpdated_EntityUpdateDisplayStillShown()
    {
        CreateComponent();
        
        await InvokeEntityUpdateHandler(_differentUser, _entityBeingUpdated);
        await InvokeEditingEndedHandler(_differentUser);

        GetEditingBegunDisplay.Should().Throw<ElementNotFoundException>();
        GetEntityUpdatedDisplay.Should().NotThrow();
    }

    [Theory]
    [EnumData(typeof(LiveUpdateEventType))]
    public async Task EntityUpdated_AfterAnyEvent_EntityUpdateDisplayShown(LiveUpdateEventType eventType)
    {
        CreateComponent();
        
        switch (eventType)
        {
            case LiveUpdateEventType.EntityUpdated:
                await InvokeEntityUpdateHandler(_differentUser, _entityBeingUpdated);
                break;
            case LiveUpdateEventType.EditingBegunOnEntity:
                await InvokeEditingBegunHandler(_differentUser);
                break;
            case LiveUpdateEventType.EditingEndedOnEntity:
                await InvokeEditingEndedHandler(_differentUser);
                break;
            case LiveUpdateEventType.ConnectionError:
            case LiveUpdateEventType.ConnectionSuccess:
            case LiveUpdateEventType.EntityHasChanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
        }

        await InvokeEntityUpdateHandler(_differentUser, _entityBeingUpdated);

        GetEditingBegunDisplay.Should().Throw<ElementNotFoundException>();
        GetEntityUpdatedDisplay.Should().NotThrow();
    }
}