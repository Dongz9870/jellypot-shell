using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Paths;

namespace JellyfinPotPlayerShell.Tests;

public sealed class PathMappingServiceTests
{
    private readonly PathMappingService _service = new();

    [Fact]
    public void Map_MapsUncPrefixToDrive()
    {
        var rule = Rule(
            @"\\NAS-SERVER\Media\Movies\",
            @"M:\");

        var result = _service.Map(
            @"\\NAS-SERVER\Media\Movies\电影\正片.mkv",
            new[] { rule });

        Assert.True(result.IsMapped);
        Assert.Equal(rule.Id, result.RuleId);
        Assert.Equal(@"M:\电影\正片.mkv", result.MappedPath);
    }

    [Fact]
    public void Map_MapsUncPrefixToUncPrefix()
    {
        var rule = Rule(
            @"\\jellyfin\media\",
            @"\\NAS-SERVER\Media\");

        var result = _service.Map(
            @"\\jellyfin\media\TVShow\剧集\S01E01.mkv",
            new[] { rule });

        Assert.Equal(
            @"\\NAS-SERVER\Media\TVShow\剧集\S01E01.mkv",
            result.MappedPath);
    }

    [Fact]
    public void Map_MapsLinuxPrefixToDrive()
    {
        var rule = Rule("/media/tv/", @"T:\");

        var result = _service.Map(
            "/media/tv/Show/S01E01.mkv",
            new[] { rule });

        Assert.Equal(@"T:\Show\S01E01.mkv", result.MappedPath);
    }

    [Fact]
    public void Map_IsCaseInsensitive()
    {
        var rule = Rule(@"\\NAS\MOVIES\", @"M:\");

        var result = _service.Map(
            @"\\nas\movies\Film.mkv",
            new[] { rule });

        Assert.True(result.IsMapped);
        Assert.Equal(@"M:\Film.mkv", result.MappedPath);
    }

    [Fact]
    public void Map_UsesLongestMatchingPrefix()
    {
        var rootRule = Rule(@"\\NAS-SERVER\Media\", @"Z:\");
        var movieRule = Rule(
            @"\\NAS-SERVER\Media\Movies\",
            @"M:\");

        var result = _service.Map(
            @"\\NAS-SERVER\Media\Movies\Film.mkv",
            new[] { rootRule, movieRule });

        Assert.Equal(movieRule.Id, result.RuleId);
        Assert.Equal(@"M:\Film.mkv", result.MappedPath);
    }

    [Fact]
    public void Map_IgnoresDisabledRule()
    {
        var disabledRule = Rule(
            @"\\NAS-SERVER\Media\Movies\",
            @"M:\");
        disabledRule.Enabled = false;
        var rootRule = Rule(@"\\NAS-SERVER\Media\", @"Z:\");

        var result = _service.Map(
            @"\\NAS-SERVER\Media\Movies\Film.mkv",
            new[] { disabledRule, rootRule });

        Assert.Equal(rootRule.Id, result.RuleId);
        Assert.Equal(@"Z:\Movies\Film.mkv", result.MappedPath);
    }

    [Fact]
    public void Map_DoesNotMatchPartialDirectoryName()
    {
        var rule = Rule(@"\\nas\share\movie", @"M:\");
        const string source = @"\\nas\share\movie-archive\Film.mkv";

        var result = _service.Map(source, new[] { rule });

        Assert.False(result.IsMapped);
        Assert.Equal(source, result.MappedPath);
    }

    [Fact]
    public void Map_ReturnsOriginalPathWhenNoRuleMatches()
    {
        var rule = Rule(@"\\nas\movies\", @"M:\");
        const string source = @"\\nas\tv\Show\Episode.mkv";

        var result = _service.Map(source, new[] { rule });

        Assert.False(result.IsMapped);
        Assert.Null(result.RuleId);
        Assert.Equal(source, result.MappedPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Map_HandlesEmptySourcePath(string? source)
    {
        var result = _service.Map(
            source,
            new[] { Rule(@"\\nas\movies\", @"M:\") });

        Assert.False(result.IsMapped);
        Assert.Equal(source ?? string.Empty, result.MappedPath);
    }

    [Fact]
    public void Map_NormalizesTrailingSeparators()
    {
        var rule = Rule(@"\\nas\movies\\", @"M:\\");

        var result = _service.Map(
            @"\\nas\movies\Film.mkv",
            new[] { rule });

        Assert.Equal(@"M:\Film.mkv", result.MappedPath);
    }

    [Theory]
    [InlineData(@"\\nas\share\", @"M:\")]
    [InlineData(@"\\nas\share\", @"\\server\share\")]
    [InlineData("/media/movies/", @"M:\")]
    [InlineData(@"D:\Media\", @"M:\")]
    public void TryValidateRule_AcceptsSupportedAbsolutePaths(
        string serverPrefix,
        string windowsPrefix)
    {
        var result = PathMappingService.TryValidateRule(
            Rule(serverPrefix, windowsPrefix),
            out var error);

        Assert.True(result);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData("relative/path", @"M:\")]
    [InlineData(@"\\nas\share\", "relative")]
    [InlineData("file:///?/UNC/nas/share", @"M:\")]
    [InlineData(@"\\nas\share\", "file:///?/UNC/server/share")]
    public void TryValidateRule_RejectsUnsupportedPaths(
        string serverPrefix,
        string windowsPrefix)
    {
        var result = PathMappingService.TryValidateRule(
            Rule(serverPrefix, windowsPrefix),
            out var error);

        Assert.False(result);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void TryValidateRule_AllowsIncompleteDisabledRule()
    {
        var rule = Rule(string.Empty, string.Empty);
        rule.Enabled = false;

        Assert.True(PathMappingService.TryValidateRule(rule, out var error));
        Assert.Empty(error);
    }

    private static PathMappingRule Rule(
        string serverPrefix,
        string windowsPrefix)
    {
        return new PathMappingRule
        {
            ServerPrefix = serverPrefix,
            WindowsPrefix = windowsPrefix
        };
    }
}
