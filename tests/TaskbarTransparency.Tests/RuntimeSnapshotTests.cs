using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class RuntimeSnapshotTests
{
    [Fact]
    public void RecordEvent_KeepsMostRecentEightEvents()
    {
        var snapshot = new RuntimeSnapshot();

        for (var index = 0; index < 10; index++)
        {
            snapshot.RecordEvent(new RuntimeEvent
            {
                Time = DateTimeOffset.Now.AddSeconds(index),
                State = $"State {index}",
                Source = "Base opacity",
                Opacity = (byte)index,
                TaskbarsUpdated = 1
            });
        }

        Assert.Equal(8, snapshot.RecentEvents.Count);
        Assert.Equal("State 9", snapshot.RecentEvents[0].State);
        Assert.Equal("State 2", snapshot.RecentEvents[^1].State);
    }

    [Fact]
    public void RecordEvent_IncrementsRecentEventsVersion()
    {
        var snapshot = new RuntimeSnapshot();

        snapshot.RecordEvent(new RuntimeEvent { State = "Desktop" });
        snapshot.RecordEvent(new RuntimeEvent { State = "Hover" });

        Assert.Equal(2, snapshot.RecentEventsVersion);
    }
}
