using System.Text.Json;
using System.IO;
using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Networking;
using JellyfinPotPlayerShell.Core.Paths;
using JellyfinPotPlayerShell.Core.Playback;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<JsonSettingsService> _logger;
    private readonly string _settingsDirectory;
    private readonly string _settingsPath;

    public JsonSettingsService(IConfiguration configuration, ILogger<JsonSettingsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JellyfinPotPlayerShell");
        _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
        Current = CreateDefaults();
    }

    public ShellSettings Current { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            Current = CreateDefaults();
            _logger.LogInformation("未找到用户设置，使用默认 Jellyfin 地址");
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<ShellSettings>(
                stream,
                JsonOptions,
                cancellationToken);

            if (settings?.Jellyfin is null ||
                !JellyfinServerUrl.TryNormalize(settings.Jellyfin.ServerUrl, out var normalized, out _))
            {
                throw new JsonException("设置中的 Jellyfin Server URL 无效。");
            }

            settings.Jellyfin.ServerUrl = normalized;
            settings.Player ??= new PlayerSettings();
            if (!string.IsNullOrWhiteSpace(settings.Player.PotPlayerPath))
            {
                settings.Player.PotPlayerPath = PotPlayerExecutable.TryValidate(
                    settings.Player.PotPlayerPath,
                    out var normalizedPlayerPath,
                    out _)
                    ? normalizedPlayerPath
                    : string.Empty;
            }

            settings.PathMappings = PreparePathMappings(
                settings.PathMappings ?? new List<PathMappingRule>(),
                validate: false);
            settings.ManagedNetworkDrives = PrepareManagedNetworkDrives(
                settings.ManagedNetworkDrives);

            Current = settings;
            _logger.LogInformation("用户设置加载完成");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            BackupBrokenSettings();
            Current = CreateDefaults();
            _logger.LogWarning(exception, "用户设置损坏或无法读取，已恢复默认值");
        }
    }

    public Task SaveAsync(
        string serverUrl,
        string potPlayerPath,
        bool autoDetect,
        IReadOnlyList<PathMappingRule> pathMappings,
        CancellationToken cancellationToken = default)
    {
        var settings = CreateValidatedSettings(
            serverUrl,
            potPlayerPath,
            autoDetect,
            pathMappings,
            Current.SetupCompleted,
            Current.ManagedNetworkDrives);
        return SaveSettingsAsync(settings, cancellationToken);
    }

    public Task SavePotPlayerPathAsync(
        string potPlayerPath,
        CancellationToken cancellationToken = default)
    {
        if (!PotPlayerExecutable.TryValidate(
                potPlayerPath,
                out var normalizedPlayerPath,
                out var playerError))
        {
            throw new ArgumentException(playerError, nameof(potPlayerPath));
        }

        var settings = new ShellSettings
        {
            Jellyfin = new JellyfinSettings
            {
                ServerUrl = Current.Jellyfin.ServerUrl
            },
            Player = new PlayerSettings
            {
                PotPlayerPath = normalizedPlayerPath,
                AutoDetect = Current.Player.AutoDetect
            },
            PathMappings = PreparePathMappings(
                Current.PathMappings,
                validate: false),
            SetupCompleted = Current.SetupCompleted,
            ManagedNetworkDrives = PrepareManagedNetworkDrives(
                Current.ManagedNetworkDrives)
        };

        return SaveSettingsAsync(settings, cancellationToken);
    }

    public Task CompleteSetupAsync(
        string serverUrl,
        string potPlayerPath,
        IReadOnlyList<PathMappingRule> pathMappings,
        IReadOnlyList<ManagedNetworkDrive> managedNetworkDrives,
        CancellationToken cancellationToken = default)
    {
        var settings = CreateValidatedSettings(
            serverUrl,
            potPlayerPath,
            true,
            pathMappings,
            true,
            managedNetworkDrives);
        return SaveSettingsAsync(settings, cancellationToken);
    }

    public Task SaveNetworkDriveStateAsync(
        IReadOnlyList<PathMappingRule> pathMappings,
        IReadOnlyList<ManagedNetworkDrive> managedNetworkDrives,
        CancellationToken cancellationToken = default)
    {
        var settings = CreateValidatedSettings(
            Current.Jellyfin.ServerUrl,
            Current.Player.PotPlayerPath,
            Current.Player.AutoDetect,
            pathMappings,
            Current.SetupCompleted,
            managedNetworkDrives);
        return SaveSettingsAsync(settings, cancellationToken);
    }

    private async Task SaveSettingsAsync(
        ShellSettings settings,
        CancellationToken cancellationToken)
    {

        Directory.CreateDirectory(_settingsDirectory);
        var temporaryPath = _settingsPath + ".tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _settingsPath, true);
            Current = settings;
            _logger.LogInformation("应用设置已保存");
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private ShellSettings CreateDefaults()
    {
        var configuredUrl = _configuration["Jellyfin:ServerUrl"];
        if (!JellyfinServerUrl.TryNormalize(configuredUrl, out var normalized, out _))
        {
            normalized = "http://127.0.0.1:8096";
        }

        return new ShellSettings
        {
            Jellyfin = new JellyfinSettings
            {
                ServerUrl = normalized
            },
            Player = new PlayerSettings
            {
                PotPlayerPath = string.Empty,
                AutoDetect = true
            },
            PathMappings = new List<PathMappingRule>(),
            SetupCompleted = false,
            ManagedNetworkDrives = new List<ManagedNetworkDrive>()
        };
    }

    private static ShellSettings CreateValidatedSettings(
        string serverUrl,
        string potPlayerPath,
        bool autoDetect,
        IReadOnlyList<PathMappingRule> pathMappings,
        bool setupCompleted,
        IReadOnlyList<ManagedNetworkDrive> managedNetworkDrives)
    {
        if (!JellyfinServerUrl.TryNormalize(
                serverUrl,
                out var normalizedServerUrl,
                out var serverError))
        {
            throw new ArgumentException(serverError, nameof(serverUrl));
        }

        var normalizedPlayerPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(potPlayerPath) &&
            !PotPlayerExecutable.TryValidate(
                potPlayerPath,
                out normalizedPlayerPath,
                out var playerError))
        {
            throw new ArgumentException(playerError, nameof(potPlayerPath));
        }

        return new ShellSettings
        {
            Jellyfin = new JellyfinSettings
            {
                ServerUrl = normalizedServerUrl
            },
            Player = new PlayerSettings
            {
                PotPlayerPath = normalizedPlayerPath,
                AutoDetect = autoDetect
            },
            PathMappings = PreparePathMappings(pathMappings, validate: true),
            SetupCompleted = setupCompleted,
            ManagedNetworkDrives = PrepareManagedNetworkDrives(
                managedNetworkDrives)
        };
    }

    private static List<PathMappingRule> PreparePathMappings(
        IEnumerable<PathMappingRule>? pathMappings,
        bool validate)
    {
        var prepared = new List<PathMappingRule>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceRule in pathMappings ?? Array.Empty<PathMappingRule>())
        {
            if (sourceRule is null)
            {
                continue;
            }

            var rule = new PathMappingRule
            {
                Id = sourceRule.Id?.Trim() ?? string.Empty,
                Enabled = sourceRule.Enabled,
                Description = sourceRule.Description?.Trim() ?? string.Empty,
                ServerPrefix = sourceRule.ServerPrefix?.Trim().Trim('"') ?? string.Empty,
                WindowsPrefix = sourceRule.WindowsPrefix?.Trim().Trim('"') ?? string.Empty
            };

            if (rule.Id.Length == 0 || !usedIds.Add(rule.Id))
            {
                do
                {
                    rule.Id = Guid.NewGuid().ToString("N");
                }
                while (!usedIds.Add(rule.Id));
            }

            if (validate &&
                !PathMappingService.TryValidateRule(rule, out var mappingError))
            {
                var ruleName = string.IsNullOrWhiteSpace(rule.Description)
                    ? rule.Id
                    : rule.Description;
                throw new ArgumentException(
                    $"路径映射“{ruleName}”：{mappingError}",
                    nameof(pathMappings));
            }

            if (!validate &&
                !PathMappingService.TryValidateRule(rule, out _))
            {
                rule.Enabled = false;
            }

            prepared.Add(rule);
        }

        return prepared;
    }

    private static List<ManagedNetworkDrive> PrepareManagedNetworkDrives(
        IEnumerable<ManagedNetworkDrive>? managedNetworkDrives)
    {
        var prepared = new List<ManagedNetworkDrive>();
        var usedDriveLetters = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var managedDrive in
                 managedNetworkDrives ?? Array.Empty<ManagedNetworkDrive>())
        {
            if (managedDrive is null ||
                !NetworkDriveDefinition.TryCreate(
                    managedDrive.DriveLetter,
                    managedDrive.RemotePath,
                    out var definition,
                    out _) ||
                definition is null ||
                !usedDriveLetters.Add(definition.DriveName))
            {
                continue;
            }

            prepared.Add(new ManagedNetworkDrive
            {
                DriveLetter = definition.DriveName,
                RemotePath = definition.RemotePath
            });
        }

        return prepared;
    }

    private void BackupBrokenSettings()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            File.Copy(_settingsPath, Path.Combine(_settingsDirectory, "settings.broken.json"), true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "无法备份损坏的用户设置");
        }
    }
}
