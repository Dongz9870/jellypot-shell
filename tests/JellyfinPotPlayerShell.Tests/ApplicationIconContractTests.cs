namespace JellyfinPotPlayerShell.Tests;

public sealed class ApplicationIconContractTests
{
    [Fact]
    public void PinkIcon_IsAValidHighResolutionIcoAsset()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "JellyPot.ico");
        Assert.True(File.Exists(path), $"缺少应用图标：{path}");

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 6);
        Assert.Equal((byte)0, bytes[0]);
        Assert.Equal((byte)0, bytes[1]);
        Assert.Equal((byte)1, bytes[2]);
        Assert.Equal((byte)0, bytes[3]);
        Assert.True(BitConverter.ToUInt16(bytes, 4) >= 1);
        Assert.Equal((byte)0, bytes[6]);
        Assert.Equal((byte)0, bytes[7]);
    }

    [Fact]
    public void Project_EmbedsIconInExecutable()
    {
        var project = ReadContractFile("JellyfinPotPlayerShell.App.csproj");

        Assert.Contains(
            "<ApplicationIcon>Assets\\JellyPot.ico</ApplicationIcon>",
            project);
        Assert.Contains(
            "<Resource Include=\"Assets\\JellyPot.ico\" />",
            project);
    }

    [Fact]
    public void Application_AppliesIconToEveryWindow()
    {
        var application = ReadContractFile("App.xaml");

        Assert.Contains("x:Key=\"AppIcon\"", application);
        Assert.Contains(
            "JellyfinPotPlayerShell;component/Assets/JellyPot.ico",
            application);
        Assert.Contains("<Style TargetType=\"Window\">", application);
        Assert.Contains(
            "<Setter Property=\"Icon\" Value=\"{StaticResource AppIcon}\" />",
            application);
    }

    private static string ReadContractFile(string fileName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "IconContract",
            fileName);
        Assert.True(File.Exists(path), $"缺少图标契约资源：{path}");
        return File.ReadAllText(path);
    }
}
