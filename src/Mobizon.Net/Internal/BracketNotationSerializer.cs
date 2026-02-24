using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Mobizon.Net.Internal
{
    internal static class BracketNotationSerializer
    {
        public static Dictionary<string, string> Serialize(IDictionary<string, object?> obj)
        {
            var result = new Dictionary<string, string>();
            SerializeInternal(obj, null, result);
            return result;
        }

        private static void SerializeInternal(
            IDictionary<string, object?> obj,
            string? prefix,
            Dictionary<string, string> result)
        {
            foreach (var kvp in obj)
            {
                var key = prefix == null ? kvp.Key : $"{prefix}[{kvp.Key}]";
                SerializeValue(key, kvp.Value, result);
            }
        }

        private static void SerializeValue(
            string key,
            object? value,
            Dictionary<string, string> result)
        {
            if (value == null)
                return;

            if (value is IDictionary<string, object?> dict)
            {
                SerializeInternal(dict, key, result);
                return;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    var itemKey = $"{key}[{index}]";
                    SerializeValue(itemKey, item, result);
                    index++;
                }
                return;
            }

            result[key] = Convert.ToString(value, CultureInfo.InvariantCulture)
                          ?? string.Empty;
        }

        private static class Convert
        {
            public static string? ToString(object value, CultureInfo culture)
            {
                return System.Convert.ToString(value, culture);
            }
        }
    }
}
