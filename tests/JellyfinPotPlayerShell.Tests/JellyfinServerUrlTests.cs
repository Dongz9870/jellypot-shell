using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Tests;

public sealed class JellyfinServerUrlTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8096", "http://127.0.0.1:8096")]
    [InlineData(" https://media.example.com/jellyfin/ ", "https://media.example.com/jellyfin")]
    [InlineData("HTTP://LOCALHOST:8096/", "http://localhost:8096")]
    public void TryNormalize_ValidAddress_ReturnsNormalizedUrl(string input, string expected)
    {
        var result = JellyfinServerUrl.TryNormalize(input, out var normalized, out var error);

        Assert.True(result, error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:8096")]
    [InlineData("ftp://media.example.com")]
    [InlineData("http://user:password@media.example.com")]
    [InlineData("https://media.example.com/?api_key=secret")]
    [InlineData("https://media.example.com/#fragment")]
    public void TryNormalize_InvalidAddress_ReturnsError(string input)
    {
        var result = JellyfinServerUrl.TryNormalize(input, out var normalized, out var error);

        Assert.False(result);
        Assert.Empty(normalized);
        Assert.NotEmpty(error);
    }
}
