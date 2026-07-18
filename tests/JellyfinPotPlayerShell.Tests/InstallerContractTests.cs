namespace JellyfinPotPlayerShell.Tests;

public sealed class InstallerContractTests
{
    [Fact]
    public void InnoSetup_ProvidesInstallUninstallAndShortcuts()
    {
        var script = ReadInstallerFile("JellyfinPotPlayerShell.iss");

        Assert.Contains("PrivilegesRequired=lowest", script);
        Assert.Contains("ArchitecturesAllowed=x64compatible", script);
        Assert.Contains("{localappdata}\\Programs", script);
        Assert.Contains("{autodesktop}", script);
        Assert.Contains("{uninstallexe}", script);
        Assert.Contains("postinstall", script);
        Assert.Contains("Excludes: \"*.pdb,*.xml\"", script);
        Assert.Contains("Source: \"appsettings.example.json\"", script);
        Assert.Contains(
            "SetupIconFile=..\\src\\JellyfinPotPlayerShell.App\\Assets\\JellyPot.ico",
            script);
        Assert.DoesNotContain("PotPlayerMini64.exe", script);
        Assert.DoesNotContain("qBittorrent", script);
    }

    [Fact]
    public void InnoSetup_ChecksAndBootstrapsWebView2Runtime()
    {
        var script = ReadInstallerFile("JellyfinPotPlayerShell.iss");

        Assert.Contains(
            "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
            script,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegQueryStringValue", script);
        Assert.Contains("MicrosoftEdgeWebview2Setup.exe", script);
        Assert.Contains("/silent /install", script);
        Assert.Contains("Flags: dontcopy", script);
    }

    [Fact]
    public void BuildScript_PublishesSelfContainedAndVerifiesInputs()
    {
        var script = ReadInstallerFile("build-installer.ps1");

        Assert.Contains("dotnet publish", script);
        Assert.Contains("--self-contained true", script);
        Assert.Contains("PublishSingleFile=false", script);
        Assert.Contains("Get-AuthenticodeSignature", script);
        Assert.Contains("WebView2Loader.dll", script);
        Assert.Contains("ISCC.exe", script);
        Assert.Contains("Reset-ArtifactDirectory", script);
    }

    [Fact]
    public void InstallerConfigurationExample_IsValidJson()
    {
        var json = ReadInstallerFile("appsettings.example.json");
        using var document = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal(
            "http://127.0.0.1:8096",
            document.RootElement
                .GetProperty("Jellyfin")
                .GetProperty("ServerUrl")
                .GetString());
    }

    private static string ReadInstallerFile(string fileName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Installer",
            fileName);
        Assert.True(File.Exists(path), $"缺少安装器测试资源：{path}");
        return File.ReadAllText(path);
    }
}
