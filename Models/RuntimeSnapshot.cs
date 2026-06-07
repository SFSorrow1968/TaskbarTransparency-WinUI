namespace TaskbarTransparency.Models;

public sealed class RuntimeSnapshot
{
    public DateTimeOffset LastAppliedAt { get; set; } = DateTimeOffset.Now;
    public string State { get; set; } = "Desktop";
    public string AppliedProfile { get; set; } = TaskbarProfile.OxygenClear.Name;
    public int TaskbarsUpdated { get; set; }
    public string LastMessage { get; set; } = "Ready";
}
