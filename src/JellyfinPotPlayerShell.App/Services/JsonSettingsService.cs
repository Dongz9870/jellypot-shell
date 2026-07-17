using System.Text.Json;
using System.IO;
using JellyfinPotPlayerShell.Core.Configuration;
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

    public async Task SaveServerUrlAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        if (!JellyfinServerUrl.TryNormalize(serverUrl, out var normalized, out var error))
        {
            throw new ArgumentException(error, nameof(serverUrl));
        }

        var settings = new ShellSettings
        {
            Jellyfin = new JellyfinSettings
            {
                ServerUrl = normalized
            }
        };

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
            _logger.LogInformation("Jellyfin 服务器设置已保存");
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
            }
        };
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
