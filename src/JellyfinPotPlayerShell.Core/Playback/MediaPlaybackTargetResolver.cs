namespace JellyfinPotPlayerShell.Core.Playback;

public static class MediaPlaybackTargetResolver
{
    private const string BluRayDirectoryName = "BDMV";
    private const string BluRayIndexFileName = "index.bdmv";

    public static string Resolve(string? mediaPath)
    {
        var normalizedPath = MediaPathNormalizer.NormalizeForPlayer(mediaPath);
        if (File.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        if (!Directory.Exists(normalizedPath))
        {
            throw CreateInaccessiblePathException(normalizedPath);
        }

        var trimmedPath = Path.TrimEndingDirectorySeparator(normalizedPath);
        var bdmvDirectory = Path.GetFileName(trimmedPath).Equals(
            BluRayDirectoryName,
            StringComparison.OrdinalIgnoreCase)
            ? trimmedPath
            : Path.Combine(trimmedPath, BluRayDirectoryName);
        var indexPath = Path.Combine(bdmvDirectory, BluRayIndexFileName);

        if (File.Exists(indexPath))
        {
            return indexPath;
        }

        throw new PotPlayerException(
            $"检测到媒体路径是目录，但没有找到 Blu-ray 播放入口：\n{indexPath}\n\n" +
            "可识别的原盘目录必须包含 BDMV\\index.bdmv。");
    }

    private static PotPlayerException CreateInaccessiblePathException(string path)
    {
        return new PotPlayerException(
            $"Windows 无法访问该媒体文件或 Blu-ray 原盘目录：\n{path}\n\n" +
            "请检查 NAS 是否开机、共享是否可访问，以及 Jellyfin 返回的路径是否适用于本机。");
    }
}
