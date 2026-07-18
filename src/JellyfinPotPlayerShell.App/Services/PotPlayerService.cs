using System.ComponentModel;
using System.Diagnostics;
using JellyfinPotPlayerShell.Core.Playback;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class PotPlayerService : IPotPlayerService
{
    public Task PlayAsync(
        string executablePath,
        string mediaPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startInfo = PotPlayerStartInfoFactory.Create(executablePath, mediaPath);

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new PotPlayerException("Windows 未能启动 PotPlayer。");
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            throw new PotPlayerException(
                "无法启动 PotPlayer，请检查播放器是否安装完整。");
        }

        return Task.CompletedTask;
    }
}
