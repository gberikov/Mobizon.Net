using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Marker for <see cref="EmptyTolerantObjectConverter{T}"/> instances so they can be stripped
    /// from a cloned <see cref="JsonSerializerOptions"/> to avoid converter recursion.
    /// </summary>
    internal interface IEmptyTolerantConverter
    {
    }

    /// <summary>
    /// Deserializes a complex contact field object (e.g. <c>email</c>, <c>mobile</c>, <c>address</c>)
    /// that the Mobizon PHP API may serialise as an empty array <c>[]</c> or empty string <c>""</c>
    /// when the field is unset. Such values are mapped to <see langword="null"/> instead of failing
    /// deserialization of the entire contact card. A real object is deserialized normally.
    /// </summary>
    internal sealed class EmptyTolerantObjectConverter<T> : JsonConverter<T>, IEmptyTolerantConverter
        where T : class
    {
        private JsonSerializerOptions? _inner;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return JsonSerializer.Deserialize<T>(ref reader, Inner(options));

                case JsonTokenType.StartArray:
                    // PHP serialises an empty associative field as an empty JSON array.
                    reader.Skip();
                    return null;

                // Null / empty string / any other scalar → treat as "no value".
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value, Inner(options));
        }

        // A copy of the serializer options with the empty-tolerant converters removed, so the
        // default object (de)serializer is used for T instead of recursing back into this converter.
        private JsonSerializerOptions Inner(JsonSerializerOptions options)
        {
            var inner = _inner;
            if (inner != null)
                return inner;

            inner = new JsonSerializerOptions(options);
            for (var i = inner.Converters.Count - 1; i >= 0; i--)
            {
                if (inner.Converters[i] is IEmptyTolerantConverter)
                    inner.Converters.RemoveAt(i);
            }

            _inner = inner;
            return inner;
        }
    }
}
