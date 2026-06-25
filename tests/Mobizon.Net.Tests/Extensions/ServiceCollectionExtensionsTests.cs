using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts.Services;
using Mobizon.Net.Extensions.DependencyInjection;
using Xunit;

namespace Mobizon.Net.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddMobizon_WithAction_RegistersIMobizonClient()
        {
            var services = new ServiceCollection();

            services.AddMobizon(options =>
            {
                options.ApiKey = "test-key";
                options.ApiUrl = "https://api.mobizon.test";
            });

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMobizonClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public void AddMobizon_WithConfiguration_BindsCorrectly()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ApiKey", "config-key" },
                    { "ApiUrl", "https://api.mobizon.config" }
                })
                .Build();

            services.AddMobizon(configuration);

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMobizonClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public void AddMobizon_ReturnsIHttpClientBuilder()
        {
            var services = new ServiceCollection();

            var builder = services.AddMobizon(options =>
            {
                options.ApiKey = "test-key";
                options.ApiUrl = "https://api.mobizon.test";
            });

            Assert.NotNull(builder);
            Assert.IsAssignableFrom<IHttpClientBuilder>(builder);
        }

        [Fact]
        public void AddMobizon_AppliesConfiguredTimeoutToNamedHttpClient()
        {
            var services = new ServiceCollection();

            services.AddMobizon(options =>
            {
                options.ApiKey = "test-key";
                options.ApiUrl = "https://api.mobizon.test";
                options.Timeout = TimeSpan.FromSeconds(5);
            });

            var provider = services.BuildServiceProvider();
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Mobizon");

            Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
        }

        [Fact]
        public void AddMobizon_DefaultTimeout_AppliedToNamedHttpClient()
        {
            var services = new ServiceCollection();

            services.AddMobizon(options =>
            {
                options.ApiKey = "test-key";
                options.ApiUrl = "https://api.mobizon.test";
            });

            var provider = services.BuildServiceProvider();
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Mobizon");

            // Default options.Timeout is 30s; it must be applied (not the HttpClient 100s default).
            Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
        }
    }
}
