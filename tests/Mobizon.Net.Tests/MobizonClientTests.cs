using System;
using System.Net.Http;
using Mobizon.Contracts;
using Mobizon.Net;
using Xunit;

namespace Mobizon.Net.Tests
{
    public class MobizonClientTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        [Fact]
        public void Constructor_WithOptions_CreatesAllServices()
        {
            using var client = new MobizonClient(new HttpClient(), _options);

            Assert.NotNull(client.Messages);
            Assert.NotNull(client.Campaigns);
            Assert.NotNull(client.Links);
            Assert.NotNull(client.User);
            Assert.NotNull(client.TaskQueue);
            Assert.NotNull(client.ContactCards);
            Assert.IsAssignableFrom<IContactCardSet>(client.ContactCards);
        }

        [Fact]
        public void Constructor_ImplementsIMobizonClient()
        {
            using var client = new MobizonClient(new HttpClient(), _options);
            Assert.IsAssignableFrom<IMobizonClient>(client);
        }

        [Fact]
        public void Constructor_InvalidOptions_Throws()
        {
            var options = new MobizonClientOptions();
            Assert.Throws<ArgumentException>(() =>
                new MobizonClient(new HttpClient(), options));
        }

        [Fact]
        public void Dispose_WithOwnedHttpClient_DisposesClient()
        {
            var client = new MobizonClient(_options);
            client.Dispose();
            // No exception means success — HttpClient is disposed
        }

        [Fact]
        public void Dispose_WithExternalHttpClient_DoesNotDispose()
        {
            var httpClient = new HttpClient();
            var client = new MobizonClient(httpClient, _options);
            client.Dispose();

            // External HttpClient should still be usable (no ObjectDisposedException)
            _ = httpClient.Timeout;
        }

        [Fact]
        public void Ctor_WithInjectedHttpClient_DoesNotMutateItsTimeout()
        {
            using var http = new HttpClient();
            var original = http.Timeout; // default 100s
            using var client = new MobizonClient(http, new MobizonClientOptions
            {
                ApiKey = "k",
                ApiUrl = "https://api.mobizon.kz",
                Timeout = TimeSpan.FromSeconds(5)
            });
            Assert.Equal(original, http.Timeout); // injected client untouched
        }
    }
}
