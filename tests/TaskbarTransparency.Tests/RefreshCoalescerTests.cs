using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class RefreshCoalescerTests
{
    [Fact]
    public void Request_CoalescesDuplicateRefreshesWhilePending()
    {
        var coalescer = new RefreshCoalescer();
        var queued = new List<Action>();
        var refreshes = 0;

        Assert.True(coalescer.Request(action =>
        {
            queued.Add(action);
            return true;
        }, () => refreshes++));
        Assert.False(coalescer.Request(action =>
        {
            queued.Add(action);
            return true;
        }, () => refreshes++));

        Assert.Single(queued);
        queued[0]();
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public void Request_AllowsNextRefreshAfterQueuedWorkRuns()
    {
        var coalescer = new RefreshCoalescer();
        var queued = new List<Action>();

        Assert.True(coalescer.Request(action =>
        {
            queued.Add(action);
            return true;
        }, () => { }));

        queued[0]();

        Assert.True(coalescer.Request(action =>
        {
            queued.Add(action);
            return true;
        }, () => { }));
        Assert.Equal(2, queued.Count);
    }

    [Fact]
    public void Request_ClearsPendingFlagWhenEnqueueFails()
    {
        var coalescer = new RefreshCoalescer();

        Assert.False(coalescer.Request(_ => false, () => { }));
        Assert.True(coalescer.Request(_ => true, () => { }));
    }
}
