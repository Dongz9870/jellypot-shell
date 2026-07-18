using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Messaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class WebViewHostService : IWebViewHostService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WebViewHostService> _logger;
    private CoreWebView2? _coreWebView;

    public WebViewHostService(
        ISettingsService settingsService,
        ILogger<WebViewHostService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public event EventHandler<PlayRequestReceivedEventArgs>? PlayRequested;

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

        if (_coreWebView is not null)
        {
            _coreWebView.WebMessageReceived -= OnWebMessageReceived;
        }

        _coreWebView = webView.CoreWebView2;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        await RegisterInjectionAsync(webView.CoreWebView2);
        _logger.LogInformation("WebView2 初始化完成，登录数据将持久化保存");
    }

    private void OnWebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs eventArgs)
    {
        if (!IsConfiguredJellyfinOrigin(eventArgs.Source))
        {
            _logger.LogWarning("已忽略非配置 Jellyfin 来源的网页消息");
            return;
        }

        if (!WebBridgeMessageParser.TryParsePlayRequest(
                eventArgs.WebMessageAsJson,
                out var message) ||
            message is null)
        {
            _logger.LogWarning("已忽略格式无效的网页消息");
            return;
        }

        _logger.LogInformation("已收到有效的 PotPlayer 按钮请求");
        PlayRequested?.Invoke(
            this,
            new PlayRequestReceivedEventArgs(message));
    }

    private bool IsConfiguredJellyfinOrigin(string source)
    {
        return JellyfinServerOrigin.Matches(
            _settingsService.Current.Jellyfin.ServerUrl,
            source);
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
