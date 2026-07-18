using JellyfinPotPlayerShell.Core.Messaging;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class PlayRequestReceivedEventArgs : EventArgs
{
    public PlayRequestReceivedEventArgs(PlayRequestMessage request)
    {
        Request = request;
    }

    public PlayRequestMessage Request { get; }
}
