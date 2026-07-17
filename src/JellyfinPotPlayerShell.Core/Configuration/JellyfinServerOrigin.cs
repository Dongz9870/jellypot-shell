namespace JellyfinPotPlayerShell.Core.Configuration;

public static class JellyfinServerOrigin
{
    public static bool Matches(string? configuredServerUrl, string? messageSource)
    {
        return Uri.TryCreate(
                   configuredServerUrl,
                   UriKind.Absolute,
                   out var configuredUri) &&
            Uri.TryCreate(messageSource, UriKind.Absolute, out var sourceUri) &&
            sourceUri.Scheme.Equals(
                configuredUri.Scheme,
                StringComparison.OrdinalIgnoreCase) &&
            sourceUri.IdnHost.Equals(
                configuredUri.IdnHost,
                StringComparison.OrdinalIgnoreCase) &&
            sourceUri.Port == configuredUri.Port;
    }
}
