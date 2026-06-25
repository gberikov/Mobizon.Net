using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Net.Webhooks.AspNetCore;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class AspNetCoreWebhookEndpointTests
    {
        private static (HttpContext context, MemoryStream responseBody) BuildContext(string body, Func<IServiceProvider, MobizonWebhookEvent, string> resolver)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMobizonWebhooks(o => o.SecretKeyResolver = resolver);
            var provider = services.BuildServiceProvider();

            var context = new DefaultHttpContext { RequestServices = provider };
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            return (context, responseBody);
        }

        private static async Task<int> InvokeAsync(HttpContext context, Func<MobizonWebhookEvent, CancellationToken, Task> handler)
        {
            var result = await WebhookEndpointRouteBuilderExtensions.HandleRequestAsync(context, handler);
            await result.ExecuteAsync(context);
            return context.Response.StatusCode;
        }

        [Fact]
        public async Task ValidSignedRequest_Returns200_AndInvokesHandler()
        {
            var (context, _) = BuildContext(Payloads.Load(Payloads.SmsDeliveryReport), (_, __) => Payloads.Secret);
            MobizonWebhookEvent? received = null;

            var status = await InvokeAsync(context, (evt, ct) => { received = evt; return Task.CompletedTask; });

            Assert.Equal(200, status);
            Assert.NotNull(received);
            Assert.IsType<SmsDeliveryReportEvent>(received);
        }

        [Fact]
        public async Task TamperedSignature_Returns403_AndDoesNotInvokeHandler()
        {
            var (context, _) = BuildContext(Payloads.Load(Payloads.SmsDeliveryReport), (_, __) => "wrong-secret");
            var invoked = false;

            var status = await InvokeAsync(context, (evt, ct) => { invoked = true; return Task.CompletedTask; });

            Assert.Equal(403, status);
            Assert.False(invoked);
        }

        [Fact]
        public async Task MalformedBody_Returns400_AndDoesNotInvokeHandler()
        {
            var (context, _) = BuildContext("{ broken", (_, __) => Payloads.Secret);
            var invoked = false;

            var status = await InvokeAsync(context, (evt, ct) => { invoked = true; return Task.CompletedTask; });

            Assert.Equal(400, status);
            Assert.False(invoked);
        }

        [Fact]
        public async Task SecretResolver_ReceivesEvent_ForPerWebhookSecret()
        {
            long seenWebhookId = -1;
            var (context, _) = BuildContext(Payloads.Load(Payloads.SmsDeliveryReport), (_, evt) =>
            {
                seenWebhookId = evt.WebhookId;
                return evt.WebhookId == 1 ? Payloads.Secret : "wrong";
            });

            var status = await InvokeAsync(context, (evt, ct) => Task.CompletedTask);

            Assert.Equal(200, status);
            Assert.Equal(1, seenWebhookId);
        }

        [Fact]
        public async Task OversizedBody_Returns413()
        {
            var (context, _) = BuildContext(Payloads.Load(Payloads.SmsDeliveryReport), (_, __) => Payloads.Secret);
            // Set ContentLength to 1 byte over the default cap (262144)
            context.Request.ContentLength = 262145;

            var status = await InvokeAsync(context, (evt, ct) => Task.CompletedTask);

            Assert.Equal(413, status);
        }

        [Fact]
        public async Task OversizedBody_WithoutContentLength_Returns413()
        {
            // Body exceeds the 262144-byte cap, but Content-Length is not set (chunked transfer).
            var (context, _) = BuildContext(new string('x', 262145), (_, __) => Payloads.Secret);
            Assert.Null(context.Request.ContentLength);

            var status = await InvokeAsync(context, (evt, ct) => Task.CompletedTask);

            Assert.Equal(413, status);
        }

        [Fact]
        public async Task SignatureMismatch_Returns403_WithJsonBody()
        {
            var (context, responseBody) = BuildContext(Payloads.Load(Payloads.SmsDeliveryReport), (_, __) => "wrong-secret");

            var status = await InvokeAsync(context, (evt, ct) => Task.CompletedTask);

            Assert.Equal(403, status);
            var json = Encoding.UTF8.GetString(responseBody.ToArray());
            Assert.Contains("signature_mismatch", json);
            Assert.Equal("application/json", context.Response.ContentType?.Split(';')[0]);
        }
    }
}
