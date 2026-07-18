namespace JellyfinPotPlayerShell.Core.Playback;

public sealed class PotPlayerException : Exception
{
    public PotPlayerException(string message)
        : base(message)
    {
    }
}
