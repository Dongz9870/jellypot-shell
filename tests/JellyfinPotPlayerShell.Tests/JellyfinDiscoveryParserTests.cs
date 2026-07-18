using JellyfinPotPlayerShell.Core.Jellyfin;

namespace JellyfinPotPlayerShell.Tests;

public sealed class JellyfinDiscoveryParserTests
{
    [Theory]
    [InlineData(
        "{\"Address\":\"http://192.0.2.10:8096/\",\"Name\":\"NAS\"}",
        "http://192.0.2.10:8096")]
    [InlineData(
        "{\"address\":\"https://jellyfin.example.com/base/\"}",
        "https://jellyfin.example.com/base")]
    public void TryParseAddress_ReturnsNormalizedServerUrl(
        string json,
        string expected)
    {
        var result = JellyfinDiscoveryParser.TryParseAddress(
            json,
            out var address);

        Assert.True(result);
        Assert.Equal(expected, address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"Address\":\"file://server/share\"}")]
    public void TryParseAddress_RejectsInvalidDiscoveryPayload(string? json)
    {
        var result = JellyfinDiscoveryParser.TryParseAddress(
            json,
            out var address);

        Assert.False(result);
        Assert.Empty(address);
    }
}
