namespace JellyfinPotPlayerShell.Core.Configuration;

public sealed class ShellSettings
{
    public JellyfinSettings Jellyfin { get; set; } = new();

    public PlayerSettings Player { get; set; } = new();

    public List<PathMappingRule> PathMappings { get; set; } = new();

    public bool SetupCompleted { get; set; }

    public List<ManagedNetworkDrive> ManagedNetworkDrives { get; set; } = new();
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

public sealed class PathMappingRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public bool Enabled { get; set; } = true;

    public string Description { get; set; } = string.Empty;

    public string ServerPrefix { get; set; } = string.Empty;

    public string WindowsPrefix { get; set; } = string.Empty;

    public PathMappingRule Clone()
    {
        return new PathMappingRule
        {
            Id = Id,
            Enabled = Enabled,
            Description = Description,
            ServerPrefix = ServerPrefix,
            WindowsPrefix = WindowsPrefix
        };
    }
}

public sealed class ManagedNetworkDrive
{
    public string DriveLetter { get; set; } = string.Empty;

    public string RemotePath { get; set; } = string.Empty;

    public ManagedNetworkDrive Clone()
    {
        return new ManagedNetworkDrive
        {
            DriveLetter = DriveLetter,
            RemotePath = RemotePath
        };
    }
}
