using System;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace IdentityProvider.Tests;

public static class GlobalFluentAssertionsConfig
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void ConfigureCustomAssertionsConfig()
    {
        AssertionOptions.AssertEquivalencyUsing(AllowCloseToTime());
    }
    
    /// <summary>
    /// Used for configuring FluentAssertions to allow two datetime objects to be considered equivalent as long as
    /// they are no more than 1 second apart. Commonly used when comparing object that have been serialized / deserialized
    /// such as when an entity has been transmitted over gRPC.
    /// </summary>
    /// <returns>Function for configuring fluent assertions to accept sufficiently similar DateTimes as equivalent</returns>
    private static Func<EquivalencyAssertionOptions, EquivalencyAssertionOptions> AllowCloseToTime()
    {
        return options =>
        {
            options.Using<DateTime>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1))).WhenTypeIs<DateTime>();
            options.Using<DateTimeOffset>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1))).WhenTypeIs<DateTimeOffset>();
            return options;
        };
    }
}