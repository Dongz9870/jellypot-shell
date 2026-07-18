using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JellyfinPotPlayerShell.Core.Configuration;
using JellyfinPotPlayerShell.Core.Messaging;

namespace JellyfinPotPlayerShell.Core.Jellyfin;

public sealed class JellyfinApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web);
    private static readonly Regex SessionValuePattern = new(
        "^[A-Za-z0-9._~+/=-]+$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private readonly HttpClient _httpClient;

    public JellyfinApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JellyfinMediaItem> GetItemAsync(
        string configuredServerUrl,
        PlayRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.AccessToken))
        {
            throw new JellyfinApiException(
                "Jellyfin 登录状态已失效。请在当前窗口重新登录后再点击黄色按钮。");
        }

        if (!JellyfinServerOrigin.Matches(
                configuredServerUrl,
                request.ServerAddress))
        {
            throw new JellyfinApiException("网页会话与配置的 Jellyfin 服务器不匹配。");
        }

        if (!IsSafeSessionValue(request.UserId) ||
            !IsSafeSessionValue(request.AccessToken))
        {
            throw new JellyfinApiException("Jellyfin 登录会话格式无效，请重新登录后再试。");
        }

        if (!Uri.TryCreate(
                configuredServerUrl.TrimEnd('/') + "/",
                UriKind.Absolute,
                out var serverUri))
        {
            throw new JellyfinApiException("配置的 Jellyfin 服务器地址无效。");
        }

        var relativePath = $"Items/{Uri.EscapeDataString(request.ItemId)}" +
            $"?userId={Uri.EscapeDataString(request.UserId)}";
        var requestUri = new Uri(serverUri, relativePath);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "MediaBrowser",
            $"Token=\"{request.AccessToken}\"");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new JellyfinApiException("连接 Jellyfin 超时，请检查服务器状态后重试。");
        }
        catch (HttpRequestException)
        {
            throw new JellyfinApiException("无法连接 Jellyfin，请检查服务器地址和网络状态。");
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new JellyfinApiException(
                    "Jellyfin 登录状态已失效。请在当前窗口重新登录后再点击黄色按钮。");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new JellyfinApiException(
                    $"Jellyfin 返回错误（HTTP {(int)response.StatusCode}）。");
            }

            JellyfinMediaItem? item;
            try
            {
                item = await response.Content.ReadFromJsonAsync<JellyfinMediaItem>(
                    JsonOptions,
                    cancellationToken);
            }
            catch (JsonException)
            {
                throw new JellyfinApiException("Jellyfin 返回了无法识别的媒体信息。");
            }

            if (item is null || string.IsNullOrWhiteSpace(item.Id))
            {
                throw new JellyfinApiException("Jellyfin 未返回有效的媒体条目。");
            }

            return item;
        }
    }

    private static bool IsSafeSessionValue(string value)
    {
        try
        {
            return value.Length <= 4096 && SessionValuePattern.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
