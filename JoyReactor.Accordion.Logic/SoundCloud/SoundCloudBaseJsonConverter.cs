using JoyReactor.Accordion.Logic.SoundCloud.Responses;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud;

public class SoundCloudBaseJsonConverter : JsonConverter<SoundCloudBaseResponse>
{
    public override SoundCloudBaseResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty("kind", out var kindElement))
            throw new JsonException("Missing 'kind' property.");

        var kind = kindElement.GetString();
        return kind switch
        {
            "track" => document.Deserialize<SoundCloudTrackResponse>(options) ?? throw new NullReferenceException(),
            "playlist" => document.Deserialize<SoundCloudTrackResponse>(options) ?? throw new NullReferenceException(),
            _ => throw new JsonException($"Unsupported kind: {kind}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, SoundCloudBaseResponse value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}