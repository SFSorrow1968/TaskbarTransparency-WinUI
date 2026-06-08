namespace TaskbarTransparency.Models;

public sealed record DiagnosticsRecoveryState(
    string Title,
    string PrimaryDetail,
    string SecondaryDetail,
    string TertiaryDetail,
    string PrimaryAction,
    string SecondaryAction,
    string HelperText,
    bool ShowSecondaryAction)
{
    public static DiagnosticsRecoveryState FromTaskbarUpdateCount(int taskbarsUpdated)
    {
        if (taskbarsUpdated > 0)
        {
            return new DiagnosticsRecoveryState(
                "System health",
                "Taskbar windows are detected and responding.",
                "Recent automation results are recorded in the sensor timeline below.",
                "Use a simulation action only when you want to verify a specific runtime state.",
                "Recheck now",
                "Apply safe desktop",
                "Rechecks detection and records a fresh diagnostic result without changing your saved settings.",
                false);
        }

        return new DiagnosticsRecoveryState(
            "Probable causes",
            "Windows Explorer is restarting or has not created the taskbar yet.",
            "An unsupported shell replacement is in use.",
            "The target taskbar is hidden, disabled, or unavailable.",
            "Retry detection",
            "Apply safe desktop",
            "Applies the desktop rule and tries again without changing your saved settings.",
            true);
    }
}
