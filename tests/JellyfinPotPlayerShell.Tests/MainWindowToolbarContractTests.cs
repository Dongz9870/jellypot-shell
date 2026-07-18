namespace JellyfinPotPlayerShell.Tests;

public sealed class MainWindowToolbarContractTests
{
    [Fact]
    public void Toolbar_UsesFluentRefreshAndSettingsIconButtons()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("x:Key=\"ToolbarIconButton\"", xaml);
        Assert.Contains("Segoe Fluent Icons", xaml);
        Assert.Contains("Content=\"&#xE72C;\"", xaml);
        Assert.Contains("Content=\"&#xE713;\"", xaml);
        Assert.Contains("ToolTip=\"刷新\"", xaml);
        Assert.Contains("ToolTip=\"设置\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"刷新\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"设置\"", xaml);
        Assert.DoesNotContain("Content=\"刷新\"", xaml);
        Assert.DoesNotContain("Content=\"设置\"", xaml);
    }

    [Fact]
    public void ToolbarIconButton_HasPointerAndKeyboardFeedback()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("Property=\"IsMouseOver\"", xaml);
        Assert.Contains("Property=\"IsPressed\"", xaml);
        Assert.Contains("Property=\"IsKeyboardFocused\"", xaml);
        Assert.Contains("Property=\"Cursor\" Value=\"Hand\"", xaml);
    }

    private static string ReadMainWindowXaml()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "UiContract",
            "MainWindow.xaml");
        Assert.True(File.Exists(path), $"缺少主窗口 UI 契约资源：{path}");
        return File.ReadAllText(path);
    }
}
