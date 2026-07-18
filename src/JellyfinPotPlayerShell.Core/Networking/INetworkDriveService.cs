namespace JellyfinPotPlayerShell.Core.Networking;

public interface INetworkDriveService
{
    NetworkDriveStatus Inspect(string driveLetter, string remotePath);

    NetworkDriveOperationResult Connect(string driveLetter, string remotePath);

    NetworkDriveOperationResult Disconnect(string driveLetter, string remotePath);
}
