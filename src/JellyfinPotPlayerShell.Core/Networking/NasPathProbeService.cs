namespace JellyfinPotPlayerShell.Core.Networking;

public sealed record NasPathProbeResult(
    bool IsAccessible,
    bool TimedOut,
    string Message);

public sealed class NasPathProbeService
{
    public async Task<NasPathProbeResult> ProbeAsync(
        string path,
        TimeSpan? timeout = null)
    {
        var probeTask = Task.Run(() => Directory.Exists(path));
        var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(4));
        var completed = await Task.WhenAny(probeTask, timeoutTask);

        if (completed != probeTask)
        {
            return new NasPathProbeResult(
                false,
                true,
                "检测超时，请确认 NAS 已开机且网络正常。" );
        }

        return await probeTask
            ? new NasPathProbeResult(true, false, "可以访问。" )
            : new NasPathProbeResult(
                false,
                false,
                "无法访问，请检查 NAS、共享权限和当前 Windows 凭据。" );
    }
}
