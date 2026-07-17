using System.Windows;
using JellyfinPotPlayerShell.App.Services;
using JellyfinPotPlayerShell.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace JellyfinPotPlayerShell.App;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsWindow> _logger;

    public SettingsWindow(ISettingsService settingsService, ILogger<SettingsWindow> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        ServerUrlTextBox.Text = _settingsService.Current.Jellyfin.ServerUrl;
        ServerUrlTextBox.SelectAll();
        ServerUrlTextBox.Focus();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;

        if (!JellyfinServerUrl.TryNormalize(ServerUrlTextBox.Text, out var normalized, out var error))
        {
            ValidationText.Text = error;
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            await _settingsService.SaveServerUrlAsync(normalized);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "保存 Jellyfin 服务器设置失败");
            ValidationText.Text = $"保存失败：{exception.Message}";
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
