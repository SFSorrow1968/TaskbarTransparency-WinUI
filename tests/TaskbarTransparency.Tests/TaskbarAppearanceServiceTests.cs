using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class TaskbarAppearanceServiceTests
{
    [Fact]
    public void ComposeColorForTest_UsesAbgrOrderingExpectedByAccentPolicy()
    {
        var color = TaskbarAppearanceService.ComposeColorForTest("#112233", 50);

        Assert.Equal(unchecked((int)0x7F332211), color);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(32, 81)]
    [InlineData(50, 127)]
    [InlineData(100, 255)]
    public void ConvertOpacityToAlphaForTest_MapsPercentToLayeredWindowAlpha(byte opacity, byte expected)
    {
        var alpha = TaskbarAppearanceService.ConvertOpacityToAlphaForTest(opacity);

        Assert.Equal(expected, alpha);
    }
}
