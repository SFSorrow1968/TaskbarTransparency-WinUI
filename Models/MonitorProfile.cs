namespace TaskbarTransparency.Models;

public sealed class MonitorProfile
{
    public string DeviceName { get; set; } = "Primary display";
    public string FriendlyName { get; set; } = "Primary display";
    public bool IsPrimary { get; set; } = true;
    public bool SyncWithPrimary { get; set; } = true;
    public byte OverrideOpacity { get; set; } = 32;

    public static MonitorProfile MergeDetected(MonitorProfile detected, MonitorProfile? saved)
    {
        return new MonitorProfile
        {
            DeviceName = detected.DeviceName,
            FriendlyName = detected.FriendlyName,
            IsPrimary = detected.IsPrimary,
            SyncWithPrimary = saved?.SyncWithPrimary ?? detected.SyncWithPrimary,
            OverrideOpacity = saved?.OverrideOpacity ?? detected.OverrideOpacity
        };
    }

    public static bool SequenceMatches(IEnumerable<MonitorProfile> left, IEnumerable<MonitorProfile> right)
    {
        using var leftItems = left.GetEnumerator();
        using var rightItems = right.GetEnumerator();
        while (true)
        {
            var hasLeft = leftItems.MoveNext();
            var hasRight = rightItems.MoveNext();
            if (hasLeft != hasRight)
            {
                return false;
            }

            if (!hasLeft)
            {
                return true;
            }

            if (!Matches(leftItems.Current, rightItems.Current))
            {
                return false;
            }
        }
    }

    private static bool Matches(MonitorProfile left, MonitorProfile right)
    {
        return string.Equals(left.DeviceName, right.DeviceName, StringComparison.Ordinal)
            && string.Equals(left.FriendlyName, right.FriendlyName, StringComparison.Ordinal)
            && left.IsPrimary == right.IsPrimary
            && left.SyncWithPrimary == right.SyncWithPrimary
            && left.OverrideOpacity == right.OverrideOpacity;
    }
}
