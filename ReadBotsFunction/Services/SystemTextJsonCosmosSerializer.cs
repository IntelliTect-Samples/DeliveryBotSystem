using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace ReadBotsFunction.Services;

public sealed class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public SystemTextJsonCosmosSerializer(JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = serializerOptions;
    }

    public override T FromStream<T>(Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
        {
            return (T)(object)stream;
        }

        using (stream)
        {
            var result = JsonSerializer.Deserialize<T>(stream, _serializerOptions);
            if (result is null)
            {
                throw new JsonException($"Could not deserialize Cosmos response to {typeof(T).Name}.");
            }

            return result;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, _serializerOptions);
        stream.Position = 0;
        return stream;
    }
}
