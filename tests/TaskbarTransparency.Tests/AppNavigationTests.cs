using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class AppNavigationTests
{
    [Fact]
    public void ShouldNavigate_SkipsWhenRequestedPageIsAlreadyActive()
    {
        Assert.False(AppNavigation.ShouldNavigate(typeof(CurrentPage), typeof(CurrentPage)));
    }

    [Fact]
    public void ShouldNavigate_AllowsFirstNavigationAndPageChanges()
    {
        Assert.True(AppNavigation.ShouldNavigate(null, typeof(CurrentPage)));
        Assert.True(AppNavigation.ShouldNavigate(typeof(CurrentPage), typeof(NextPage)));
    }

    private sealed class CurrentPage;
    private sealed class NextPage;
}
