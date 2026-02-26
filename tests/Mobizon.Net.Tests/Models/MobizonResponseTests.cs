using System.Text.Json;
using Mobizon.Contracts.Models.Common;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class MobizonResponseTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void Deserialize_SuccessResponse_MapsCorrectly()
        {
            var json = @"{""code"":0,""data"":{""messageId"":123},""message"":""""}";
            var response = JsonSerializer.Deserialize<MobizonResponse<TestData>>(json, JsonOptions);

            Assert.NotNull(response);
            Assert.Equal(0, response!.RawCode);
            Assert.Equal(MobizonResponseCode.Success, response.Code);
            Assert.Equal(123, response.Data.MessageId);
            Assert.Equal(string.Empty, response.Message);
        }

        [Fact]
        public void Deserialize_ErrorResponse_MapsCorrectly()
        {
            var json = @"{""code"":2,""data"":null,""message"":""Auth failed""}";
            var response = JsonSerializer.Deserialize<MobizonResponse<TestData>>(json);

            Assert.NotNull(response);
            Assert.Equal(2, response!.RawCode);
            Assert.Equal(MobizonResponseCode.NotFound, response.Code);
            Assert.Equal("Auth failed", response.Message);
        }

        [Fact]
        public void Deserialize_BackgroundTaskResponse_MapsCorrectly()
        {
            var json = @"{""code"":100,""data"":{""messageId"":456},""message"":""""}";
            var response = JsonSerializer.Deserialize<MobizonResponse<TestData>>(json);

            Assert.NotNull(response);
            Assert.Equal(100, response!.RawCode);
            Assert.Equal(MobizonResponseCode.BackgroundTask, response.Code);
        }

        [Fact]
        public void Code_UnknownValue_PreservesRawCode()
        {
            var json = @"{""code"":999,""data"":null,""message"":""Unknown error""}";
            var response = JsonSerializer.Deserialize<MobizonResponse<TestData>>(json);

            Assert.NotNull(response);
            Assert.Equal(999, response!.RawCode);
        }

        private class TestData
        {
            public int MessageId { get; set; }
        }
    }
}
