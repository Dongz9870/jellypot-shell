using System.Text.Json.Serialization;

namespace JellyfinPotPlayerShell.Core.Jellyfin;

public sealed class JellyfinMediaItem
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Type { get; init; }

    public string? Path { get; init; }

    public string? VideoType { get; init; }

    public string? Container { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRange { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRangeType { get; init; }

    public IReadOnlyList<JellyfinMediaStream> MediaStreams { get; init; } =
        Array.Empty<JellyfinMediaStream>();

    [JsonPropertyName("MediaSources")]
    public IReadOnlyList<JellyfinMediaSource> MediaSources { get; init; } =
        Array.Empty<JellyfinMediaSource>();
}

public sealed class JellyfinMediaSource
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Path { get; init; }

    public string? VideoType { get; init; }

    public string? Container { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRange { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRangeType { get; init; }

    public IReadOnlyList<JellyfinMediaStream> MediaStreams { get; init; } =
        Array.Empty<JellyfinMediaStream>();
}

public sealed class JellyfinMediaStream
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Type { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRange { get; init; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? VideoRangeType { get; init; }

    public string? ColorTransfer { get; init; }

    public string? CodecTag { get; init; }

    public string? DisplayTitle { get; init; }

    public int? DvProfile { get; init; }

    public int? RpuPresentFlag { get; init; }

    public int? BlPresentFlag { get; init; }

    public int? DvBlSignalCompatibilityId { get; init; }

    public bool? Hdr10PlusPresentFlag { get; init; }
}
