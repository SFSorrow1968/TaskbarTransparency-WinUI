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
}
