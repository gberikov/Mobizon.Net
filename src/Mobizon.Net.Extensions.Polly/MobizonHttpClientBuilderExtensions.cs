using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
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
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> returned by <c>AddMobizon</c>.</param>
        /// <returns>The same <see cref="IHttpClientBuilder"/> for further chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMobizon(options =>
        /// {
        ///     options.ApiKey = "your-api-key";
        ///     options.ApiUrl = "https://api.mobizon.com/service/";
        /// })
        /// .AddMobizonResilience();
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMobizonResilience(this IHttpClientBuilder builder)
        {
            return builder
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
        }

        /// <summary>
        /// Adds customised Mobizon resilience policies (exponential retry and circuit breaker) to the HTTP client.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> returned by <c>AddMobizon</c>.</param>
        /// <param name="configure">A delegate that configures the <see cref="MobizonResilienceOptions"/>.</param>
        /// <returns>The same <see cref="IHttpClientBuilder"/> for further chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMobizon(options =>
        /// {
        ///     options.ApiKey = "your-api-key";
        ///     options.ApiUrl = "https://api.mobizon.com/service/";
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
            var options = new MobizonResilienceOptions();
            configure(options);

            return builder
                .AddPolicyHandler(GetRetryPolicy(options.RetryCount, options.RetryBaseDelay))
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
                .WaitAndRetryAsync(retryCount, attempt =>
                    TimeSpan.FromTicks(delay.Ticks * (long)Math.Pow(2, attempt - 1)));
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
