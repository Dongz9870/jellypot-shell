using System.Windows;
using JellyfinPotPlayerShell.App.Services;
using JellyfinPotPlayerShell.Core.Jellyfin;
using JellyfinPotPlayerShell.Core.Playback;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace JellyfinPotPlayerShell.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly IWebViewHostService _webViewHostService;
    private readonly JellyfinApiService _jellyfinApiService;
    private readonly IPotPlayerLocator _potPlayerLocator;
    private readonly IPotPlayerService _potPlayerService;
    private readonly ILogger<MainWindow> _logger;
    private bool _webViewReady;
    private bool _isResolvingMedia;

    public MainWindow(
        IServiceProvider serviceProvider,
        ISettingsService settingsService,
        IWebViewHostService webViewHostService,
        JellyfinApiService jellyfinApiService,
        IPotPlayerLocator potPlayerLocator,
        IPotPlayerService potPlayerService,
        ILogger<MainWindow> logger)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _webViewHostService = webViewHostService;
        _jellyfinApiService = jellyfinApiService;
        _potPlayerLocator = potPlayerLocator;
        _potPlayerService = potPlayerService;
        _logger = logger;

        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        Browser.NavigationStarting += Browser_NavigationStarting;
        Browser.NavigationCompleted += Browser_NavigationCompleted;
        _webViewHostService.PlayRequested += WebViewHostService_PlayRequested;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        try
        {
            await _webViewHostService.InitializeAsync(Browser);
            _webViewReady = true;
            NavigateToConfiguredServer();
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            _logger.LogError(exception, "WebView2 Runtime 未安装");
            ShowStartupError("未检测到 Microsoft Edge WebView2 Runtime，请安装后重试。");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "初始化 WebView2 失败");
            ShowStartupError($"无法初始化网页组件：{exception.Message}");
        }
    }

    private void NavigateToConfiguredServer()
    {
        if (!_webViewReady)
        {
            return;
        }

        var serverUri = new Uri(_settingsService.Current.Jellyfin.ServerUrl);
        Browser.Source = serverUri;
        StatusText.Text = $"正在打开 {serverUri.GetLeftPart(UriPartial.Authority)}";
    }

    private void Browser_NavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs e)
    {
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        {
            StatusText.Text = $"正在加载 {uri.GetLeftPart(UriPartial.Authority)}";
        }
    }

    private void Browser_NavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            StatusText.Text = "Jellyfin 已加载";
            return;
        }

        StatusText.Text = $"加载失败：{e.WebErrorStatus}";
        _logger.LogWarning(
            "Jellyfin 页面加载失败，WebView2 状态：{WebErrorStatus}",
            e.WebErrorStatus);
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_webViewReady)
        {
            Browser.Reload();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;

        if (settingsWindow.ShowDialog() == true)
        {
            NavigateToConfiguredServer();
        }
    }

    private async void WebViewHostService_PlayRequested(
        object? sender,
        PlayRequestReceivedEventArgs eventArgs)
    {
        if (_isResolvingMedia)
        {
            StatusText.Text = "正在处理上一条播放请求，请稍候";
            return;
        }

        _isResolvingMedia = true;
        StatusText.Text = "正在读取 Jellyfin 媒体路径";

        try
        {
            var item = await _jellyfinApiService.GetItemAsync(
                _settingsService.Current.Jellyfin.ServerUrl,
                eventArgs.Request);
            var mediaSourceCount = item.MediaSources?.Count ?? 0;

            _logger.LogInformation(
                "Jellyfin 媒体信息读取成功，媒体源数量：{MediaSourceCount}",
                mediaSourceCount);

            var mediaPath = SelectMediaPath(item);
            if (mediaPath is null)
            {
                StatusText.Text = "已取消 PotPlayer 播放";
                return;
            }

            var playerPath = await ResolvePlayerPathAsync();
            if (playerPath is null)
            {
                StatusText.Text = "未选择 PotPlayer";
                return;
            }

            StatusText.Text = "正在检查媒体路径并启动 PotPlayer";
            await Task.Run(() => _potPlayerService.PlayAsync(playerPath, mediaPath));
            _logger.LogInformation("PotPlayer 已成功启动");
            StatusText.Text = "已交给 PotPlayer 播放";
        }
        catch (JellyfinApiException exception)
        {
            _logger.LogWarning("Jellyfin 媒体信息读取失败");
            StatusText.Text = exception.Message;
            MessageBox.Show(
                this,
                exception.Message,
                "无法读取媒体信息",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (PotPlayerException exception)
        {
            _logger.LogWarning("PotPlayer 播放失败");
            StatusText.Text = "PotPlayer 播放失败";
            MessageBox.Show(
                this,
                exception.Message,
                "无法使用 PotPlayer 播放",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            const string message = "处理 PotPlayer 播放请求时发生未知错误，请稍后重试。";
            _logger.LogError("处理 PotPlayer 播放请求时发生未知错误");
            StatusText.Text = message;
            MessageBox.Show(
                this,
                message,
                "无法使用 PotPlayer 播放",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isResolvingMedia = false;
        }
    }

    private string? SelectMediaPath(JellyfinMediaItem item)
    {
        var mediaSources = (item.MediaSources ?? Array.Empty<JellyfinMediaSource>())
            .Where(source => !string.IsNullOrWhiteSpace(source.Path))
            .ToArray();

        if (mediaSources.Length == 0)
        {
            return item.Path;
        }

        if (mediaSources.Length == 1)
        {
            return mediaSources[0].Path;
        }

        var selectionWindow = new MediaSourceSelectionWindow(mediaSources)
        {
            Owner = this
        };
        return selectionWindow.ShowDialog() == true
            ? selectionWindow.SelectedMediaSource?.Path
            : null;
    }

    private async Task<string?> ResolvePlayerPathAsync()
    {
        var playerSettings = _settingsService.Current.Player;
        string? playerPath;

        if (playerSettings.AutoDetect)
        {
            playerPath = _potPlayerLocator.Locate(playerSettings.PotPlayerPath);
        }
        else
        {
            playerPath = PotPlayerExecutable.TryValidate(
                playerSettings.PotPlayerPath,
                out var configuredPath,
                out _)
                ? configuredPath
                : null;
        }

        if (playerPath is not null)
        {
            await SaveDetectedPlayerPathAsync(playerPath);
            return playerPath;
        }

        var chooseResult = MessageBox.Show(
            this,
            "没有找到 PotPlayerMini64.exe。\n\n是否现在手动选择播放器？",
            "未找到 PotPlayer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (chooseResult != MessageBoxResult.Yes)
        {
            return null;
        }

        var dialog = new OpenFileDialog
        {
            Title = "选择 PotPlayerMini64.exe",
            Filter = "PotPlayerMini64.exe|PotPlayerMini64.exe",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
        {
            return null;
        }

        if (!PotPlayerExecutable.TryValidate(
                dialog.FileName,
                out var selectedPlayerPath,
                out var error))
        {
            throw new PotPlayerException(error);
        }

        await SaveDetectedPlayerPathAsync(selectedPlayerPath);
        return selectedPlayerPath;
    }

    private async Task SaveDetectedPlayerPathAsync(string playerPath)
    {
        if (string.Equals(
                _settingsService.Current.Player.PotPlayerPath,
                playerPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await _settingsService.SavePotPlayerPathAsync(playerPath);
            _logger.LogInformation("自动检测到的 PotPlayer 路径已保存");
        }
        catch (Exception)
        {
            _logger.LogWarning("PotPlayer 路径可用，但无法保存到设置");
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _webViewHostService.PlayRequested -= WebViewHostService_PlayRequested;
    }

    private void ShowStartupError(string message)
    {
        StatusText.Text = message;
        MessageBox.Show(
            message,
            "Jellyfin PotPlayer Shell",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
