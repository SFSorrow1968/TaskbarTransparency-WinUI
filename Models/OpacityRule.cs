namespace TaskbarTransparency.Models;

public sealed class OpacityRule
{
    public bool Enabled { get; set; }
    public byte Opacity { get; set; } = 100;

    public void Normalize()
    {
        Opacity = Math.Clamp(Opacity, (byte)0, (byte)100);
    }
}
