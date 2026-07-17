using System.Text.Json;
using System.Text.RegularExpressions;

namespace JellyfinPotPlayerShell.Core.Messaging;

public static class WebBridgeMessageParser
{
    public const string PlayMessageType = "playWithPotPlayer";

    private const int MaximumMessageLength = 512;
    private static readonly Regex ItemIdPattern = new(
        "^[a-fA-F0-9-]{16,64}$",
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
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!propertyNames.Add(property.Name) ||
                    (property.Name != "type" && property.Name != "itemId") ||
                    property.Value.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                if (property.Name == "type")
                {
                    type = property.Value.GetString();
                }
                else
                {
                    itemId = property.Value.GetString();
                }
            }

            if (type != PlayMessageType ||
                string.IsNullOrEmpty(itemId) ||
                !ItemIdPattern.IsMatch(itemId))
            {
                return false;
            }

            message = new PlayRequestMessage(itemId);
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
}
