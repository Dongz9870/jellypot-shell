using System.Text.Json;
using System.Text.RegularExpressions;

namespace JellyfinPotPlayerShell.Core.Messaging;

public static class WebBridgeMessageParser
{
    public const string PlayMessageType = "playWithPotPlayer";

    private const int MaximumMessageLength = 8192;
    private const int MaximumServerAddressLength = 2048;
    private const int MaximumAccessTokenLength = 4096;
    private static readonly Regex ItemIdPattern = new(
        "^[a-fA-F0-9-]{16,64}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex AccessTokenPattern = new(
        "^[A-Za-z0-9._~+/=-]+$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static bool TryParsePlayRequest(
        string? json,
        out PlayRequestMessage? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumMessageLength)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            string? type = null;
            string? itemId = null;
            string? serverAddress = null;
            string? userId = null;
            string? accessToken = null;
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!propertyNames.Add(property.Name) ||
                    !IsAllowedProperty(property.Name) ||
                    property.Value.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                switch (property.Name)
                {
                    case "type":
                        type = property.Value.GetString();
                        break;
                    case "itemId":
                        itemId = property.Value.GetString();
                        break;
                    case "serverAddress":
                        serverAddress = property.Value.GetString();
                        break;
                    case "userId":
                        userId = property.Value.GetString();
                        break;
                    case "accessToken":
                        accessToken = property.Value.GetString();
                        break;
                }
            }

            if (type != PlayMessageType ||
                string.IsNullOrEmpty(itemId) ||
                !ItemIdPattern.IsMatch(itemId) ||
                !IsValidServerAddress(serverAddress) ||
                userId is null ||
                (userId.Length > 0 && !ItemIdPattern.IsMatch(userId)) ||
                accessToken is null ||
                accessToken.Length > MaximumAccessTokenLength ||
                (accessToken.Length > 0 && !AccessTokenPattern.IsMatch(accessToken)))
            {
                return false;
            }

            message = new PlayRequestMessage(
                itemId,
                serverAddress!,
                userId,
                accessToken);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool IsAllowedProperty(string propertyName)
    {
        return propertyName is
            "type" or
            "itemId" or
            "serverAddress" or
            "userId" or
            "accessToken";
    }

    private static bool IsValidServerAddress(string? serverAddress)
    {
        return !string.IsNullOrWhiteSpace(serverAddress) &&
            serverAddress.Length <= MaximumServerAddressLength &&
            Uri.TryCreate(serverAddress, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            string.IsNullOrEmpty(uri.Query) &&
            string.IsNullOrEmpty(uri.Fragment);
    }
}
