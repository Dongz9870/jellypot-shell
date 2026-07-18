using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Core.Paths;

public sealed class PathMappingService
{
    public PathMappingResult Map(
        string? sourcePath,
        IEnumerable<PathMappingRule>? rules)
    {
        var originalPath = sourcePath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sourcePath) || rules is null)
        {
            return new PathMappingResult(
                originalPath,
                originalPath,
                null,
                false);
        }

        var comparableSource = NormalizeForComparison(sourcePath);
        var matchingRule = rules
            .Where(rule => rule is not null && rule.Enabled)
            .Select(rule => new RuleCandidate(
                rule,
                NormalizePrefix(rule.ServerPrefix)))
            .Where(candidate =>
                candidate.ServerPrefix.Length > 0 &&
                IsPrefixMatch(comparableSource, candidate.ServerPrefix))
            .OrderByDescending(candidate => candidate.ServerPrefix.Length)
            .FirstOrDefault();

        if (matchingRule is null)
        {
            return new PathMappingResult(
                originalPath,
                originalPath,
                null,
                false);
        }

        var remainder = comparableSource[matchingRule.ServerPrefix.Length..]
            .TrimStart('\\');
        var mappedPath = CombineWindowsPath(
            matchingRule.Rule.WindowsPrefix,
            remainder);

        return new PathMappingResult(
            originalPath,
            mappedPath,
            matchingRule.Rule.Id,
            true);
    }

    public static bool TryValidateRule(PathMappingRule? rule, out string error)
    {
        error = string.Empty;
        if (rule is null)
        {
            error = "路径映射规则不能为空。";
            return false;
        }

        if (!rule.Enabled)
        {
            return true;
        }

        if (!IsAbsoluteServerPrefix(rule.ServerPrefix))
        {
            error = "Jellyfin 前缀必须是绝对路径，例如 UNC、盘符路径或 /media 路径。";
            return false;
        }

        if (!IsAbsoluteWindowsPrefix(rule.WindowsPrefix))
        {
            error = "Windows 前缀必须是盘符路径或 UNC 路径。";
            return false;
        }

        return true;
    }

    private static bool IsPrefixMatch(string sourcePath, string serverPrefix)
    {
        if (!sourcePath.StartsWith(
                serverPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return sourcePath.Length == serverPrefix.Length ||
            serverPrefix.EndsWith('\\') ||
            sourcePath[serverPrefix.Length] == '\\';
    }

    private static string NormalizeForComparison(string path)
    {
        return path.Trim().Trim('"').Replace('/', '\\');
    }

    private static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        var normalized = NormalizeForComparison(prefix).TrimEnd('\\');
        return normalized.Length == 0 && prefix.Contains('/')
            ? "\\"
            : normalized;
    }

    private static string CombineWindowsPath(string? windowsPrefix, string remainder)
    {
        var prefix = (windowsPrefix ?? string.Empty)
            .Trim()
            .Trim('"')
            .Replace('/', '\\')
            .TrimEnd('\\');

        if (remainder.Length == 0)
        {
            return prefix.EndsWith(':') ? prefix + "\\" : prefix;
        }

        return prefix + "\\" + remainder;
    }

    private static bool IsAbsoluteServerPrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            prefix.TrimStart().StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = prefix.Trim().Trim('"');
        return value.StartsWith('/') ||
            value.StartsWith(@"\\") ||
            IsDrivePath(value);
    }

    private static bool IsAbsoluteWindowsPrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            prefix.TrimStart().StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = prefix.Trim().Trim('"').Replace('/', '\\');
        return value.StartsWith(@"\\") || IsDrivePath(value);
    }

    private static bool IsDrivePath(string value)
    {
        return value.Length >= 3 &&
            char.IsAsciiLetter(value[0]) &&
            value[1] == ':' &&
            (value[2] == '\\' || value[2] == '/');
    }

    private sealed record RuleCandidate(
        PathMappingRule Rule,
        string ServerPrefix);
}
