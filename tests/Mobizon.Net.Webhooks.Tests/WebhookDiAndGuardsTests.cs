using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts.Webhooks;
using Mobizon.Contracts;
using Mobizon.Net.Webhooks.AspNetCore;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class WebhookDiAndGuardsTests
    {
        [Fact]
        public void AddMobizonWebhooks_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddMobizonWebhooks(o => o.SecretKeyResolver = (_, __) => "s");
            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IWebhookParser>());
            Assert.NotNull(provider.GetService<IWebhookSignatureVerifier>());
            Assert.NotNull(provider.GetService<IWebhookProcessor>());
            Assert.NotNull(provider.GetService<MobizonWebhookOptions>());
        }

        [Fact]
        public void AddMobizonWebhooks_NullServices_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ((IServiceCollection)null!).AddMobizonWebhooks(o => { }));
        }

        [Fact]
        public void AddMobizonWebhooks_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection().AddMobizonWebhooks(null!));
        }

        [Fact]
        public void AddMobizonWebhooks_NoResolver_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ServiceCollection().AddMobizonWebhooks(o => { }));
        }

        [Fact]
        public void MapMobizonWebhook_NullEndpoints_Throws()
        {
            Func<MobizonWebhookEvent, CancellationToken, Task> handler = (_, __) => Task.CompletedTask;
            Assert.Throws<ArgumentNullException>(() =>
                ((IEndpointRouteBuilder)null!).MapMobizonWebhook("/x", handler));
        }
    }
}
