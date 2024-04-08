using Xunit;

namespace ScrumBoard.Tests.Integration.LiveUpdating;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public class LiveUpdateIsolationCollection
{
    public const string CollectionName = "Live Update Tests";
}