using System.Net;
using System.Text;
using JellyfinPotPlayerShell.Core.Jellyfin;
using JellyfinPotPlayerShell.Core.Messaging;

namespace JellyfinPotPlayerShell.Tests;

public sealed class JellyfinApiServiceTests
{
    private const string ItemId = "0123456789abcdef0123456789abcdef";
    private const string UserId = "fedcba9876543210fedcba9876543210";
    private const string AccessToken = "secret-session-token";

    [Fact]
    public async Task GetItemAsync_UsesCurrentEndpointAndParsesPaths()
    {
        var handler = new RecordingHandler(CreateMediaResponse());
        var service = new JellyfinApiService(new HttpClient(handler));

        var item = await service.GetItemAsync(
            "https://media.example/jellyfin",
            CreateRequest());

        Assert.Equal(
            $"https://media.example/jellyfin/Items/{ItemId}?userId={UserId}",
            handler.RequestUri?.AbsoluteUri);
        Assert.Equal("MediaBrowser", handler.AuthorizationScheme);
        Assert.Equal($"Token=\"{AccessToken}\"", handler.AuthorizationParameter);
        Assert.DoesNotContain(
            AccessToken,
            handler.RequestUri?.AbsoluteUri ?? string.Empty,
            StringComparison.Ordinal);
        Assert.Equal(@"\\NAS-SERVER\Media\Movies\Film.mkv", item.Path);
        var source = Assert.Single(item.MediaSources);
        Assert.Equal("source-1", source.Id);
        Assert.Equal("4K REMUX", source.Name);
        Assert.Equal(@"\\NAS-SERVER\Media\Movies\Film.mkv", source.Path);
    }

    [Fact]
    public async Task GetItemAsync_UntrustedSessionOrigin_IsRejectedBeforeRequest()
    {
        var handler = new RecordingHandler(CreateMediaResponse());
        var service = new JellyfinApiService(new HttpClient(handler));
        var request = new PlayRequestMessage(
            ItemId,
            "https://attacker.example/jellyfin",
            UserId,
            AccessToken);

        var exception = await Assert.ThrowsAsync<JellyfinApiException>(() =>
            service.GetItemAsync("https://media.example/jellyfin", request));

        Assert.Contains("不匹配", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("", "secret-session-token")]
    [InlineData("fedcba9876543210fedcba9876543210", "")]
    public async Task GetItemAsync_MissingSessionValue_ShowsLoginExpired(
        string userId,
        string accessToken)
    {
        var handler = new RecordingHandler(CreateMediaResponse());
        var service = new JellyfinApiService(new HttpClient(handler));
        var request = new PlayRequestMessage(
            ItemId,
            "https://media.example/jellyfin",
            userId,
            accessToken);

        var exception = await Assert.ThrowsAsync<JellyfinApiException>(() =>
            service.GetItemAsync("https://media.example/jellyfin", request));

        Assert.Contains("登录状态已失效", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetItemAsync_UnauthorizedResponse_ShowsLoginExpired(
        HttpStatusCode statusCode)
    {
        var handler = new RecordingHandler(new HttpResponseMessage(statusCode));
        var service = new JellyfinApiService(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JellyfinApiException>(() =>
            service.GetItemAsync(
                "https://media.example/jellyfin",
                CreateRequest()));

        Assert.Contains("登录状态已失效", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, exception.ToString(), StringComparison.Ordinal);
    }

    private static PlayRequestMessage CreateRequest()
    {
        return new PlayRequestMessage(
            ItemId,
            "https://media.example/jellyfin",
            UserId,
            AccessToken);
    }

    private static HttpResponseMessage CreateMediaResponse()
    {
        const string json = """
            {
              "Id": "0123456789abcdef0123456789abcdef",
              "Name": "Film",
              "Type": "Movie",
              "Path": "\\\\NAS-SERVER\\Media\\Movies\\Film.mkv",
              "VideoType": "VideoFile",
              "Container": "mkv",
              "MediaSources": [
                {
                  "Id": "source-1",
                  "Name": "4K REMUX",
                  "Path": "\\\\NAS-SERVER\\Media\\Movies\\Film.mkv",
                  "VideoType": "VideoFile",
                  "Container": "mkv"
                }
              ]
            }
            """;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public int RequestCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(_response);
        }
    }
}
