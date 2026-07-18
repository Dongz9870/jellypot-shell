using JellyfinPotPlayerShell.Core.Playback;

namespace JellyfinPotPlayerShell.Tests;

public sealed class MediaPathNormalizerTests
{
    [Theory]
    [InlineData(
        @"\\?\UNC\NAS-SERVER\Media\movie.mkv",
        @"\\NAS-SERVER\Media\movie.mkv")]
    [InlineData(@"\\?\C:\Movies\movie.mkv", @"C:\Movies\movie.mkv")]
    [InlineData(@"\\NAS-SERVER\Media\movie.mkv", @"\\NAS-SERVER\Media\movie.mkv")]
    [InlineData("C:/Movies/movie.mkv", @"C:\Movies\movie.mkv")]
    [InlineData("  \"C:\\Movies\\movie.mkv\"  ", @"C:\Movies\movie.mkv")]
    public void NormalizeForPlayer_WindowsPath_ReturnsPlainPath(
        string source,
        string expected)
    {
        var result = MediaPathNormalizer.NormalizeForPlayer(source);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("file://", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeForPlayer_EmptyPath_IsRejected(string? source)
    {
        Assert.Throws<PotPlayerException>(() =>
            MediaPathNormalizer.NormalizeForPlayer(source));
    }

    [Theory]
    [InlineData("file:///C:/Movies/movie.mkv")]
    [InlineData("file:///?/UNC/NAS-SERVER/Media/movie.mkv")]
    public void NormalizeForPlayer_FileUri_IsRejected(string source)
    {
        var exception = Assert.Throws<PotPlayerException>(() =>
            MediaPathNormalizer.NormalizeForPlayer(source));

        Assert.Contains("file URI", exception.Message, StringComparison.Ordinal);
    }
}
