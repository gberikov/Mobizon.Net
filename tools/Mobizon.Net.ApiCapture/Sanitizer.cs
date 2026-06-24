using System.Text.RegularExpressions;

namespace Mobizon.Net.ApiCapture
{
    public static class Sanitizer
    {
        private static readonly Regex PhoneLike = new Regex(@"\d{7,}", RegexOptions.Compiled);
        private static readonly Regex Balance = new Regex("\"balance\"\\s*:\\s*\"[^\"]*\"", RegexOptions.Compiled);

        public static string Scrub(string json)
        {
            var s = PhoneLike.Replace(json, "7000000XXXX");
            s = Balance.Replace(s, "\"balance\":\"0.0000\"");
            return s;
        }
    }
}
