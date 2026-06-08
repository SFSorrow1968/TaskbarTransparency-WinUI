namespace TaskbarTransparency.Models;

public sealed class RuntimeSnapshot
{
    private const int MaxEvents = 8;

    public DateTimeOffset LastAppliedAt { get; set; } = DateTimeOffset.Now;
    public string State { get; set; } = "Desktop";
    public string AppliedProfile { get; set; } = TaskbarProfile.OxygenClear.Name;
    public byte ResolvedOpacity { get; set; } = TaskbarProfile.OxygenClear.Opacity;
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
    public string Profile { get; init; } = TaskbarProfile.OxygenClear.Name;
    public byte Opacity { get; init; }
    public int TaskbarsUpdated { get; init; }
    public string Message { get; init; } = "Ready";
}
