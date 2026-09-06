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

        /// <summary>
        /// When <see langword="false"/> (default) the retry policy applies only to read-only API calls
        /// (<c>get*</c> / <c>list</c>). Write calls such as <c>message/sendSmsMessage</c> or <c>campaign/send</c>
        /// are not retried, because a retry after a lost response can duplicate an SMS.
        /// Set to <see langword="true"/> to retry every call.
        /// Warning: multipart uploads (contact-card photos, campaign recipient files) are not retry-safe even
        /// then — the retry re-sends the same <see cref="System.Net.Http.HttpRequestMessage"/>, and by the second
        /// attempt the caller's <see cref="System.IO.Stream"/> has already been read to EOF, so the resend carries
        /// no content.
        /// </summary>
        public bool RetryNonIdempotentRequests { get; set; }

        /// <summary>
        /// Validates the options up front so a bad value fails at registration time rather than during an outage:
        /// non-negative <see cref="RetryCount"/> and <see cref="RetryBaseDelay"/>, a positive duration, a threshold of at least 1, and a
        /// back-off schedule whose longest delay still fits in a <see cref="TimeSpan"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any option is out of range.</exception>
        public void Validate()
        {
            if (RetryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(RetryCount), "RetryCount must not be negative.");
            if (RetryBaseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RetryBaseDelay), "RetryBaseDelay must not be negative.");
            if (CircuitBreakerFailureThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(CircuitBreakerFailureThreshold), "CircuitBreakerFailureThreshold must be at least 1.");
            if (CircuitBreakerDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(CircuitBreakerDuration), "CircuitBreakerDuration must be positive.");
            if (RetryCount > 0 && DelayForAttempt(RetryBaseDelay, RetryCount) == TimeSpan.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(RetryCount), "RetryBaseDelay * 2^(RetryCount - 1) overflows TimeSpan.");
        }

        /// <summary>
        /// Exponential back-off <c>baseDelay * 2^(attempt-1)</c>, computed in floating point so it cannot silently
        /// wrap around; a schedule that does not fit in a <see cref="TimeSpan"/> yields <see cref="TimeSpan.MaxValue"/>
        /// (rejected by <see cref="Validate"/>).
        /// </summary>
        internal static TimeSpan DelayForAttempt(TimeSpan baseDelay, int attempt)
        {
            var ms = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
            return ms < TimeSpan.MaxValue.TotalMilliseconds ? TimeSpan.FromMilliseconds(ms) : TimeSpan.MaxValue;
        }
    }
}
