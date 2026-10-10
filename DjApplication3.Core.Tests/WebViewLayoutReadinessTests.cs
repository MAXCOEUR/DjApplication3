using DjApplication3.WinUI.Views;

namespace DjApplication3.Core.Tests;

public sealed class WebViewLayoutReadinessTests
{
    [Theory]
    [InlineData(false, 400, 300, false)]
    [InlineData(true, 0, 300, false)]
    [InlineData(true, 400, 0, false)]
    [InlineData(true, 400, 300, true)]
    public void IsRenderable_RequiresLoadedControlWithNonZeroLayout(
        bool isLoaded,
        double width,
        double height,
        bool expected)
    {
        Assert.Equal(expected, WebViewLayoutReadiness.IsRenderable(isLoaded, width, height));
    }

    [Fact]
    public void PrepareLayout_ForcesParentLayoutBeforeRecheckingHost()
    {
        var isRenderable = false;
        var updateLayoutCalls = 0;

        var result = WebViewLayoutReadiness.PrepareLayout(
            () => isRenderable,
            () =>
            {
                updateLayoutCalls++;
                isRenderable = true;
            });

        Assert.True(result);
        Assert.Equal(1, updateLayoutCalls);
    }
}
