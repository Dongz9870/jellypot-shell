using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Core.Setup;

public static class SetupPathMappingPlanner
{
    public static List<PathMappingRule> Build(
        IEnumerable<PathMappingRule>? existingRules,
        bool useMovieDrive,
        bool useTvDrive)
    {
        var knownPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePrefix(KnownMediaLocations.MovieRoot),
            NormalizePrefix(KnownMediaLocations.TvRoot),
            NormalizePrefix(KnownMediaLocations.NasRoot)
        };

        var result = (existingRules ?? Array.Empty<PathMappingRule>())
            .Where(rule => rule is not null)
            .Where(rule => !knownPrefixes.Contains(NormalizePrefix(rule.ServerPrefix)))
            .Select(rule => rule.Clone())
            .ToList();

        if (useMovieDrive)
        {
            result.Add(CreateRule(
                "电影短路径",
                KnownMediaLocations.MovieRoot,
                KnownMediaLocations.MovieDrive));
        }

        if (useTvDrive)
        {
            result.Add(CreateRule(
                "电视剧短路径",
                KnownMediaLocations.TvRoot,
                KnownMediaLocations.TvDrive));
        }

        result.Add(CreateRule(
            "UNC 原路径备用",
            KnownMediaLocations.NasRoot,
            KnownMediaLocations.NasRoot));

        return result;
    }

    private static PathMappingRule CreateRule(
        string description,
        string serverPrefix,
        string windowsPrefix)
    {
        return new PathMappingRule
        {
            Description = description,
            ServerPrefix = serverPrefix,
            WindowsPrefix = windowsPrefix
        };
    }

    private static string NormalizePrefix(string? path)
    {
        return (path ?? string.Empty)
            .Trim()
            .Trim('"')
            .Replace('/', '\\')
            .TrimEnd('\\');
    }
}
