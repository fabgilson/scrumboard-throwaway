using System;
using FluentAssertions;

namespace ScrumBoard.Tests.Unit.Utils;

public class AssertionHelper
{
    public static void WaitFor(Action assertionAction)
    {
        assertionAction.Should().NotThrowAfter(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
    }
}