using System;
using System.Text.Json;
using Mobizon.Net.Internal.Converters;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class MobizonDateTimeConverterTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters = { new MobizonDateTimeConverter() }
        };

        private class Holder { public DateTime? Value { get; set; } }

        [Theory]
        [InlineData("\"2026-03-10 12:00:00\"", 2026, 3, 10, 12, 0, 0)]
        [InlineData("\"2025-12-31\"", 2025, 12, 31, 0, 0, 0)]
        public void Read_AcceptsDateTimeAndDateOnly(string json, int y, int m, int d, int hh, int mm, int ss)
        {
            var h = JsonSerializer.Deserialize<Holder>("{\"Value\":" + json + "}", Options)!;
            Assert.Equal(new DateTime(y, m, d, hh, mm, ss), h.Value);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        public void Read_NullOrEmpty_YieldsNull(string json)
        {
            var h = JsonSerializer.Deserialize<Holder>("{\"Value\":" + json + "}", Options)!;
            Assert.Null(h.Value);
        }

        [Fact]
        public void Read_Garbage_Throws()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<Holder>("{\"Value\":\"tomorrow\"}", Options));
        }
    }
}
