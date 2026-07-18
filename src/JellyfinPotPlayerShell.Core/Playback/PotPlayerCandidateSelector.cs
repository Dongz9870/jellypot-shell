namespace JellyfinPotPlayerShell.Core.Playback;

public static class PotPlayerCandidateSelector
{
    public static string? FindFirstValid(IEnumerable<string?> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        foreach (var candidate in candidates)
        {
            if (PotPlayerExecutable.TryValidate(
                    candidate,
                    out var normalizedPath,
                    out _))
            {
                return normalizedPath;
            }
        }

        return null;
    }
}
