using System.Configuration;
using System.Windows;
using System.IO;
using System.Net.Http;
using JellyfinPotPlayerShell.App.Logging;
using JellyfinPotPlayerShell.App.Services;
using JellyfinPotPlayerShell.Core.Jellyfin;
using JellyfinPotPlayerShell.Core.Networking;
using JellyfinPotPlayerShell.Core.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JellyfinPotPlayerShell.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = CreateHost();
            await _host.StartAsync();

            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            if (!settingsService.Current.SetupCompleted)
            {
                var setupWizard = _host.Services.GetRequiredService<SetupWizardWindow>();
                setupWizard.ShowDialog();
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"应用启动失败：{exception.Message}",
                "Jellyfin PotPlayer Shell",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JellyfinPotPlayerShell",
            "Logs");
        var fileLoggerProvider = new DailyFileLoggerProvider(logDirectory);

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(fileLoggerProvider);

        builder.Services.AddSingleton<ISettingsService, JsonSettingsService>();
        builder.Services.AddSingleton<IWebViewHostService, WebViewHostService>();
        builder.Services.AddSingleton<IPotPlayerLocator, PotPlayerLocator>();
        builder.Services.AddSingleton<IPotPlayerService, PotPlayerService>();
        builder.Services.AddSingleton<PathMappingService>();
        builder.Services.AddSingleton<INetworkDriveService, WindowsNetworkDriveService>();
        builder.Services.AddSingleton<NasPathProbeService>();
        builder.Services.AddHttpClient<JellyfinServerDetector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        builder.Services.AddHttpClient<JellyfinApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<SettingsWindow>();
        builder.Services.AddTransient<SetupWizardWindow>();

        return builder.Build();
    }
}
