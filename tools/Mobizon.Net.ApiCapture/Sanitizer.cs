using System.Text.RegularExpressions;

namespace Mobizon.Net.ApiCapture
{
    public static class Sanitizer
    {
        private static readonly Regex PhoneLike = new Regex(@"\d{7,}", RegexOptions.Compiled);
        private static readonly Regex Balance = new Regex("\"balance\"\\s*:\\s*\"[^\"]*\"", RegexOptions.Compiled);

        // Captured payloads carry real account free text (alphaname descriptions, contact
        // names, stop-list comments). Fixtures are committed to a public repo and kept
        // Latin-only, so Cyrillic runs are replaced here rather than hand-edited after
        // every capture. Inner spaces/hyphens are part of the run; a trailing one is not,
        // so "<cyrillic> Smith" becomes "REDACTED Smith", not "REDACTEDSmith".
        private static readonly Regex CyrillicRun =
            new Regex("[\u0400-\u04FF]+(?:[ \\-][\u0400-\u04FF]+)*", RegexOptions.Compiled);

        public static string Scrub(string json)
        {
            var s = PhoneLike.Replace(json, "7000000XXXX");
            s = Balance.Replace(s, "\"balance\":\"0.0000\"");
            s = CyrillicRun.Replace(s, "REDACTED");
            return s;
        }
    }
}
