using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.App.Services;

public interface ISettingsService
{
    ShellSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        string serverUrl,
        string potPlayerPath,
        bool autoDetect,
        IReadOnlyList<PathMappingRule> pathMappings,
        CancellationToken cancellationToken = default);

    Task SavePotPlayerPathAsync(
        string potPlayerPath,
        CancellationToken cancellationToken = default);
}
