using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class WebViewHostService : IWebViewHostService
{
    private readonly ILogger<WebViewHostService> _logger;

    public WebViewHostService(ILogger<WebViewHostService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(WebView2 webView)
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JellyfinPotPlayerShell",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);
        await webView.EnsureCoreWebView2Async(environment);

        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        await RegisterInjectionAsync(webView.CoreWebView2);
        _logger.LogInformation("WebView2 初始化完成，登录数据将持久化保存");
    }

    private static async Task RegisterInjectionAsync(CoreWebView2 coreWebView)
    {
        var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
        var adapterScript = await File.ReadAllTextAsync(
            Path.Combine(assetsDirectory, "jellyfin-web-adapter.js"));
        var injectionScript = await File.ReadAllTextAsync(
            Path.Combine(assetsDirectory, "inject.js"));
        var buttonCss = await File.ReadAllTextAsync(
            Path.Combine(assetsDirectory, "potplayer-button.css"));

        var cssLiteral = JsonSerializer.Serialize(buttonCss);
        var styleInstaller = """
            (() => {
                "use strict";

                const STYLE_ID = "jpps-potplayer-button-style";

                function installStyle() {
                    if (document.getElementById(STYLE_ID)) return;
                    const target = document.head ?? document.documentElement;
                    if (!target) return;

                    const style = document.createElement("style");
                    style.id = STYLE_ID;
                    style.textContent = CSS_CONTENT;
                    target.appendChild(style);
                }

                document.addEventListener("DOMContentLoaded", installStyle, { once: true });
                installStyle();
            })();
            """.Replace("CSS_CONTENT", cssLiteral, StringComparison.Ordinal);

        var combinedScript = string.Join(
            Environment.NewLine,
            adapterScript,
            styleInstaller,
            injectionScript);

        await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync(combinedScript);
    }
}
