namespace TaskbarTransparency.Services;

public sealed class RefreshCoalescer
{
    private bool _refreshPending;

    public bool Request(Func<Action, bool> enqueue, Action refresh)
    {
        if (_refreshPending)
        {
            return false;
        }

        _refreshPending = true;
        if (!enqueue(() =>
        {
            _refreshPending = false;
            refresh();
        }))
        {
            _refreshPending = false;
            return false;
        }

        return true;
    }
}
