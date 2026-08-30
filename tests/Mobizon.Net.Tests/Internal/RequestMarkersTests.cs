using System.Net.Http;
using Mobizon.Net.Internal;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class RequestMarkersTests
    {
        [Fact]
        public void Unmarked_IsNotIdempotent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/");
            Assert.False(RequestMarkers.IsIdempotent(request));
        }

        [Fact]
        public void Marked_IsIdempotent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/");
            RequestMarkers.MarkIdempotent(request);
            Assert.True(RequestMarkers.IsIdempotent(request));
        }
    }
}
