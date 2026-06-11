namespace TaskbarTransparency.Models;

public sealed class RuntimeSnapshot
{
    private const int MaxEvents = 8;

    public DateTimeOffset LastAppliedAt { get; set; } = DateTimeOffset.Now;
    public string State { get; set; } = "Desktop";
    public byte ResolvedOpacity { get; set; } = 30;
    public string OpacitySource { get; set; } = "Base opacity";
    public int TaskbarsUpdated { get; set; }
    public string LastMessage { get; set; } = "Ready";
    public List<RuntimeEvent> RecentEvents { get; } = [];
    public int RecentEventsVersion { get; private set; }

    public void RecordEvent(RuntimeEvent runtimeEvent)
    {
        RecentEvents.Insert(0, runtimeEvent);
        RecentEventsVersion++;

        if (RecentEvents.Count > MaxEvents)
        {
            RecentEvents.RemoveRange(MaxEvents, RecentEvents.Count - MaxEvents);
        }
    }
}

public sealed class RuntimeEvent
{
    public DateTimeOffset Time { get; init; }
    public string State { get; init; } = "Desktop";
    public string Source { get; init; } = "Base opacity";
    public byte Opacity { get; init; }
    public int TaskbarsUpdated { get; init; }
}
