using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Services;
using Mobizon.Net;

namespace Mobizon.Net.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering the Mobizon client in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IMobizonClient"/> as a transient service backed by <see cref="IHttpClientFactory"/>, and configures it using a delegate.
        /// </summary>
        /// <param name="services">The service collection to add the Mobizon client to.</param>
        /// <param name="configure">A delegate that configures the <see cref="MobizonClientOptions"/>.</param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the underlying named
        /// <c>"Mobizon"</c> <see cref="HttpClient"/>, for example by adding resilience policies.
        /// </returns>
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
        public static IHttpClientBuilder AddMobizon(
            this IServiceCollection services,
            Action<MobizonClientOptions> configure)
        {
            services.Configure(configure);
            return AddMobizonCore(services);
        }

        /// <summary>
        /// Registers <see cref="IMobizonClient"/> as a transient service backed by <see cref="IHttpClientFactory"/>, and binds its configuration from an
        /// <see cref="IConfiguration"/> section.
        /// </summary>
        /// <param name="services">The service collection to add the Mobizon client to.</param>
        /// <param name="configuration">
        /// The configuration section whose keys map to <see cref="MobizonClientOptions"/> properties.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the underlying named
        /// <c>"Mobizon"</c> <see cref="HttpClient"/>, for example by adding resilience policies.
        /// </returns>
        /// <example>
        /// <code>
        /// // appsettings.json:
        /// // { "Mobizon": { "ApiKey": "...", "ApiUrl": "https://api.mobizon.kz" } }
        ///
        /// services.AddMobizon(configuration.GetSection("Mobizon"));
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMobizon(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MobizonClientOptions>(configuration);
            return AddMobizonCore(services);
        }

        private static IHttpClientBuilder AddMobizonCore(IServiceCollection services)
        {
            services.AddTransient<IMobizonClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MobizonClientOptions>>().Value;
                options.Validate();
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("Mobizon");
                return new MobizonClient(httpClient, options);
            });

            // Apply the configured timeout to the named HttpClient. The MobizonClient constructor only
            // sets Timeout when it owns the HttpClient, so for the DI path (externally-owned client) the
            // timeout must be applied here — otherwise a configured value is silently ignored and the
            // HttpClient default (100s) is used instead.
            return services.AddHttpClient("Mobizon")
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<MobizonClientOptions>>().Value;
                    if (options.Timeout > TimeSpan.Zero)
                        client.Timeout = options.Timeout;
                });
        }
    }
}
