using System;

namespace Mobizon.Net.Extensions.Polly
{
    /// <summary>
    /// Configuration options for the Mobizon Polly resilience policies (retry and circuit breaker).
    /// </summary>
    public class MobizonResilienceOptions
    {
        /// <summary>
        /// Gets or sets the number of times a failed HTTP request will be retried before giving up.
        /// Defaults to <c>3</c>.
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retry attempts. Each attempt uses exponential back-off
        /// calculated as <c>RetryBaseDelay * 2^(attempt - 1)</c>. Defaults to 1 second.
        /// </summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the number of consecutive failures required to open (trip) the circuit breaker.
        /// Defaults to <c>5</c>.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration for which the circuit breaker stays open before attempting recovery.
        /// Defaults to 30 seconds.
        /// </summary>
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);
    }
}
