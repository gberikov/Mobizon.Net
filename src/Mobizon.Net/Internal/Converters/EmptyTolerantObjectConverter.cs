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
    /// Deserializes a complex contact field object (e.g. <c>email</c>, <c>mobile</c>, <c>address</c>) that the
    /// Mobizon PHP API may serialise as an empty array <c>[]</c> or an empty string <c>""</c> when the field is
    /// unset. Only those confirmed "no value" shapes become <see langword="null"/>.
    /// <para>
    /// A non-empty scalar is data, not absence: for the field types that carry a single <c>value</c> it is
    /// mapped onto that value, matching the write path, which sends these fields as bare strings. Any other
    /// incompatible shape — a populated array, a number, a boolean, or a scalar for a field
    /// with no single-value form — raises a protocol error instead of quietly discarding the value.
    /// </para>
    /// </summary>
    internal sealed class EmptyTolerantObjectConverter<T> : JsonConverter<T>, IEmptyTolerantConverter
        where T : class
    {
        private readonly Func<string, T>? _fromScalar;
        private JsonSerializerOptions? _inner;

        /// <param name="fromScalar">
        /// Builds an instance from a non-empty scalar string, or <see langword="null"/> when the type has no
        /// meaningful single-value form (an address, for instance).
        /// </param>
        public EmptyTolerantObjectConverter(Func<string, T>? fromScalar = null) => _fromScalar = fromScalar;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return JsonSerializer.Deserialize<T>(ref reader, Inner(options));

                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.StartArray:
                    // PHP serialises an empty associative field as an empty JSON array. A populated array is
                    // an unknown shape and must not be dropped.
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return null;
                    throw Unexpected("a non-empty array");

                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;
                    if (_fromScalar != null)
                        return _fromScalar(text!);
                    throw Unexpected("a non-empty string");

                default:
                    throw Unexpected(reader.TokenType.ToString());
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value, Inner(options));
        }

        private static JsonException Unexpected(string shape) =>
            new JsonException($"Contact field of type {typeof(T).Name} arrived as {shape}, which the SDK cannot map.");

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
