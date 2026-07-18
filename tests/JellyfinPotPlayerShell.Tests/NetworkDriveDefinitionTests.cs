using JellyfinPotPlayerShell.Core.Networking;

namespace JellyfinPotPlayerShell.Tests;

public sealed class NetworkDriveDefinitionTests
{
    [Theory]
    [InlineData("m", "M:", "M:\\")]
    [InlineData("m:", "M:", "M:\\")]
    [InlineData("M:\\", "M:", "M:\\")]
    public void TryCreate_NormalizesDriveLetter(
        string input,
        string expectedName,
        string expectedRoot)
    {
        var result = NetworkDriveDefinition.TryCreate(
            input,
            @"\\server\share\folder\",
            out var definition,
            out var error);

        Assert.True(result);
        Assert.NotNull(definition);
        Assert.Equal(expectedName, definition.DriveName);
        Assert.Equal(expectedRoot, definition.DriveRoot);
        Assert.Equal(@"\\server\share\folder", definition.RemotePath);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MM:")]
    [InlineData("1:")]
    [InlineData("M:\\folder")]
    public void TryCreate_RejectsInvalidDriveLetter(string driveLetter)
    {
        var result = NetworkDriveDefinition.TryCreate(
            driveLetter,
            @"\\server\share",
            out var definition,
            out var error);

        Assert.False(result);
        Assert.Null(definition);
        Assert.NotEmpty(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData(@"M:\Media")]
    [InlineData(@"\\server")]
    [InlineData(@"\\?\UNC\server\share")]
    [InlineData("file:///?/UNC/server/share")]
    public void TryCreate_RejectsNonUncRemotePath(string remotePath)
    {
        var result = NetworkDriveDefinition.TryCreate(
            "M:",
            remotePath,
            out var definition,
            out var error);

        Assert.False(result);
        Assert.Null(definition);
        Assert.NotEmpty(error);
    }
}
