namespace JellyfinPotPlayerShell.Core.Messaging;

public sealed class PlayRequestMessage
{
    public PlayRequestMessage(
        string itemId,
        string serverAddress,
        string userId,
        string accessToken)
    {
        ItemId = itemId;
        ServerAddress = serverAddress;
        UserId = userId;
        AccessToken = accessToken;
    }

    public string ItemId { get; }

    public string ServerAddress { get; }

    public string UserId { get; }

    public string AccessToken { get; }

    public override string ToString()
    {
        return nameof(PlayRequestMessage);
    }
}
