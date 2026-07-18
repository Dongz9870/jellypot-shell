using JellyfinPotPlayerShell.Core.Playback;

namespace JellyfinPotPlayerShell.Tests;

public sealed class PotPlayerPlaybackTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly string _playerPath;
    private readonly string _mediaPath;

    public PotPlayerPlaybackTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "JellyfinPotPlayerShell.Tests",
            Guid.NewGuid().ToString("N"));
        var playerDirectory = Path.Combine(_temporaryDirectory, "Player Folder");
        var mediaDirectory = Path.Combine(_temporaryDirectory, "Movie Folder");
        Directory.CreateDirectory(playerDirectory);
        Directory.CreateDirectory(mediaDirectory);

        _playerPath = Path.Combine(playerDirectory, PotPlayerExecutable.FileName);
        _mediaPath = Path.Combine(mediaDirectory, "Movie Name.mkv");
        File.WriteAllBytes(_playerPath, Array.Empty<byte>());
        File.WriteAllBytes(_mediaPath, Array.Empty<byte>());
    }

    [Fact]
    public void Create_ExistingMkv_UsesOneUnquotedArgument()
    {
        var startInfo = PotPlayerStartInfoFactory.Create(_playerPath, _mediaPath);

        Assert.Equal(Path.GetFullPath(_playerPath), startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        var argument = Assert.Single(startInfo.ArgumentList);
        Assert.Equal(_mediaPath, argument);
        Assert.False(argument.StartsWith('"'));
        Assert.False(argument.EndsWith('"'));
        Assert.Empty(startInfo.Arguments);
    }

    [Fact]
    public void Create_MissingMediaFile_IsRejectedBeforeLaunch()
    {
        var missingPath = Path.Combine(_temporaryDirectory, "missing.mkv");

        var exception = Assert.Throws<PotPlayerException>(() =>
            PotPlayerStartInfoFactory.Create(_playerPath, missingPath));

        Assert.Contains("无法访问", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Executable_WrongFileName_IsRejected()
    {
        var otherExecutable = Path.Combine(_temporaryDirectory, "other.exe");
        File.WriteAllBytes(otherExecutable, Array.Empty<byte>());

        var result = PotPlayerExecutable.TryValidate(
            otherExecutable,
            out _,
            out var error);

        Assert.False(result);
        Assert.Contains(PotPlayerExecutable.FileName, error, StringComparison.Ordinal);
    }

    [Fact]
    public void CandidateSelector_UsesFirstValidCandidate()
    {
        var secondPlayerDirectory = Path.Combine(_temporaryDirectory, "Second Player");
        Directory.CreateDirectory(secondPlayerDirectory);
        var secondPlayerPath = Path.Combine(
            secondPlayerDirectory,
            PotPlayerExecutable.FileName);
        File.WriteAllBytes(secondPlayerPath, Array.Empty<byte>());

        var result = PotPlayerCandidateSelector.FindFirstValid(new[]
        {
            Path.Combine(_temporaryDirectory, "missing", PotPlayerExecutable.FileName),
            _playerPath,
            secondPlayerPath
        });

        Assert.Equal(Path.GetFullPath(_playerPath), result);
    }

    public void Dispose()
    {
        var resolvedTemporaryDirectory = Path.GetFullPath(_temporaryDirectory);
        var resolvedTempRoot = Path.GetFullPath(Path.GetTempPath());
        if (resolvedTemporaryDirectory.StartsWith(
                resolvedTempRoot,
                StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(resolvedTemporaryDirectory))
        {
            Directory.Delete(resolvedTemporaryDirectory, true);
        }
    }
}
