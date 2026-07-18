namespace JellyfinPotPlayerShell.Core.Configuration;

public sealed class ShellSettings
{
    public JellyfinSettings Jellyfin { get; set; } = new();

    public PlayerSettings Player { get; set; } = new();
}

public sealed class JellyfinSettings
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:8096";
}

public sealed class PlayerSettings
{
    public string PotPlayerPath { get; set; } = string.Empty;

    public bool AutoDetect { get; set; } = true;
}
