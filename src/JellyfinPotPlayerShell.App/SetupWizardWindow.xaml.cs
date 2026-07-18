using System.IO;
using System.Windows;
using JellyfinPotPlayerShell.App.Services;
using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Jellyfin;
using JellyfinPotPlayerShell.Core.Networking;
using JellyfinPotPlayerShell.Core.Playback;
using JellyfinPotPlayerShell.Core.Setup;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace JellyfinPotPlayerShell.App;

public partial class SetupWizardWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly JellyfinServerDetector _jellyfinServerDetector;
    private readonly IPotPlayerLocator _potPlayerLocator;
    private readonly INetworkDriveService _networkDriveService;
    private readonly NasPathProbeService _nasPathProbeService;
    private readonly ILogger<SetupWizardWindow> _logger;
    private readonly CancellationTokenSource _closeCancellation = new();

    public SetupWizardWindow(
        ISettingsService settingsService,
        JellyfinServerDetector jellyfinServerDetector,
        IPotPlayerLocator potPlayerLocator,
        INetworkDriveService networkDriveService,
        NasPathProbeService nasPathProbeService,
        ILogger<SetupWizardWindow> logger)
    {
        _settingsService = settingsService;
        _jellyfinServerDetector = jellyfinServerDetector;
        _potPlayerLocator = potPlayerLocator;
        _networkDriveService = networkDriveService;
        _nasPathProbeService = nasPathProbeService;
        _logger = logger;

        InitializeComponent();
        JellyfinUrlTextBox.Text = _settingsService.Current.Jellyfin.ServerUrl;
        PotPlayerPathTextBox.Text = _settingsService.Current.Player.PotPlayerPath;
        Loaded += SetupWizardWindow_Loaded;
        Closed += SetupWizardWindow_Closed;
    }

    private async void SetupWizardWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= SetupWizardWindow_Loaded;
        await DetectEnvironmentAsync();
    }

    private async Task DetectEnvironmentAsync()
    {
        SetDetectionEnabled(false);
        ValidationText.Text = string.Empty;

        try
        {
            var jellyfinTask = DetectJellyfinAsync();
            DetectPotPlayer();
            var movieProbeTask = _nasPathProbeService.ProbeAsync(
                KnownMediaLocations.MovieRoot);
            var tvProbeTask = _nasPathProbeService.ProbeAsync(
                KnownMediaLocations.TvRoot);
            var driveStatusTask = Task.Run(() => new[]
            {
                _networkDriveService.Inspect(
                    KnownMediaLocations.MovieDrive,
                    KnownMediaLocations.MovieRoot),
                _networkDriveService.Inspect(
                    KnownMediaLocations.TvDrive,
                    KnownMediaLocations.TvRoot)
            });

            await jellyfinTask;
            var probes = await Task.WhenAll(movieProbeTask, tvProbeTask);
            var driveStatuses = await driveStatusTask;

            ApplyNasResult(
                probes[0],
                driveStatuses[0],
                MovieNasStatusText,
                MovieDriveStatusText,
                CreateMovieDriveCheckBox);
            ApplyNasResult(
                probes[1],
                driveStatuses[1],
                TvNasStatusText,
                TvDriveStatusText,
                CreateTvDriveCheckBox);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "首次设置环境检测未全部完成");
            ValidationText.Text = "部分自动检测未完成，你仍可手动填写后继续。";
        }
        finally
        {
            SetDetectionEnabled(true);
        }
    }

    private async Task DetectJellyfinAsync()
    {
        JellyfinStatusText.Text = "正在检测当前地址、本机服务和局域网 Jellyfin…";
        var detectedUrl = await _jellyfinServerDetector.DetectAsync(
            JellyfinUrlTextBox.Text,
            _closeCancellation.Token);
        if (detectedUrl is null)
        {
            JellyfinStatusText.Text = "没有自动检测到可访问的服务器，请检查或手动填写地址。";
            return;
        }

        JellyfinUrlTextBox.Text = detectedUrl;
        JellyfinStatusText.Text = "已检测到可访问的 Jellyfin Server。";
    }

    private void DetectPotPlayer()
    {
        var detectedPath = _potPlayerLocator.Locate(PotPlayerPathTextBox.Text);
        if (detectedPath is null)
        {
            PotPlayerStatusText.Text = "没有自动找到 PotPlayerMini64.exe，请手动选择。";
            return;
        }

        PotPlayerPathTextBox.Text = detectedPath;
        PotPlayerStatusText.Text = "已找到 PotPlayerMini64.exe。";
    }

    private static void ApplyNasResult(
        NasPathProbeResult probe,
        NetworkDriveStatus driveStatus,
        System.Windows.Controls.TextBlock nasStatusText,
        System.Windows.Controls.TextBlock driveStatusText,
        System.Windows.Controls.CheckBox createDriveCheckBox)
    {
        nasStatusText.Text = probe.IsAccessible
            ? "NAS：可以访问。"
            : $"NAS：{probe.Message}";
        driveStatusText.Text = driveStatus.Message;

        var canUseDrive = driveStatus.Kind is
            NetworkDriveStatusKind.Available or
            NetworkDriveStatusKind.ConnectedToExpectedPath;
        createDriveCheckBox.IsEnabled = canUseDrive;
        createDriveCheckBox.IsChecked = canUseDrive &&
            (probe.IsAccessible ||
             driveStatus.Kind == NetworkDriveStatusKind.ConnectedToExpectedPath);
    }

    private async void DetectJellyfinButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DetectJellyfinButton.IsEnabled = false;
        ValidationText.Text = string.Empty;
        try
        {
            await DetectJellyfinAsync();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            DetectJellyfinButton.IsEnabled = true;
        }
    }

    private void DetectPotPlayerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        DetectPotPlayer();
    }

    private void BrowsePotPlayerButton_Click(
        object sender,
        RoutedEventArgs e)
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
        PotPlayerStatusText.Text = "已选择 PotPlayerMini64.exe。";
    }

    private async void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        if (!JellyfinServerUrl.TryNormalize(
                JellyfinUrlTextBox.Text,
                out var serverUrl,
                out var serverError))
        {
            ValidationText.Text = serverError;
            return;
        }

        if (!PotPlayerExecutable.TryValidate(
                PotPlayerPathTextBox.Text,
                out var playerPath,
                out var playerError))
        {
            ValidationText.Text = playerError;
            return;
        }

        FinishButton.IsEnabled = false;
        var createdThisAttempt = new List<ManagedNetworkDrive>();

        try
        {
            var useMovieDrive = await ConnectSelectedDriveAsync(
                CreateMovieDriveCheckBox,
                KnownMediaLocations.MovieDrive,
                KnownMediaLocations.MovieRoot,
                createdThisAttempt);
            var useTvDrive = await ConnectSelectedDriveAsync(
                CreateTvDriveCheckBox,
                KnownMediaLocations.TvDrive,
                KnownMediaLocations.TvRoot,
                createdThisAttempt);

            var managedDrives = _settingsService.Current.ManagedNetworkDrives
                .Select(drive => drive.Clone())
                .ToList();
            foreach (var createdDrive in createdThisAttempt)
            {
                managedDrives.RemoveAll(drive => string.Equals(
                    drive.DriveLetter,
                    createdDrive.DriveLetter,
                    StringComparison.OrdinalIgnoreCase));
                managedDrives.Add(createdDrive.Clone());
            }

            var pathMappings = SetupPathMappingPlanner.Build(
                _settingsService.Current.PathMappings,
                useMovieDrive,
                useTvDrive);
            await _settingsService.CompleteSetupAsync(
                serverUrl,
                playerPath,
                pathMappings,
                managedDrives);

            DialogResult = true;
        }
        catch (Exception exception)
        {
            await RollBackCreatedDrivesAsync(createdThisAttempt);
            _logger.LogWarning(exception, "首次设置保存失败");
            ValidationText.Text = exception.Message;
        }
        finally
        {
            FinishButton.IsEnabled = true;
        }
    }

    private async Task<bool> ConnectSelectedDriveAsync(
        System.Windows.Controls.CheckBox checkBox,
        string driveLetter,
        string remotePath,
        ICollection<ManagedNetworkDrive> createdThisAttempt)
    {
        if (checkBox.IsChecked != true)
        {
            return false;
        }

        var result = await Task.Run(() =>
            _networkDriveService.Connect(driveLetter, remotePath));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Message);
        }

        if (result.Changed)
        {
            var definitionCreated = NetworkDriveDefinition.TryCreate(
                driveLetter,
                remotePath,
                out var definition,
                out _);
            if (definitionCreated && definition is not null)
            {
                createdThisAttempt.Add(new ManagedNetworkDrive
                {
                    DriveLetter = definition.DriveName,
                    RemotePath = definition.RemotePath
                });
            }
        }

        return true;
    }

    private async Task RollBackCreatedDrivesAsync(
        IEnumerable<ManagedNetworkDrive> createdDrives)
    {
        foreach (var drive in createdDrives.Reverse())
        {
            await Task.Run(() => _networkDriveService.Disconnect(
                drive.DriveLetter,
                drive.RemotePath));
        }
    }

    private void SetDetectionEnabled(bool enabled)
    {
        DetectJellyfinButton.IsEnabled = enabled;
        DetectPotPlayerButton.IsEnabled = enabled;
        FinishButton.IsEnabled = enabled;
    }

    private void SetupWizardWindow_Closed(object? sender, EventArgs e)
    {
        _closeCancellation.Cancel();
        _closeCancellation.Dispose();
    }
}
