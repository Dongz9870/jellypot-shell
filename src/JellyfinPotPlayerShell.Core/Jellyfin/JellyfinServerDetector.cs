using System.Net;
using System.Net.Sockets;
using System.Text;
using JellyfinPotPlayerShell.Core.Configuration;

namespace JellyfinPotPlayerShell.Core.Jellyfin;

public sealed class JellyfinServerDetector
{
    private static readonly string[] LocalCandidates =
    {
        "http://127.0.0.1:8096",
        "http://localhost:8096"
    };

    private readonly HttpClient _httpClient;

    public JellyfinServerDetector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> DetectAsync(
        string? configuredUrl,
        CancellationToken cancellationToken = default)
    {
        var checkedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var initialCandidates = new[] { configuredUrl }
            .Concat(LocalCandidates);

        foreach (var candidate in initialCandidates)
        {
            if (!JellyfinServerUrl.TryNormalize(
                    candidate,
                    out var normalized,
                    out _) ||
                !checkedUrls.Add(normalized))
            {
                continue;
            }

            if (await IsJellyfinServerAsync(normalized, cancellationToken))
            {
                return normalized;
            }
        }

        foreach (var discoveredUrl in await DiscoverLanServersAsync(cancellationToken))
        {
            if (!checkedUrls.Add(discoveredUrl))
            {
                continue;
            }

            if (await IsJellyfinServerAsync(discoveredUrl, cancellationToken))
            {
                return discoveredUrl;
            }
        }

        return null;
    }

    private async Task<bool> IsJellyfinServerAsync(
        string serverUrl,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            var endpoint = new Uri(
                serverUrl.TrimEnd('/') + "/System/Info/Public",
                UriKind.Absolute);
            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<string>> DiscoverLanServersAsync(
        CancellationToken cancellationToken)
    {
        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var client = new UdpClient(AddressFamily.InterNetwork)
            {
                EnableBroadcast = true
            };
            var payload = Encoding.UTF8.GetBytes("Who is JellyfinServer?");
            await client.SendAsync(
                payload,
                payload.Length,
                new IPEndPoint(IPAddress.Broadcast, 7359));

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(1500));

            while (!timeout.IsCancellationRequested)
            {
                UdpReceiveResult response;
                try
                {
                    response = await client.ReceiveAsync(timeout.Token);
                }
                catch (OperationCanceledException) when (
                    !cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var json = Encoding.UTF8.GetString(response.Buffer);
                if (JellyfinDiscoveryParser.TryParseAddress(
                        json,
                        out var address))
                {
                    addresses.Add(address);
                }
            }
        }
        catch (Exception exception) when (
            exception is SocketException or InvalidOperationException)
        {
            return Array.Empty<string>();
        }

        return addresses.ToArray();
    }
}
