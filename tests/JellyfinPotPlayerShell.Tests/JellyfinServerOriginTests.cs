using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Tests;

public sealed class JellyfinServerOriginTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8096", "http://127.0.0.1:8096/web/#/details", true)]
    [InlineData("https://MEDIA.example.com/jellyfin", "https://media.example.com/web", true)]
    [InlineData("https://media.example.com", "http://media.example.com", false)]
    [InlineData("https://media.example.com", "https://other.example.com", false)]
    [InlineData("https://media.example.com:8443", "https://media.example.com", false)]
    [InlineData("https://media.example.com", "not-a-url", false)]
    public void Matches_ValidatesSchemeHostAndPort(
        string configuredServerUrl,
        string messageSource,
        bool expected)
    {
        var result = JellyfinServerOrigin.Matches(
            configuredServerUrl,
            messageSource);

        Assert.Equal(expected, result);
    }
}
