using System.Net.Http;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Per-request flags carried on the <see cref="HttpRequestMessage"/> so outer layers
    /// (e.g. the Polly package) can decide whether a request is safe to retry.
    /// </summary>
    internal static class RequestMarkers
    {
        private const string IdempotentKey = "Mobizon.Idempotent";

#if NET5_0_OR_GREATER
        private static readonly HttpRequestOptionsKey<bool> IdempotentOption = new HttpRequestOptionsKey<bool>(IdempotentKey);

        public static void MarkIdempotent(HttpRequestMessage request) => request.Options.Set(IdempotentOption, true);

        public static bool IsIdempotent(HttpRequestMessage request) =>
            request.Options.TryGetValue(IdempotentOption, out var value) && value;
#else
        public static void MarkIdempotent(HttpRequestMessage request) => request.Properties[IdempotentKey] = true;

        public static bool IsIdempotent(HttpRequestMessage request) =>
            request.Properties.TryGetValue(IdempotentKey, out var value) && value is bool b && b;
#endif
    }
}
