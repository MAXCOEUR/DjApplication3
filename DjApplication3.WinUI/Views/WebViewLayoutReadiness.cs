namespace DjApplication3.WinUI.Views;

public static class WebViewLayoutReadiness
{
    public static bool IsRenderable(bool isLoaded, double width, double height)
        => isLoaded && width > 0 && height > 0;

    public static bool PrepareLayout(Func<bool> isRenderable, Action updateParentLayout)
    {
        if (isRenderable())
        {
            return true;
        }

        updateParentLayout();
        return isRenderable();
    }
}
