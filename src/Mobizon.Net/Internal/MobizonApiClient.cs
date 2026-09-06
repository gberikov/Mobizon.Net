using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

using Mobizon.Net.Internal.Converters;

namespace Mobizon.Net.Internal
{
    internal class MobizonApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new StringToIntConverter(),
                new StringToLongConverter(),
                new StringToFloatConverter(),
                new StringToDecimalConverter(),
                new StringToBoolConverter(),
                new StringMinutesToTimeSpanConverter(),
                new SmsStatusConverter(),
                new CampaignCommonStatusConverter(),
                new MessageTypeConverter(),
                new StringToNumericEnumConverter<CampaignType>(),
                new StringToNumericEnumConverter<StopListLevel>(),
                new StringToNumericEnumConverter<Mobizon.Contracts.LinkStatus>(),
                new StringToNumericEnumConverter<Mobizon.Contracts.LinkModeratorStatus>(),
                new StringToNumericEnumConverter<BackgroundTaskStatus>(),
                new StringToNumericEnumConverter<CampaignStatus>(),
                new StringToNumericEnumConverter<MessageClass>(),
                new MobizonDateTimeConverter(),
                new TolerantStringEnumConverter<ContactType>(),
                new TolerantStringEnumConverter<Gender>(),
                // Contact-card fields the PHP API may emit as `[]`/`""` when unset — tolerate that.
                new EmptyTolerantObjectConverter<Mobizon.Contracts.ContactFieldInfo>(),
                new EmptyTolerantObjectConverter<Mobizon.Contracts.MobileFieldInfo>(),
                new EmptyTolerantObjectConverter<Mobizon.Contracts.AddressFieldInfo>(),
                new AddRecipientsResultConverter(),
                new LinkStatsResultConverter()
            }
        };

        private static readonly ProductInfoHeaderValue UserAgent =
            new ProductInfoHeaderValue("Mobizon.Net", GetSdkVersion());

        private readonly HttpClient _httpClient;
        private readonly MobizonClientOptions _options;
        private readonly string _baseUrl;

        public MobizonApiClient(HttpClient httpClient, MobizonClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
            _baseUrl = options.ApiUrl.TrimEnd('/') + "/service/";
        }

        /// <summary>
        /// Sends a form-encoded POST and deserializes the <c>data</c> payload as <typeparamref name="T"/>.
        /// The API key is always a body field (never part of the URL), so it does not leak into proxy /
        /// HttpClient request logs.
        /// <para>
        /// <c>acceptedCodes</c>: non-zero response codes this operation treats as success (e.g. 100 for operations
        /// that may queue a background task, 98/99 for bulk operations). Any other non-zero code throws
        /// <see cref="MobizonApiException"/>.
        /// <c>allowNullData</c>: when <see langword="true"/>, a missing or <c>null</c> <c>data</c> payload yields
        /// <c>default(T)</c> instead of a protocol error. Only for operations observed to answer that way.
        /// </para>
        /// </summary>
        public Task<MobizonResponse<T>> SendAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default,
            int[]? acceptedCodes = null,
            bool allowNullData = false)
        {
            var request = CreateRequest(module, apiMethod);
            request.Content = new FormUrlEncodedContent(BuildForm(parameters));
            return SendCoreAsync<T>(request, module, apiMethod, cancellationToken, acceptedCodes, allowNullData, readData: true);
        }

        /// <summary>
        /// Sends a form-encoded POST for a command whose <c>data</c> payload carries no information
        /// (<c>delete</c>/<c>update</c> return <c>true</c>); the payload is validated as an envelope but not deserialized.
        /// </summary>
        public Task SendCommandAsync(
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(module, apiMethod);
            request.Content = new FormUrlEncodedContent(BuildForm(parameters));
            return SendCoreAsync<bool>(request, module, apiMethod, cancellationToken, null, allowNullData: true, readData: false);
        }

        /// <summary>
        /// Sends a form-encoded POST that must return a positive identifier (create-style operations).
        /// A missing, zero or negative id is a protocol error rather than a "created" result.
        /// </summary>
        public async Task<long> SendForIdAsync(
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default)
        {
            var response = await SendAsync<long>(module, apiMethod, parameters, cancellationToken).ConfigureAwait(false);
            return RequireId(response, module, apiMethod);
        }

        /// <summary>
        /// Sends a multipart POST. <paramref name="file"/> is owned by the caller: the SDK reads it to the end
        /// but never closes it.
        /// </summary>
        public Task<MobizonResponse<T>> SendMultipartAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string> fields,
            Stream? file = null,
            string? fileName = null,
            CancellationToken cancellationToken = default,
            int[]? acceptedCodes = null,
            string fileFieldName = "data[photo]",
            bool allowNullData = false)
        {
            var request = CreateRequest(module, apiMethod);

            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(_options.ApiKey), "apiKey");

            foreach (var kv in fields)
                multipart.Add(new StringContent(kv.Value ?? string.Empty), kv.Key);

            if (file != null)
            {
                var fileContent = new StreamContent(new LeaveOpenStream(file));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                multipart.Add(fileContent, fileFieldName, fileName ?? "file");
            }

            request.Content = multipart;

            return SendCoreAsync<T>(request, module, apiMethod, cancellationToken, acceptedCodes, allowNullData, readData: true);
        }

        internal static long RequireId<T>(MobizonResponse<T> response, string module, string apiMethod)
        {
            if (response.Data is long id && id > 0)
                return id;

            throw new MobizonException(
                $"Mobizon API reported success for {module}/{apiMethod} but returned no valid identifier.",
                response.StatusCode);
        }

        /// <summary>Read-only API methods are safe to retry. Every Mobizon read is <c>get*</c> or <c>list</c>.</summary>
        internal static bool IsReadMethod(string apiMethod) =>
            apiMethod.StartsWith("get", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(apiMethod, "list", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, string> BuildForm(IDictionary<string, string>? parameters)
        {
            var form = new Dictionary<string, string>();
            if (parameters != null)
                foreach (var kv in parameters)
                    form[kv.Key] = kv.Value;
            form["apiKey"] = _options.ApiKey; // SDK's key always wins; never overridable by a caller parameter.
            return form;
        }

        private HttpRequestMessage CreateRequest(string module, string apiMethod)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(module, apiMethod));
            request.Headers.UserAgent.Add(UserAgent);
            if (IsReadMethod(apiMethod))
                RequestMarkers.MarkIdempotent(request);
            return request;
        }

        private string BuildUrl(string module, string apiMethod) =>
            $"{_baseUrl}{module}/{apiMethod}?output=json&api={Uri.EscapeDataString(_options.ApiVersion)}";

        private static string GetSdkVersion()
        {
            var v = typeof(MobizonApiClient).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            var plus = v.IndexOf('+');
            return plus > 0 ? v.Substring(0, plus) : v;
        }

        // Envelope first, payload second: the API code decides whether `data` is a result or an error
        // description, so `data` is only bound to T once the envelope says the call succeeded.
        // Diagnostics deliberately never quote the response body (it may carry message texts, phone
        // numbers or one-time codes); they name the operation, HTTP status and JSON path instead.
        private async Task<MobizonResponse<T>> SendCoreAsync<T>(
            HttpRequestMessage request,
            string module,
            string apiMethod,
            CancellationToken cancellationToken,
            int[]? acceptedCodes,
            bool allowNullData,
            bool readData)
        {
            var operation = $"{module}/{apiMethod}";

            using (request)
            {
                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // the caller asked for it
                }
                catch (OperationCanceledException ex)
                {
                    // HttpClient.Timeout surfaces as TaskCanceledException while the caller's token is untouched.
                    throw new MobizonException(
                        $"Request {operation} to the Mobizon API timed out after {_httpClient.Timeout}.", statusCode: null, ex);
                }
                catch (Exception ex)
                {
                    throw new MobizonException($"Failed to send request {operation} to the Mobizon API: {ex.Message}", statusCode: null, ex);
                }

                using (response)
                {
                    var status = response.StatusCode;
                    JsonDocument document;
                    try
                    {
                        var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        document = await JsonDocument.ParseAsync(body, default, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (JsonException ex)
                    {
                        // Deliberately no inner exception: the reader's message quotes the offending token,
                        // i.e. the (possibly sensitive) body. Line/byte position is kept for diagnosis.
                        throw new MobizonException(
                            $"{DescribeNonJsonBody(operation, response)} JSON error at line {ex.LineNumber}, byte {ex.BytePositionInLine}.",
                            status);
                    }
                    catch (Exception ex)
                    {
                        throw new MobizonException(
                            $"Failed to read the Mobizon API response for {operation} (HTTP {(int)status}).", status, ex);
                    }

                    using (document)
                    {
                        var root = document.RootElement;
                        if (root.ValueKind != JsonValueKind.Object || !TryReadCode(root, out var code))
                            throw new MobizonException(
                                $"Mobizon API response for {operation} (HTTP {(int)status}) is not a valid envelope: " +
                                "missing or non-numeric \"code\".", status);

                        var message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                            ? m.GetString() ?? string.Empty
                            : string.Empty;

                        root.TryGetProperty("data", out var data); // ValueKind.Undefined when absent

                        var accepted = code == 0 || (acceptedCodes != null && Array.IndexOf(acceptedCodes, code) >= 0);
                        if (!accepted)
                            throw new MobizonApiException(code, message, status, ParseFieldErrors(data));

                        if (!response.IsSuccessStatusCode)
                            throw new MobizonException(
                                $"Mobizon API returned HTTP {(int)status} ({status}) for {operation} together with a " +
                                $"success envelope (code {code}); the result cannot be trusted.", status);

                        var result = new MobizonResponse<T> { RawCode = code, Message = message, StatusCode = status };
                        if (!readData)
                            return result;

                        if (data.ValueKind == JsonValueKind.Undefined || data.ValueKind == JsonValueKind.Null)
                        {
                            if (!allowNullData)
                                throw new MobizonException(
                                    $"Mobizon API response for {operation} (HTTP {(int)status}) reported success but carries no data payload.",
                                    status);
                            result.Data = default!;
                            return result;
                        }

                        try
                        {
                            result.Data = data.Deserialize<T>(JsonOptions)!;
                        }
                        catch (JsonException ex)
                        {
                            throw new MobizonException(
                                $"Failed to deserialize the Mobizon API response for {operation} (HTTP {(int)status}) " +
                                $"into {typeof(T).Name}{DescribePath(ex)}.", status, ex);
                        }

                        return result;
                    }
                }
            }
        }

        private static bool TryReadCode(JsonElement root, out int code)
        {
            code = 0;
            if (!root.TryGetProperty("code", out var element))
                return false;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.TryGetInt32(out code);
                case JsonValueKind.String:
                    return int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Flattens an error <c>data</c> object (<c>{ "field": ["msg"], "nested": { "child": "msg" } }</c>) into
        /// field path → messages. Any other shape yields <see langword="null"/>; the API code and message are
        /// preserved regardless.
        /// </summary>
        internal static IReadOnlyDictionary<string, IReadOnlyList<string>>? ParseFieldErrors(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
                return null;

            var errors = new Dictionary<string, IReadOnlyList<string>>();
            Collect(data, string.Empty, errors);
            return errors.Count > 0 ? errors : null;

            static void Collect(JsonElement element, string prefix, Dictionary<string, IReadOnlyList<string>> into)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.Object:
                            Collect(property.Value, path, into);
                            break;
                        case JsonValueKind.Array:
                            var messages = new List<string>();
                            foreach (var item in property.Value.EnumerateArray())
                                if (item.ValueKind != JsonValueKind.Object && item.ValueKind != JsonValueKind.Array)
                                    messages.Add(item.ToString());
                            if (messages.Count > 0)
                                into[path] = messages;
                            break;
                        case JsonValueKind.Null:
                            break;
                        default:
                            into[path] = new[] { property.Value.ToString() };
                            break;
                    }
                }
            }
        }

        private static string DescribeNonJsonBody(string operation, HttpResponseMessage response)
        {
            var status = (int)response.StatusCode;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
            var length = response.Content.Headers.ContentLength;
            var size = length.HasValue ? $"{length.Value} bytes" : "unknown length";
            return response.IsSuccessStatusCode
                ? $"Mobizon API response for {operation} (HTTP {status}) is not valid JSON (Content-Type: {contentType}, {size})."
                : $"Mobizon API returned HTTP {status} ({response.StatusCode}) for {operation} with a non-JSON body (Content-Type: {contentType}, {size}).";
        }

        private static string DescribePath(JsonException ex) =>
            string.IsNullOrEmpty(ex.Path) ? string.Empty : $" at data path {ex.Path}";

        /// <summary>Delegates to the caller's stream but ignores <see cref="Stream.Dispose()"/>, so disposing the request content never closes it.</summary>
        private sealed class LeaveOpenStream : Stream
        {
            private readonly Stream _inner;
            public LeaveOpenStream(Stream inner) => _inner = inner;

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                _inner.CopyToAsync(destination, bufferSize, cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { /* caller owns the stream */ }
        }
    }
}
