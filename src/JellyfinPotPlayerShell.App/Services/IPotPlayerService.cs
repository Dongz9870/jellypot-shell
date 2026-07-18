namespace JellyfinPotPlayerShell.App.Services;

public interface IPotPlayerService
{
    Task PlayAsync(
        string executablePath,
        string mediaPath,
        CancellationToken cancellationToken = default);
}
