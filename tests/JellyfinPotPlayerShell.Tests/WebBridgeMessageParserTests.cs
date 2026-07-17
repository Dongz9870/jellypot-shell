using JellyfinPotPlayerShell.Core.Messaging;

namespace JellyfinPotPlayerShell.Tests;

public sealed class WebBridgeMessageParserTests
{
    private const string ItemId = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void TryParsePlayRequest_ValidMinimalMessage_ReturnsItemId()
    {
        var json = $$"""{"type":"playWithPotPlayer","itemId":"{{ItemId}}"}""";

        var result = WebBridgeMessageParser.TryParsePlayRequest(
            json,
            out var message);

        Assert.True(result);
        Assert.NotNull(message);
        Assert.Equal(ItemId, message.ItemId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"type\":\"other\",\"itemId\":\"0123456789abcdef0123456789abcdef\"}")]
    [InlineData("{\"type\":\"playWithPotPlayer\",\"itemId\":\"too-short\"}")]
    [InlineData("{\"type\":\"playWithPotPlayer\",\"itemId\":\"0123456789abcdef0123456789abcdeg\"}")]
    [InlineData("{\"type\":\"playWithPotPlayer\"}")]
    public void TryParsePlayRequest_InvalidMessage_IsRejected(string? json)
    {
        var result = WebBridgeMessageParser.TryParsePlayRequest(
            json,
            out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Theory]
    [InlineData("accessToken", "secret")]
    [InlineData("executablePath", "C:\\\\Tools\\\\player.exe")]
    [InlineData("pageUrl", "https://example.invalid")]
    [InlineData("serverAddress", "https://example.invalid")]
    public void TryParsePlayRequest_AdditionalField_IsRejected(
        string fieldName,
        string fieldValue)
    {
        var json = $$"""
            {
              "type": "playWithPotPlayer",
              "itemId": "{{ItemId}}",
              "{{fieldName}}": "{{fieldValue}}"
            }
            """;

        var result = WebBridgeMessageParser.TryParsePlayRequest(
            json,
            out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryParsePlayRequest_DuplicateProperty_IsRejected()
    {
        var json = $$"""
            {
              "type": "playWithPotPlayer",
              "type": "playWithPotPlayer",
              "itemId": "{{ItemId}}"
            }
            """;

        var result = WebBridgeMessageParser.TryParsePlayRequest(
            json,
            out var message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryParsePlayRequest_OversizedMessage_IsRejected()
    {
        var json = new string('x', 513);

        var result = WebBridgeMessageParser.TryParsePlayRequest(
            json,
            out var message);

        Assert.False(result);
        Assert.Null(message);
    }
}
