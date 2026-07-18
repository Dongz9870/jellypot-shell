namespace JellyfinPotPlayerShell.Core.Networking;

public sealed record NetworkDriveDefinition(
    string DriveName,
    string DriveRoot,
    string RemotePath)
{
    public static bool TryCreate(
        string? driveLetter,
        string? remotePath,
        out NetworkDriveDefinition? definition,
        out string error)
    {
        definition = null;
        error = string.Empty;

        var drive = (driveLetter ?? string.Empty)
            .Trim()
            .TrimEnd('\\', '/');
        if (drive.Length == 1 && char.IsAsciiLetter(drive[0]))
        {
            drive += ":";
        }

        if (drive.Length != 2 ||
            !char.IsAsciiLetter(drive[0]) ||
            drive[1] != ':')
        {
            error = "盘符必须是 M: 这样的单个 Windows 盘符。";
            return false;
        }

        var remote = (remotePath ?? string.Empty)
            .Trim()
            .Trim('"')
            .Replace('/', '\\')
            .TrimEnd('\\');
        var uncParts = remote.StartsWith(@"\\", StringComparison.Ordinal) &&
            !remote.StartsWith(@"\\?\", StringComparison.Ordinal)
                ? remote[2..].Split(
                    '\\',
                    StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
        if (uncParts.Length < 2)
        {
            error = "网络盘源路径必须包含服务器名和共享名，例如 \\\\NAS\\Media。";
            return false;
        }

        var normalizedDrive = char.ToUpperInvariant(drive[0]) + ":";
        definition = new NetworkDriveDefinition(
            normalizedDrive,
            normalizedDrive + "\\",
            remote);
        return true;
    }
}
