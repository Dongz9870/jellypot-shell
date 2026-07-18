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

        var playbackTarget = MediaPlaybackTargetResolver.Resolve(mediaPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = normalizedExecutablePath,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(playbackTarget);
        return startInfo;
    }
}
