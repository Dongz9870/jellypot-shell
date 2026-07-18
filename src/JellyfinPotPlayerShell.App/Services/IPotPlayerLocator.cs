namespace JellyfinPotPlayerShell.App.Services;

public interface IPotPlayerLocator
{
    string? Locate(string? configuredPath);
}
