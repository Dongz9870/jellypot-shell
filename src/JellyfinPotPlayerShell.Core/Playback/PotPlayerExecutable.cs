namespace JellyfinPotPlayerShell.Core.Playback;

public static class PotPlayerExecutable
{
    public const string FileName = "PotPlayerMini64.exe";

    public static bool TryValidate(
        string? executablePath,
        out string normalizedPath,
        out string error)
    {
        normalizedPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            error = "没有找到 PotPlayerMini64.exe。";
            return false;
        }

        var candidate = Environment.ExpandEnvironmentVariables(
            executablePath.Trim().Trim('"'));

        try
        {
            if (!Path.IsPathFullyQualified(candidate) ||
                !Path.GetFileName(candidate).Equals(
                    FileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "只能选择 PotPlayerMini64.exe。";
                return false;
            }

            candidate = Path.GetFullPath(candidate);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = "PotPlayer 路径格式无效。";
            return false;
        }

        if (!File.Exists(candidate))
        {
            error = "指定的 PotPlayerMini64.exe 不存在。";
            return false;
        }

        normalizedPath = candidate;
        return true;
    }
}
