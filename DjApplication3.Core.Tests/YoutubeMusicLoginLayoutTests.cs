using System.Xml.Linq;

namespace DjApplication3.Core.Tests;

public sealed class YoutubeMusicLoginLayoutTests
{
    [Fact]
    public void LoginPanel_IsHostedByFiniteGridInsteadOfStackPanel()
    {
        var xamlPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestInputs",
            "MainView.xaml");
        var document = XDocument.Load(xamlPath);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var loginPanel = document
            .Descendants()
            .Single(element => (string?)element.Attribute(xaml + "Name") == "LoginPanel");
        Assert.Equal("Grid", loginPanel.Parent?.Name.LocalName);
    }
}
