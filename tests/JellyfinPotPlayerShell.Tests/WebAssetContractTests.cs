namespace JellyfinPotPlayerShell.Tests;

public sealed class WebAssetContractTests
{
    [Fact]
    public void Adapter_CentralizesSelectorsAndPlayableItemTypes()
    {
        var adapter = ReadAsset("jellyfin-web-adapter.js");

        Assert.Contains("mainDetailButtons", adapter, StringComparison.Ordinal);
        Assert.Contains("itemDetailPage", adapter, StringComparison.Ordinal);
        Assert.Contains("btnPlay", adapter, StringComparison.Ordinal);
        Assert.Contains("\"movie\"", adapter, StringComparison.Ordinal);
        Assert.Contains("\"episode\"", adapter, StringComparison.Ordinal);
        Assert.Contains("NON_PLAYABLE_ITEM_TYPES", adapter, StringComparison.Ordinal);
        Assert.Contains("ApiClient", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Injection_HandlesSpaRoutesAndPreventsDuplicateButtons()
    {
        var injection = ReadAsset("inject.js");

        Assert.Contains("MutationObserver", injection, StringComparison.Ordinal);
        Assert.Contains("history.pushState", injection, StringComparison.Ordinal);
        Assert.Contains("history.replaceState", injection, StringComparison.Ordinal);
        Assert.Contains("popstate", injection, StringComparison.Ordinal);
        Assert.Contains("hashchange", injection, StringComparison.Ordinal);
        Assert.Contains("setInterval", injection, StringComparison.Ordinal);
        Assert.Contains("jpps-potplayer-button", injection, StringComparison.Ordinal);
        Assert.DoesNotContain("querySelector", injection, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonStyle_UsesRequiredColors()
    {
        var css = ReadAsset("potplayer-button.css");

        Assert.Contains("#f5c400", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#171717", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void M4Assets_SendOnlyTheExpectedItemAndSessionMessage()
    {
        var scripts = string.Join(
            Environment.NewLine,
            ReadAsset("jellyfin-web-adapter.js"),
            ReadAsset("inject.js"));

        Assert.Contains("postMessage", scripts, StringComparison.Ordinal);
        Assert.Contains("type: \"playWithPotPlayer\"", scripts, StringComparison.Ordinal);
        Assert.Contains("itemId: context.itemId", scripts, StringComparison.Ordinal);
        Assert.Contains("serverAddress: apiContext.serverAddress", scripts, StringComparison.Ordinal);
        Assert.Contains("userId: apiContext.userId", scripts, StringComparison.Ordinal);
        Assert.Contains("accessToken: apiContext.accessToken", scripts, StringComparison.Ordinal);
        Assert.DoesNotContain("executablePath", scripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pageUrl", scripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProcessStartInfo", scripts, StringComparison.Ordinal);
    }

    private static string ReadAsset(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        Assert.True(File.Exists(path), $"缺少测试资源：{path}");
        return File.ReadAllText(path);
    }
}
