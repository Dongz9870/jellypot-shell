using JellyfinPotPlayerShell.Core.Messaging;

namespace JellyfinPotPlayerShell.Tests;

public sealed class WebBridgeMessageParserTests
{
    private const string ItemId = "0123456789abcdef0123456789abcdef";
    private const string UserId = "fedcba9876543210fedcba9876543210";
    private const string AccessToken = "secret-session-token";

    [Fact]
    public void TryParsePlayRequest_ValidSessionMessage_ReturnsExpectedValues()
    {
        var result = WebBridgeMessageParser.TryParsePlayRequest(
            CreateValidJson(),
            out var message);

        Assert.True(result);
        Assert.NotNull(message);
        Assert.Equal(ItemId, message.ItemId);
        Assert.Equal("https://media.example/jellyfin", message.ServerAddress);
        Assert.Equal(UserId, message.UserId);
        Assert.Equal(AccessToken, message.AccessToken);
    }

    [Fact]
    public void TryParsePlayRequest_EmptySessionValues_RemainsActionable()
    {
        var json = $$"""
            {
              "type": "playWithPotPlayer",
              "itemId": "{{ItemId}}",
              "serverAddress": "https://media.example/jellyfin",
              "userId": "",
              "accessToken": ""
            }
            """;

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.True(result);
        Assert.NotNull(message);
        Assert.Empty(message.UserId);
        Assert.Empty(message.AccessToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"type\":\"other\"}")]
    public void TryParsePlayRequest_InvalidEnvelope_IsRejected(string? json)
    {
        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Theory]
    [InlineData("itemId")]
    [InlineData("serverAddress")]
    [InlineData("userId")]
    [InlineData("accessToken")]
    public void TryParsePlayRequest_MissingRequiredField_IsRejected(string fieldName)
    {
        var json = CreateValidJson()
            .Replace($"  \"{fieldName}\": \"{GetFieldValue(fieldName)}\",\n", string.Empty)
            .Replace($",\n  \"{fieldName}\": \"{GetFieldValue(fieldName)}\"", string.Empty);

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Theory]
    [InlineData("http://user:password@media.example")]
    [InlineData("file:///media.example/movie")]
    [InlineData("https://media.example/jellyfin?token=secret")]
    [InlineData("https://media.example/jellyfin#fragment")]
    public void TryParsePlayRequest_UnsafeServerAddress_IsRejected(string serverAddress)
    {
        var json = CreateValidJson().Replace(
            "https://media.example/jellyfin",
            serverAddress,
            StringComparison.Ordinal);

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Theory]
    [InlineData("executablePath", "C:\\\\Tools\\\\player.exe")]
    [InlineData("pageUrl", "https://example.invalid")]
    [InlineData("serverId", "server-identifier")]
    [InlineData("unknown", "value")]
    public void TryParsePlayRequest_AdditionalField_IsRejected(
        string fieldName,
        string fieldValue)
    {
        var json = CreateValidJson().Replace(
            "\n}",
            $",\n  \"{fieldName}\": \"{fieldValue}\"\n}}",
            StringComparison.Ordinal);

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryParsePlayRequest_DuplicateProperty_IsRejected()
    {
        var json = CreateValidJson().Replace(
            "\"type\": \"playWithPotPlayer\"",
            "\"type\": \"playWithPotPlayer\",\n  \"type\": \"playWithPotPlayer\"",
            StringComparison.Ordinal);

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryParsePlayRequest_OversizedMessage_IsRejected()
    {
        var json = new string('x', 8193);

        var result = WebBridgeMessageParser.TryParsePlayRequest(json, out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void PlayRequestMessage_ToString_DoesNotExposeSessionValues()
    {
        var message = new PlayRequestMessage(
            ItemId,
            "https://media.example",
            UserId,
            AccessToken);

        var text = message.ToString();

        Assert.DoesNotContain(AccessToken, text, StringComparison.Ordinal);
        Assert.DoesNotContain(UserId, text, StringComparison.Ordinal);
    }

    private static string CreateValidJson()
    {
        return $$"""
            {
              "type": "playWithPotPlayer",
              "itemId": "{{ItemId}}",
              "serverAddress": "https://media.example/jellyfin",
              "userId": "{{UserId}}",
              "accessToken": "{{AccessToken}}"
            }
            """;
    }

    private static string GetFieldValue(string fieldName)
    {
        return fieldName switch
        {
            "itemId" => ItemId,
            "serverAddress" => "https://media.example/jellyfin",
            "userId" => UserId,
            "accessToken" => AccessToken,
            _ => throw new ArgumentOutOfRangeException(nameof(fieldName))
        };
    }
}
