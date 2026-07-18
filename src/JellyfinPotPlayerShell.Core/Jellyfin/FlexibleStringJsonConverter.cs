using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JellyfinPotPlayerShell.Core.Jellyfin;

public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => GetNumberText(ref reader),
            JsonTokenType.Null => null,
            _ => throw new JsonException("Expected a string or numeric enum value.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        string? value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    private static string GetNumberText(ref Utf8JsonReader reader)
    {
        return reader.HasValueSequence
            ? System.Text.Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
            : System.Text.Encoding.UTF8.GetString(reader.ValueSpan);
    }
}
