using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Setup;

namespace JellyfinPotPlayerShell.Tests;

public sealed class SetupPathMappingPlannerTests
{
    [Fact]
    public void Build_CreatesMovieTvAndFallbackRules()
    {
        var rules = SetupPathMappingPlanner.Build(
            Array.Empty<PathMappingRule>(),
            useMovieDrive: true,
            useTvDrive: true);

        Assert.Collection(
            rules,
            rule => AssertRule(
                rule,
                KnownMediaLocations.MovieRoot,
                KnownMediaLocations.MovieDrive),
            rule => AssertRule(
                rule,
                KnownMediaLocations.TvRoot,
                KnownMediaLocations.TvDrive),
            rule => AssertRule(
                rule,
                KnownMediaLocations.NasRoot,
                KnownMediaLocations.NasRoot));
    }

    [Fact]
    public void Build_DoesNotSaveDriveRuleWhenDriveWasNotSelected()
    {
        var existing = new[]
        {
            new PathMappingRule
            {
                ServerPrefix = KnownMediaLocations.MovieRoot,
                WindowsPrefix = KnownMediaLocations.MovieDrive
            }
        };

        var rules = SetupPathMappingPlanner.Build(
            existing,
            useMovieDrive: false,
            useTvDrive: false);

        var rule = Assert.Single(rules);
        AssertRule(
            rule,
            KnownMediaLocations.NasRoot,
            KnownMediaLocations.NasRoot);
    }

    [Fact]
    public void Build_PreservesUnrelatedCustomRule()
    {
        var customRule = new PathMappingRule
        {
            Description = "自定义",
            ServerPrefix = @"\\other\media\",
            WindowsPrefix = @"D:\Media\"
        };

        var rules = SetupPathMappingPlanner.Build(
            new[] { customRule },
            useMovieDrive: true,
            useTvDrive: false);

        Assert.Equal(3, rules.Count);
        Assert.Contains(rules, rule =>
            rule.Id == customRule.Id &&
            rule.ServerPrefix == customRule.ServerPrefix);
        Assert.Contains(rules, rule =>
            rule.ServerPrefix == KnownMediaLocations.MovieRoot);
        Assert.Contains(rules, rule =>
            rule.ServerPrefix == KnownMediaLocations.NasRoot);
    }

    [Fact]
    public void Build_ReplacesKnownRulesWithoutCreatingDuplicates()
    {
        var existing = SetupPathMappingPlanner.Build(
            Array.Empty<PathMappingRule>(),
            useMovieDrive: true,
            useTvDrive: true);

        var rebuilt = SetupPathMappingPlanner.Build(
            existing,
            useMovieDrive: true,
            useTvDrive: true);

        Assert.Equal(3, rebuilt.Count);
        Assert.Equal(
            3,
            rebuilt.Select(rule => rule.ServerPrefix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    private static void AssertRule(
        PathMappingRule rule,
        string serverPrefix,
        string windowsPrefix)
    {
        Assert.True(rule.Enabled);
        Assert.Equal(serverPrefix, rule.ServerPrefix);
        Assert.Equal(windowsPrefix, rule.WindowsPrefix);
    }
}
