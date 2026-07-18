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
}
