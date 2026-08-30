using System;
using System.Globalization;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Single place that renders .NET values into the string form the Mobizon API expects.
    /// Everything is culture-invariant: the API speaks Gregorian dates and ASCII digits only.
    /// </summary>
    internal static class ApiFormat
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateFormat = "yyyy-MM-dd";

        public static string DateTime(DateTime value) =>
            value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        public static string Date(DateTime value) =>
            value.ToString(DateFormat, CultureInfo.InvariantCulture);

        public static string Int(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Bool(bool value) => value ? "1" : "0";

        public static string Sort(SortDirection direction) =>
            direction == SortDirection.Descending ? "DESC" : "ASC";

        /// <summary>Wire form of a contact's gender. Empty string clears the field.</summary>
        public static string Gender(Gender? value)
        {
            switch (value)
            {
                case Contracts.Gender.Male:   return "male";
                case Contracts.Gender.Female: return "female";
                default:                      return string.Empty;
            }
        }
    }
}
