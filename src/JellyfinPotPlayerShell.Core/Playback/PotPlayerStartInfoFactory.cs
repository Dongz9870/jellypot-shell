using System.Diagnostics;

namespace JellyfinPotPlayerShell.Core.Playback;

public static class PotPlayerStartInfoFactory
{
    public static ProcessStartInfo Create(string executablePath, string mediaPath)
    {
        if (!PotPlayerExecutable.TryValidate(
                executablePath,
                out var normalizedExecutablePath,
                out var playerError))
        {
            throw new PotPlayerException(playerError);
        }

        var normalizedMediaPath = MediaPathNormalizer.NormalizeForPlayer(mediaPath);
        if (!File.Exists(normalizedMediaPath))
        {
            throw new PotPlayerException(
                $"Windows 无法访问该媒体文件：\n{normalizedMediaPath}\n\n" +
                "请检查 NAS 是否开机、共享是否可访问，以及 Jellyfin 返回的路径是否适用于本机。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = normalizedExecutablePath,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(normalizedMediaPath);
        return startInfo;
    }
}
