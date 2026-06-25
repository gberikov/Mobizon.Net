using System.Text.Json;
using Mobizon.Net.Internal.Converters;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class StringToLongConverterTests
    {
        private static JsonSerializerOptions Opts()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new StringToLongConverter());
            return o;
        }

        [Fact]
        public void Reads_String_Above_Int32_Max()
            => Assert.Equal(70000000001L, JsonSerializer.Deserialize<long>("\"70000000001\"", Opts()));

        [Fact]
        public void Reads_Number_Token()
            => Assert.Equal(42L, JsonSerializer.Deserialize<long>("42", Opts()));
    }
}
