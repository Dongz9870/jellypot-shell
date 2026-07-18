using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace JellyfinPotPlayerShell.Core.Networking;

public sealed class WindowsNetworkDriveService : INetworkDriveService
{
    private const int NoError = 0;
    private const int ErrorMoreData = 234;
    private const int ErrorNotConnected = 2250;
    private const int ResourceTypeDisk = 1;
    private const int ConnectUpdateProfile = 1;

    public NetworkDriveStatus Inspect(string driveLetter, string remotePath)
    {
        if (!NetworkDriveDefinition.TryCreate(
                driveLetter,
                remotePath,
                out var definition,
                out var validationError))
        {
            return new NetworkDriveStatus(
                NetworkDriveStatusKind.Unsupported,
                null,
                null,
                validationError);
        }

        if (!OperatingSystem.IsWindows())
        {
            return new NetworkDriveStatus(
                NetworkDriveStatusKind.Unsupported,
                definition,
                null,
                "网络盘功能仅支持 Windows。" );
        }

        var existingRemotePath = GetRemotePath(definition!.DriveName);
        if (existingRemotePath is not null)
        {
            if (PathsEqual(existingRemotePath, definition.RemotePath))
            {
                return new NetworkDriveStatus(
                    NetworkDriveStatusKind.ConnectedToExpectedPath,
                    definition,
                    existingRemotePath,
                    $"{definition.DriveName} 已连接到目标 NAS。" );
            }

            return new NetworkDriveStatus(
                NetworkDriveStatusKind.Occupied,
                definition,
                existingRemotePath,
                $"{definition.DriveName} 已连接到其他网络位置，不会覆盖。" );
        }

        if (IsDriveLetterInUse(definition.DriveRoot))
        {
            return new NetworkDriveStatus(
                NetworkDriveStatusKind.Occupied,
                definition,
                null,
                $"{definition.DriveName} 已被本机磁盘或其他设备占用，不会覆盖。" );
        }

        return new NetworkDriveStatus(
            NetworkDriveStatusKind.Available,
            definition,
            null,
            $"{definition.DriveName} 可用于连接 NAS。" );
    }

    public NetworkDriveOperationResult Connect(
        string driveLetter,
        string remotePath)
    {
        var status = Inspect(driveLetter, remotePath);
        if (status.Kind == NetworkDriveStatusKind.ConnectedToExpectedPath)
        {
            return new NetworkDriveOperationResult(true, false, status.Message);
        }

        if (status.Kind != NetworkDriveStatusKind.Available ||
            status.Definition is null)
        {
            return new NetworkDriveOperationResult(false, false, status.Message);
        }

        var resource = new NativeNetworkResource
        {
            ResourceType = ResourceTypeDisk,
            LocalName = status.Definition.DriveName,
            RemoteName = status.Definition.RemotePath
        };
        var errorCode = WNetAddConnection2(
            ref resource,
            null,
            null,
            ConnectUpdateProfile);
        if (errorCode != NoError)
        {
            return Failure("连接网络盘失败", errorCode);
        }

        return new NetworkDriveOperationResult(
            true,
            true,
            $"已创建 {status.Definition.DriveName}，并设置为登录时重新连接。" );
    }

    public NetworkDriveOperationResult Disconnect(
        string driveLetter,
        string remotePath)
    {
        var status = Inspect(driveLetter, remotePath);
        if (status.Kind == NetworkDriveStatusKind.Available)
        {
            return new NetworkDriveOperationResult(
                true,
                false,
                "该网络盘当前未连接。" );
        }

        if (status.Kind != NetworkDriveStatusKind.ConnectedToExpectedPath ||
            status.Definition is null)
        {
            return new NetworkDriveOperationResult(false, false, status.Message);
        }

        var errorCode = WNetCancelConnection2(
            status.Definition.DriveName,
            ConnectUpdateProfile,
            false);
        if (errorCode != NoError && errorCode != ErrorNotConnected)
        {
            return Failure("断开网络盘失败", errorCode);
        }

        return new NetworkDriveOperationResult(
            true,
            errorCode == NoError,
            $"已断开 {status.Definition.DriveName}。" );
    }

    private static string? GetRemotePath(string driveName)
    {
        var capacity = 512;
        var builder = new StringBuilder(capacity);
        var result = WNetGetConnection(driveName, builder, ref capacity);
        if (result == ErrorMoreData && capacity > builder.Capacity)
        {
            builder = new StringBuilder(capacity);
            result = WNetGetConnection(driveName, builder, ref capacity);
        }

        return result == NoError ? builder.ToString() : null;
    }

    private static bool IsDriveLetterInUse(string driveRoot)
    {
        try
        {
            return DriveInfo.GetDrives().Any(drive =>
                string.Equals(
                    drive.Name,
                    driveRoot,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            left.TrimEnd('\\', '/'),
            right.TrimEnd('\\', '/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static NetworkDriveOperationResult Failure(
        string action,
        int errorCode)
    {
        var detail = new Win32Exception(errorCode).Message;
        return new NetworkDriveOperationResult(
            false,
            false,
            $"{action}（Windows 错误 {errorCode}）：{detail}" );
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeNetworkResource
    {
        public int Scope;
        public int ResourceType;
        public int DisplayType;
        public int Usage;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? LocalName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? RemoteName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Provider;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        ref NativeNetworkResource networkResource,
        string? password,
        string? username,
        int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(
        string name,
        int flags,
        bool force);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnection(
        string localName,
        StringBuilder remoteName,
        ref int length);
}
