namespace JellyfinPotPlayerShell.Core.Jellyfin;

public static class HdrMediaDetector
{
    private static readonly string[] HdrMarkers =
    {
        "HDR",
        "HLG",
        "DOVI",
        "DOLBY VISION",
        "DOLBYVISION",
        "DV PROFILE"
    };

    public static bool IsHdr(
        JellyfinMediaItem item,
        JellyfinMediaSource? selectedSource = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (selectedSource is not null)
        {
            var selectedDecision = Detect(
                selectedSource.VideoRange,
                selectedSource.VideoRangeType,
                selectedSource.MediaStreams);
            if (selectedDecision.HasValue)
            {
                return selectedDecision.Value;
            }
        }

        return Detect(item.VideoRange, item.VideoRangeType, item.MediaStreams) ?? false;
    }

    private static bool? Detect(
        string? videoRange,
        string? videoRangeType,
        IReadOnlyList<JellyfinMediaStream>? streams)
    {
        var streamDecision = DetectStreams(streams);
        if (streamDecision.HasValue)
        {
            return streamDecision;
        }

        return DetectRange(videoRange, videoRangeType);
    }

    private static bool? DetectStreams(
        IReadOnlyList<JellyfinMediaStream>? streams)
    {
        if (streams is null || streams.Count == 0)
        {
            return null;
        }

        var foundSdr = false;
        foreach (var stream in streams)
        {
            if (!IsPotentialVideoStream(stream))
            {
                continue;
            }

            var decision = DetectStream(stream);
            if (decision == true)
            {
                return true;
            }

            foundSdr |= decision == false;
        }

        return foundSdr ? false : null;
    }

    private static bool? DetectStream(JellyfinMediaStream stream)
    {
        var rangeDecision = DetectRange(stream.VideoRange, stream.VideoRangeType);
        if (rangeDecision.HasValue)
        {
            return rangeDecision;
        }

        if (stream.Hdr10PlusPresentFlag == true ||
            EqualsAny(stream.ColorTransfer, "smpte2084", "arib-std-b67"))
        {
            return true;
        }

        if (stream.DvProfile is 5 or 7 or 8 or 10 &&
            stream.RpuPresentFlag == 1 &&
            stream.BlPresentFlag == 1)
        {
            return stream.DvBlSignalCompatibilityId == 2 ? false : true;
        }

        if (EqualsAny(stream.CodecTag, "dovi", "dvh1", "dvhe", "dav1") ||
            ContainsHdrMarker(stream.DisplayTitle))
        {
            return true;
        }

        return null;
    }

    private static bool? DetectRange(string? videoRange, string? videoRangeType)
    {
        if (EqualsAny(videoRange, "SDR", "1"))
        {
            return false;
        }

        if (EqualsAny(videoRange, "HDR", "2"))
        {
            return true;
        }

        if (Contains(videoRangeType, "WITHSDR") ||
            EqualsAny(videoRangeType, "SDR", "1"))
        {
            return false;
        }

        return ContainsHdrMarker(videoRangeType) ? true : null;
    }

    private static bool IsPotentialVideoStream(JellyfinMediaStream stream)
    {
        return string.IsNullOrWhiteSpace(stream.Type) ||
            EqualsAny(stream.Type, "Video", "1");
    }

    private static bool ContainsHdrMarker(string? value)
    {
        return HdrMarkers.Any(marker => Contains(value, marker));
    }

    private static bool Contains(string? value, string marker)
    {
        return value?.Contains(marker, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool EqualsAny(string? value, params string[] candidates)
    {
        return candidates.Any(candidate => string.Equals(
            value,
            candidate,
            StringComparison.OrdinalIgnoreCase));
    }
}
