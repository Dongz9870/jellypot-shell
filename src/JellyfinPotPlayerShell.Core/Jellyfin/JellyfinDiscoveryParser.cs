using System.Text.Json;
using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Core.Jellyfin;

public static class JellyfinDiscoveryParser
{
    public static bool TryParseAddress(
        string? json,
        out string normalizedAddress)
    {
        normalizedAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
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

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals(
                        "Address",
                        StringComparison.OrdinalIgnoreCase) ||
                    property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                return JellyfinServerUrl.TryNormalize(
                    property.Value.GetString(),
                    out normalizedAddress,
                    out _);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
