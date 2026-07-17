namespace JellyfinPotPlayerShell.App.Services;

public sealed class PlayRequestReceivedEventArgs : EventArgs
{
    public PlayRequestReceivedEventArgs(string itemId)
    {
        ItemId = itemId;
    }

    public string ItemId { get; }
}
