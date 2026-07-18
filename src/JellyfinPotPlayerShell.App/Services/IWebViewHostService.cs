using Microsoft.Web.WebView2.Wpf;

namespace JellyfinPotPlayerShell.App.Services;

public interface IWebViewHostService
{
    event EventHandler<PlayRequestReceivedEventArgs>? PlayRequested;

    Task InitializeAsync(WebView2 webView);

    Task ShowHdrNoticeAsync();
}
