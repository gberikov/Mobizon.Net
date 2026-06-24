using System;
using System.IO;

namespace Mobizon.Net.Webhooks.Tests
{
    /// <summary>Loads webhook payload fixtures and shares the test secret used to sign them.</summary>
    internal static class Payloads
    {
        /// <summary>The secret key the fixture <c>sign</c> values were computed with.</summary>
        public const string Secret = "secret123";

        public const string SmsDeliveryReport = "sms-delivery-report.json";
        public const string FormSubmission = "form-submission.json";
        public const string FormContactConfirmation = "form-contact-confirmation.json";
        public const string FormContactUnsubscribe = "form-contact-unsubscribe.json";

        public static string Load(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Payloads", fileName);
            return File.ReadAllText(path);
        }
    }
}
