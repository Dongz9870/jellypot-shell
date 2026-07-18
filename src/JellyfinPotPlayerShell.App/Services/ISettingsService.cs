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

    Task CompleteSetupAsync(
        string serverUrl,
        string potPlayerPath,
        IReadOnlyList<PathMappingRule> pathMappings,
        IReadOnlyList<ManagedNetworkDrive> managedNetworkDrives,
        CancellationToken cancellationToken = default);

    Task SaveNetworkDriveStateAsync(
        IReadOnlyList<PathMappingRule> pathMappings,
        IReadOnlyList<ManagedNetworkDrive> managedNetworkDrives,
        CancellationToken cancellationToken = default);
}
