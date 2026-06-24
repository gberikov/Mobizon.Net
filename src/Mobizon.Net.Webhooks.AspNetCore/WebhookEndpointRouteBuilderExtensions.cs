using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Contracts.Services;

namespace Mobizon.Net.Webhooks.AspNetCore
{
    /// <summary>
    /// Endpoint helpers for receiving Mobizon webhooks.
    /// </summary>
    public static class WebhookEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps a POST endpoint that reads the body, verifies the signature, parses the typed event, and
        /// invokes <paramref name="handler"/> only when the request is authentic. Responds with
        /// 200 (Ok), 403 (signature mismatch), or 400 (unparseable) automatically. Verification and
        /// parsing complete before the status is determined; the handler should defer heavy work so the
        /// endpoint can acknowledge within Mobizon's 5-second window.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="pattern">The route pattern (e.g. <c>/webhooks/mobizon</c>).</param>
        /// <param name="handler">Invoked with the verified, parsed event on success.</param>
        /// <returns>A builder for further endpoint configuration.</returns>
        public static IEndpointConventionBuilder MapMobizonWebhook(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Func<MobizonWebhookEvent, CancellationToken, Task> handler)
        {
            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));
            if (pattern is null)
                throw new ArgumentNullException(nameof(pattern));
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            Func<HttpContext, Task<IResult>> requestHandler = context => HandleRequestAsync(context, handler);
            return endpoints.MapPost(pattern, requestHandler);
        }

        /// <summary>
        /// Core request handling: read body, verify + parse, map to an <see cref="IResult"/>, and invoke
        /// the handler only on success. Extracted for unit testing without a full server.
        /// </summary>
        internal static async Task<IResult> HandleRequestAsync(
            HttpContext context,
            Func<MobizonWebhookEvent, CancellationToken, Task> handler)
        {
            var processor = context.RequestServices.GetRequiredService<IWebhookProcessor>();
            var options = context.RequestServices.GetRequiredService<MobizonWebhookOptions>();
            var services = context.RequestServices;

            string body;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var result = processor.Process(body, evt => options.SecretKeyResolver!(services, evt));

            switch (result.Status)
            {
                case WebhookProcessStatus.Ok:
                    await handler(result.Event!, context.RequestAborted).ConfigureAwait(false);
                    return Results.Ok();
                case WebhookProcessStatus.SignatureMismatch:
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                default:
                    return Results.BadRequest();
            }
        }
    }
}
