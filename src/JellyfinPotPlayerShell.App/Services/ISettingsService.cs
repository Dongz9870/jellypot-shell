using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.App.Services;

public interface ISettingsService
{
    ShellSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveServerUrlAsync(string serverUrl, CancellationToken cancellationToken = default);
}
