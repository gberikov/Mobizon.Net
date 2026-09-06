using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Parses the period-major <c>link/getStats</c> <c>data</c> grid into one
    /// <see cref="LinkStatSeries"/> per requested link.
    /// <para>
    /// The API shape is:
    /// <code>
    /// { "items":  [ { "param": "2026-03-27", "clicks0": 0, "redirects0": 0, "clicks1": 1, ... }, ... ],
    ///   "totals": { "totalClicks0": 0, "totalRedirects0": 0, "totalClicks1": 1, ... } }
    /// </code>
    /// where the numeric suffix is the zero-based position of the link in the requested <c>ids</c>.
    /// Values may arrive as either JSON numbers or strings. The link IDs themselves are not present in
    /// the payload — <see cref="LinkStatSeries.LinkId"/> is filled afterwards by the service from the request.
    /// </para>
    /// </summary>
    internal sealed class LinkStatsResultConverter : JsonConverter<LinkStatsResult>
    {
        public override LinkStatsResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var result = new LinkStatsResult();

            // PHP serialises an empty result set as `[]`; anything else that is not an object is corrupt.
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0)
                return result;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("link/getStats data is neither a statistics object nor an empty array.");

            var indices = new SortedSet<int>();

            // Per-period rows: param + per-index clicks/redirects.
            var periods = new List<(string Param, Dictionary<int, int> Clicks, Dictionary<int, int> Redirects)>();
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                        continue;

                    string param = item.TryGetProperty("param", out var p) ? (p.GetString() ?? string.Empty) : string.Empty;
                    var clicks = new Dictionary<int, int>();
                    var redirects = new Dictionary<int, int>();

                    foreach (var prop in item.EnumerateObject())
                    {
                        if (TryColumn(prop.Name, "clicks", out int ci)) { clicks[ci] = ToInt(prop.Value); indices.Add(ci); }
                        else if (TryColumn(prop.Name, "redirects", out int ri)) { redirects[ri] = ToInt(prop.Value); indices.Add(ri); }
                    }

                    periods.Add((param, clicks, redirects));
                }
            }

            // Totals: per-index totalClicks/totalRedirects.
            var totalClicks = new Dictionary<int, int>();
            var totalRedirects = new Dictionary<int, int>();
            if (root.TryGetProperty("totals", out var totals) && totals.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in totals.EnumerateObject())
                {
                    if (TryColumn(prop.Name, "totalClicks", out int ci)) { totalClicks[ci] = ToInt(prop.Value); indices.Add(ci); }
                    else if (TryColumn(prop.Name, "totalRedirects", out int ri)) { totalRedirects[ri] = ToInt(prop.Value); indices.Add(ri); }
                }
            }

            var series = new List<LinkStatSeries>(indices.Count);
            foreach (var idx in indices)
            {
                var points = new List<LinkStatPoint>(periods.Count);
                foreach (var period in periods)
                {
                    points.Add(new LinkStatPoint
                    {
                        Param = period.Param,
                        Clicks = period.Clicks.TryGetValue(idx, out var c) ? c : 0,
                        Redirects = period.Redirects.TryGetValue(idx, out var r) ? r : 0
                    });
                }

                series.Add(new LinkStatSeries
                {
                    Index = idx,
                    TotalClicks = totalClicks.TryGetValue(idx, out var tc) ? tc : 0,
                    TotalRedirects = totalRedirects.TryGetValue(idx, out var tr) ? tr : 0,
                    Points = points
                });
            }

            result.Links = series;
            return result;
        }

        public override void Write(Utf8JsonWriter writer, LinkStatsResult value, JsonSerializerOptions options)
            => throw new NotSupportedException($"{nameof(LinkStatsResultConverter)} does not support writing.");

        /// <summary>
        /// Matches a column name of the form <c>{prefix}{N}</c> (case-insensitive) where <c>N</c> is a
        /// non-negative integer suffix, returning that suffix.
        /// </summary>
        private static bool TryColumn(string name, string prefix, out int index)
        {
            index = 0;
            if (name.Length <= prefix.Length || !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            return int.TryParse(
                name.Substring(prefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out index);
        }

        /// <summary>
        /// Counters arrive as numbers or numeric strings; <c>null</c>/<c>""</c> (PHP "no value") count as 0.
        /// A non-numeric or out-of-range value is corruption and must not silently become 0.
        /// </summary>
        private static int ToInt(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var n))
                        return n;
                    break;
                case JsonValueKind.String:
                    var text = element.GetString();
                    if (string.IsNullOrEmpty(text))
                        return 0;
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                        return s;
                    break;
                case JsonValueKind.Null:
                    return 0;
            }

            throw new JsonException("link/getStats counter is not a 32-bit integer.");
        }
    }
}
