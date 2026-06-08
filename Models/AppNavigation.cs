namespace TaskbarTransparency.Models;

public static class AppNavigation
{
    public static bool ShouldNavigate(Type? currentPageType, Type requestedPageType)
    {
        return currentPageType != requestedPageType;
    }
}
