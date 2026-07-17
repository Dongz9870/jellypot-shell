using Microsoft.Web.WebView2.Wpf;

namespace JellyfinPotPlayerShell.App.Services;

public interface IWebViewHostService
{
    Task InitializeAsync(WebView2 webView);
}
