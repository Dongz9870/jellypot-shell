namespace JellyfinPotPlayerShell.Core.Playback;

public static class MediaPathNormalizer
{
    public static string NormalizeForPlayer(string? mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            throw new PotPlayerException("Jellyfin 没有返回可播放的媒体路径。");
        }

        var path = mediaPath.Trim().Trim('"');
        if (path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            throw new PotPlayerException("媒体路径不能是 file URI。");
        }

        const string extendedUncPrefix = @"\\?\UNC\";
        if (path.StartsWith(extendedUncPrefix, StringComparison.OrdinalIgnoreCase))
        {
            path = @"\\" + path[extendedUncPrefix.Length..];
        }
        else
        {
            const string extendedLocalPrefix = @"\\?\";
            if (path.StartsWith(extendedLocalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                path = path[extendedLocalPrefix.Length..];
            }
        }

        return path.Replace('/', '\\');
    }
}
