using System.Xml.Linq;

namespace DjApplication3.Core.Tests;

public sealed class WinUiHostingConfigurationTests
{
    [Fact]
    public void WinUiApplication_UsesWindowsExecutableEntryPoint()
    {
        var projectPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestInputs",
            "DjApplication3.WinUI.csproj");
        var document = XDocument.Load(projectPath);
        var outputType = document.Descendants("OutputType").Single().Value;

        Assert.Equal("WinExe", outputType);
    }
}
