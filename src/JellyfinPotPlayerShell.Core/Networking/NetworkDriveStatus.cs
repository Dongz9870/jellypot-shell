namespace JellyfinPotPlayerShell.Core.Networking;

public enum NetworkDriveStatusKind
{
    Available,
    ConnectedToExpectedPath,
    Occupied,
    Unsupported
}

public sealed record NetworkDriveStatus(
    NetworkDriveStatusKind Kind,
    NetworkDriveDefinition? Definition,
    string? ExistingRemotePath,
    string Message);

public sealed record NetworkDriveOperationResult(
    bool Succeeded,
    bool Changed,
    string Message);
