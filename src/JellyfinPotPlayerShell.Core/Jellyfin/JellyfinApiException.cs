namespace JellyfinPotPlayerShell.Core.Jellyfin;

public sealed class JellyfinApiException : Exception
{
    public JellyfinApiException(string message)
        : base(message)
    {
    }
}
