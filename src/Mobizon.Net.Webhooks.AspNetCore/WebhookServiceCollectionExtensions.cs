using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mobizon.Contracts.Services;

namespace Mobizon.Net.Webhooks.AspNetCore
{
    /// <summary>
    /// Dependency-injection helpers for Mobizon webhooks.
    /// </summary>
    public static class WebhookServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Mobizon webhook services (<see cref="IWebhookParser"/>,
        /// <see cref="IWebhookSignatureVerifier"/>, <see cref="IWebhookProcessor"/>) and options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configures <see cref="MobizonWebhookOptions"/> (must set the secret resolver).</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no secret resolver is configured.</exception>
        public static IServiceCollection AddMobizonWebhooks(
            this IServiceCollection services,
            Action<MobizonWebhookOptions> configure)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));

            var options = new MobizonWebhookOptions();
            configure(options);

            if (options.SecretKeyResolver is null)
                throw new InvalidOperationException(
                    $"{nameof(MobizonWebhookOptions)}.{nameof(MobizonWebhookOptions.SecretKeyResolver)} must be configured.");

            services.AddSingleton(options);
            services.TryAddSingleton<IWebhookParser, WebhookParser>();
            services.TryAddSingleton<IWebhookSignatureVerifier, WebhookSignatureVerifier>();
            services.TryAddSingleton<IWebhookProcessor, WebhookProcessor>();

            return services;
        }
    }
}
