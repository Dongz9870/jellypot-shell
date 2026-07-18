using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using JellyfinPotPlayerShell.App.Services;
using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Paths;
using JellyfinPotPlayerShell.Core.Playback;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace JellyfinPotPlayerShell.App;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IPotPlayerLocator _potPlayerLocator;
    private readonly ILogger<SettingsWindow> _logger;
    private readonly ObservableCollection<PathMappingRule> _pathMappings;

    public SettingsWindow(
        ISettingsService settingsService,
        IPotPlayerLocator potPlayerLocator,
        ILogger<SettingsWindow> logger)
    {
        _settingsService = settingsService;
        _potPlayerLocator = potPlayerLocator;
        _logger = logger;

        InitializeComponent();
        ServerUrlTextBox.Text = _settingsService.Current.Jellyfin.ServerUrl;
        PotPlayerPathTextBox.Text = _settingsService.Current.Player.PotPlayerPath;
        AutoDetectCheckBox.IsChecked = _settingsService.Current.Player.AutoDetect;
        _pathMappings = new ObservableCollection<PathMappingRule>(
            _settingsService.Current.PathMappings.Select(rule => rule.Clone()));
        PathMappingsGrid.ItemsSource = _pathMappings;
        ServerUrlTextBox.SelectAll();
        ServerUrlTextBox.Focus();
    }

    private void DetectPotPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        var detectedPath = _potPlayerLocator.Locate(PotPlayerPathTextBox.Text);
        if (detectedPath is null)
        {
            ValidationText.Text = "没有自动找到 PotPlayerMini64.exe，请点击“浏览…”手动选择。";
            return;
        }

        PotPlayerPathTextBox.Text = detectedPath;
    }

    private void BrowsePotPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        var dialog = new OpenFileDialog
        {
            Title = "选择 PotPlayerMini64.exe",
            Filter = "PotPlayerMini64.exe|PotPlayerMini64.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        var currentPath = PotPlayerPathTextBox.Text.Trim().Trim('"');
        if (Path.IsPathFullyQualified(currentPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!PotPlayerExecutable.TryValidate(
                dialog.FileName,
                out var normalizedPath,
                out var error))
        {
            ValidationText.Text = error;
            return;
        }

        PotPlayerPathTextBox.Text = normalizedPath;
    }

    private void AddPathMappingButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        var rule = new PathMappingRule
        {
            Description = "新规则"
        };
        _pathMappings.Add(rule);
        PathMappingsGrid.SelectedItem = rule;
        PathMappingsGrid.ScrollIntoView(rule);
        PathMappingsGrid.BeginEdit();
    }

    private void DeletePathMappingButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        if (PathMappingsGrid.SelectedItem is PathMappingRule selectedRule)
        {
            _pathMappings.Remove(selectedRule);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        PathMappingsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        PathMappingsGrid.CommitEdit(DataGridEditingUnit.Row, true);

        if (!JellyfinServerUrl.TryNormalize(
                ServerUrlTextBox.Text,
                out var normalizedServerUrl,
                out var serverError))
        {
            ValidationText.Text = serverError;
            return;
        }

        if (AutoDetectCheckBox.IsChecked != true &&
            string.IsNullOrWhiteSpace(PotPlayerPathTextBox.Text))
        {
            ValidationText.Text = "关闭自动检测前，请先选择 PotPlayerMini64.exe。";
            return;
        }

        foreach (var rule in _pathMappings)
        {
            if (PathMappingService.TryValidateRule(rule, out var mappingError))
            {
                continue;
            }

            var ruleName = string.IsNullOrWhiteSpace(rule.Description)
                ? rule.Id
                : rule.Description;
            ValidationText.Text = $"路径映射“{ruleName}”：{mappingError}";
            PathMappingsGrid.SelectedItem = rule;
            PathMappingsGrid.ScrollIntoView(rule);
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            await _settingsService.SaveAsync(
                normalizedServerUrl,
                PotPlayerPathTextBox.Text,
                AutoDetectCheckBox.IsChecked == true,
                _pathMappings.ToArray());
            DialogResult = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "保存应用设置失败");
            ValidationText.Text = $"保存失败：{exception.Message}";
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
