using System.Text;
using System.Windows;
using JellyfinPotPlayerShell.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace JellyfinPotPlayerShell.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly IWebViewHostService _webViewHostService;
    private readonly ILogger<MainWindow> _logger;
    private bool _webViewReady;

    public MainWindow(
        IServiceProvider serviceProvider,
        ISettingsService settingsService,
        IWebViewHostService webViewHostService,
        ILogger<MainWindow> logger)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _webViewHostService = webViewHostService;
        _logger = logger;

        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Browser.NavigationStarting += Browser_NavigationStarting;
        Browser.NavigationCompleted += Browser_NavigationCompleted;
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

    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        {
            StatusText.Text = $"正在加载 {uri.GetLeftPart(UriPartial.Authority)}";
        }
    }

    private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            StatusText.Text = "Jellyfin 已加载";
            return;
        }

        StatusText.Text = $"加载失败：{e.WebErrorStatus}";
        _logger.LogWarning("Jellyfin 页面加载失败，WebView2 状态：{WebErrorStatus}", e.WebErrorStatus);
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
