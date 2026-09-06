using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Net.Internal;
using Polly;
using Polly.Extensions.Http;

namespace Mobizon.Net.Extensions.Polly
{
    /// <summary>
    /// Extension methods for adding Polly resilience policies to the Mobizon <see cref="IHttpClientBuilder"/>.
    /// </summary>
    public static class MobizonHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds the default Mobizon resilience policies (exponential retry and circuit breaker) to the HTTP client.
        /// By default, retry covers read-only calls only (<c>get*</c> / <c>list</c>); see
        /// <see cref="MobizonResilienceOptions.RetryNonIdempotentRequests"/> to retry every call.
        /// <para>
        /// Both policies react to <em>transport-level</em> faults only: <see cref="HttpRequestException"/>,
        /// HTTP 5xx and HTTP 408. An API error inside an HTTP 200 envelope (for example rate-limit code 30) is
        /// not retried, and HTTP 429 is not treated as transient.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> returned by <c>AddMobizon</c>.</param>
        /// <returns>The same <see cref="IHttpClientBuilder"/> for further chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMobizon(options =>
        /// {
        ///     options.ApiKey = "your-api-key";
        ///     options.ApiUrl = "https://api.mobizon.kz";
        /// })
        /// .AddMobizonResilience();
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMobizonResilience(this IHttpClientBuilder builder)
            => builder.AddMobizonResilience(_ => { });

        /// <summary>
        /// Adds customised Mobizon resilience policies (exponential retry and circuit breaker) to the HTTP client.
        /// By default, retry covers read-only calls only (<c>get*</c> / <c>list</c>); see
        /// <see cref="MobizonResilienceOptions.RetryNonIdempotentRequests"/> to retry every call.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> returned by <c>AddMobizon</c>.</param>
        /// <param name="configure">A delegate that configures the <see cref="MobizonResilienceOptions"/>.</param>
        /// <returns>The same <see cref="IHttpClientBuilder"/> for further chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMobizon(options =>
        /// {
        ///     options.ApiKey = "your-api-key";
        ///     options.ApiUrl = "https://api.mobizon.kz";
        /// })
        /// .AddMobizonResilience(resilience =>
        /// {
        ///     resilience.RetryCount = 5;
        ///     resilience.RetryBaseDelay = TimeSpan.FromMilliseconds(500);
        ///     resilience.CircuitBreakerFailureThreshold = 10;
        ///     resilience.CircuitBreakerDuration = TimeSpan.FromMinutes(1);
        /// });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMobizonResilience(
            this IHttpClientBuilder builder,
            Action<MobizonResilienceOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var options = new MobizonResilienceOptions();
            configure(options);
            options.Validate();

            var retry = GetRetryPolicy(options.RetryCount, options.RetryBaseDelay);
            var noRetry = Policy.NoOpAsync<HttpResponseMessage>();
            var retryAll = options.RetryNonIdempotentRequests;

            // Multipart uploads are never retried: the file stream has been read to EOF by the first attempt,
            // so a resend would silently carry an empty or truncated file.
            return builder
                .AddPolicyHandler(request =>
                    !(request.Content is MultipartFormDataContent) && (retryAll || RequestMarkers.IsIdempotent(request))
                        ? retry
                        : noRetry)
                .AddPolicyHandler(GetCircuitBreakerPolicy(
                    options.CircuitBreakerFailureThreshold,
                    options.CircuitBreakerDuration));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
            int retryCount = 3, TimeSpan? baseDelay = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount, attempt => MobizonResilienceOptions.DelayForAttempt(delay, attempt));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
            int failureThreshold = 5, TimeSpan? breakDuration = null)
        {
            var duration = breakDuration ?? TimeSpan.FromSeconds(30);
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(failureThreshold, duration);
        }
    }
}
