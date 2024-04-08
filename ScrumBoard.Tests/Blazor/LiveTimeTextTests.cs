using System;
using System.Threading.Tasks;
using System.Timers;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Utils;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Blazor;

public class LiveTimeTextTests : TestContext
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Func<DateTime, DateTime, string> _timeFormatFunc = (now, target) => $"{(now - target).TotalMilliseconds}";

    public LiveTimeTextTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private Mock<IClock> SetupMocks()
    {
        var clockMock = new Mock<IClock>();
        
        Services.AddSingleton(clockMock.Object);

        return clockMock;
    }

    private IRenderedComponent<LiveTimeText> RenderLiveTimeText(DateTime targetTime, EventCallback callback, int refreshPeriodInMilliseconds)
    {
        return RenderComponent<LiveTimeText>(parameters => parameters
            .Add(p => p.RefreshPeriodInMilliseconds, refreshPeriodInMilliseconds)
            .Add(p => p.TargetTime, targetTime)
            .Add(p => p.OnTimerPassesNow, callback)
            .Add(p => p.DateTimeFormatFunc, _timeFormatFunc)
        );
    }
    
    [Fact]
    public void LiveTimeText_ShowsTimeCorrectlyOnFirstRender()
    {
        var targetTime = DateTime.Now.AddHours(1);
        var clockMock = SetupMocks();
        
        var cut = RenderLiveTimeText(targetTime, EventCallback.Empty, 50);
        cut.MarkupMatches(_timeFormatFunc(clockMock.Object.Now, targetTime));
    }
    
    [Fact]
    public void LiveTimeText_ShowsTimeCorrectlyOnSubsequentRenders()
    {
        var targetTime = DateTime.Now.AddHours(1);
        var currentTime = DateTime.Now.AddHours(-1);
        
        var clockMock = SetupMocks();
        clockMock.Setup(c => c.Now).Returns(currentTime);
        var cut = RenderLiveTimeText(targetTime, EventCallback.Empty, 50);
        
        cut.MarkupMatches(_timeFormatFunc(currentTime, targetTime));

        for (var i = 0; i < 10; i++)
        {
            currentTime = currentTime.AddMinutes(1).AddSeconds(1);
            clockMock.Setup(c => c.Now).Returns(currentTime);
            var time = currentTime;
            cut.WaitForAssertion(() => cut.MarkupMatches(_timeFormatFunc(time, targetTime)));
            _testOutputHelper.WriteLine(cut.Markup);
        }
    }

    [Fact]
    public void LiveTimeText_TimerPassesNow_ActionIsCalled()
    {
        var targetTime = DateTime.Now.AddHours(1);
        var isCalled = false;
        
        var clockMock = SetupMocks();

        clockMock.Setup(c => c.Now).Returns(targetTime.AddSeconds(-10));
        var cut = RenderLiveTimeText(targetTime, EventCallback.Factory.Create(this, () => isCalled = true), 50);
        
        // Wait until the display has refreshed at least once
        var initialMarkup = cut.Markup;
        clockMock.Setup(c => c.Now).Returns(targetTime.AddSeconds(-9));
        cut.WaitForState(() => cut.Markup != initialMarkup);
        isCalled.Should().BeFalse();

        // Now set the system time to after the target event, and give some time for the event to trigger
        clockMock.Setup(c => c.Now).Returns(targetTime.AddSeconds(10));
        cut.WaitForAssertion(() => isCalled.Should().BeTrue(), TimeSpan.FromHours(1));
    }
}
