using Xunit;
using Virabis.Core.Nexus;
using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus.Tests;

/// <summary>
/// Unit tests for EventBus class (legacy - use EventChannel for new code)
/// </summary>
public class EventBusTests
{
    [Fact]
    public void EventBus_StillExists_ForBackwardCompatibility()
    {
        var eventBus = EventBus.Instance;
        Assert.NotNull(eventBus);
    }
}
