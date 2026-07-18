using System.Text.Json;
using JellyfinPotPlayerShell.Core.Jellyfin;

namespace JellyfinPotPlayerShell.Tests;

public sealed class HdrMediaDetectorTests
{
    [Theory]
    [InlineData("HDR", null)]
    [InlineData(null, "HDR10")]
    [InlineData(null, "HDR10Plus")]
    [InlineData(null, "HLG")]
    [InlineData(null, "DOVI")]
    [InlineData(null, "DOVIWithHDR10")]
    [InlineData("2", "5")]
    public void IsHdr_HdrRangeMetadata_ReturnsTrue(
        string? videoRange,
        string? videoRangeType)
    {
        var source = CreateSource(new JellyfinMediaStream
        {
            Type = "Video",
            VideoRange = videoRange,
            VideoRangeType = videoRangeType
        });

        var result = HdrMediaDetector.IsHdr(new JellyfinMediaItem(), source);

        Assert.True(result);
    }

    [Theory]
    [InlineData("smpte2084")]
    [InlineData("arib-std-b67")]
    public void IsHdr_HdrTransferFunction_ReturnsTrue(string colorTransfer)
    {
        var source = CreateSource(new JellyfinMediaStream
        {
            Type = "Video",
            ColorTransfer = colorTransfer
        });

        Assert.True(HdrMediaDetector.IsHdr(new JellyfinMediaItem(), source));
    }

    [Fact]
    public void IsHdr_DolbyVisionMetadata_ReturnsTrue()
    {
        var source = CreateSource(new JellyfinMediaStream
        {
            Type = "Video",
            DvProfile = 8,
            RpuPresentFlag = 1,
            BlPresentFlag = 1,
            DvBlSignalCompatibilityId = 1
        });

        Assert.True(HdrMediaDetector.IsHdr(new JellyfinMediaItem(), source));
    }

    [Theory]
    [InlineData("SDR", "SDR")]
    [InlineData("1", "1")]
    [InlineData("SDR", "DOVIWithSDR")]
    public void IsHdr_SdrRangeMetadata_ReturnsFalse(
        string videoRange,
        string videoRangeType)
    {
        var source = CreateSource(new JellyfinMediaStream
        {
            Type = "Video",
            VideoRange = videoRange,
            VideoRangeType = videoRangeType
        });

        Assert.False(HdrMediaDetector.IsHdr(new JellyfinMediaItem(), source));
    }

    [Fact]
    public void IsHdr_SelectedSdrVersion_DoesNotUseOtherHdrMetadata()
    {
        var item = new JellyfinMediaItem
        {
            MediaStreams = new[]
            {
                new JellyfinMediaStream
                {
                    Type = "Video",
                    VideoRange = "HDR",
                    VideoRangeType = "HDR10"
                }
            }
        };
        var selectedSdrSource = CreateSource(new JellyfinMediaStream
        {
            Type = "Video",
            VideoRange = "SDR",
            VideoRangeType = "SDR"
        });

        Assert.False(HdrMediaDetector.IsHdr(item, selectedSdrSource));
    }

    [Fact]
    public void IsHdr_FileNameOnlyContainsHdr_DoesNotGuess()
    {
        var item = new JellyfinMediaItem
        {
            Name = "Movie.HDR10.mkv",
            Path = @"\\server\movies\Movie.HDR10.mkv"
        };

        Assert.False(HdrMediaDetector.IsHdr(item));
    }

    [Fact]
    public void FlexibleEnumValues_NumericJson_DeserializesAndDetectsHdr()
    {
        const string json = """
            {
              "Type": 1,
              "VideoRange": 2,
              "VideoRangeType": 5
            }
            """;

        var stream = JsonSerializer.Deserialize<JellyfinMediaStream>(json);

        Assert.NotNull(stream);
        Assert.Equal("1", stream.Type);
        Assert.Equal("2", stream.VideoRange);
        Assert.Equal("5", stream.VideoRangeType);
        Assert.True(HdrMediaDetector.IsHdr(
            new JellyfinMediaItem(),
            CreateSource(stream)));
    }

    private static JellyfinMediaSource CreateSource(JellyfinMediaStream stream)
    {
        return new JellyfinMediaSource
        {
            Id = "source",
            Path = @"\\server\movies\movie.mkv",
            MediaStreams = new[] { stream }
        };
    }
}
