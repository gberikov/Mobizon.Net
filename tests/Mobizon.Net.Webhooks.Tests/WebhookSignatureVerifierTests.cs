using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class WebhookSignatureVerifierTests
    {
        private readonly WebhookSignatureVerifier _verifier = new WebhookSignatureVerifier();

        // Ground-truth vector computed independently:
        // SHA1("99|2|2026-02-01 00:00:00|topsecret") = a80090cf6edc623e7f4f68a36aa30de403e7766a
        private const long VectorEventId = 99;
        private const int VectorAttempt = 2;
        private const string VectorTs = "2026-02-01 00:00:00";
        private const string VectorSecret = "topsecret";
        private const string VectorSign = "a80090cf6edc623e7f4f68a36aa30de403e7766a";

        [Fact]
        public void Verify_KnownVector_ReturnsTrue()
        {
            Assert.True(_verifier.Verify(VectorEventId, VectorAttempt, VectorTs, VectorSign, VectorSecret));
        }

        [Fact]
        public void Verify_TamperedSignature_ReturnsFalse()
        {
            var tampered = VectorSign.Substring(0, VectorSign.Length - 1) + "0";
            Assert.False(_verifier.Verify(VectorEventId, VectorAttempt, VectorTs, tampered, VectorSecret));
        }

        [Fact]
        public void Verify_WrongSecret_ReturnsFalse()
        {
            Assert.False(_verifier.Verify(VectorEventId, VectorAttempt, VectorTs, VectorSign, "not-the-secret"));
        }

        [Theory]
        [InlineData("", VectorTs, VectorSecret)]   // missing sign
        [InlineData(VectorSign, "", VectorSecret)] // missing eventCreateTs
        [InlineData(VectorSign, VectorTs, "")]     // missing secret
        public void Verify_MissingInputs_FailsClosed(string sign, string ts, string secret)
        {
            Assert.False(_verifier.Verify(VectorEventId, VectorAttempt, ts, sign, secret));
        }

        [Fact]
        public void Verify_EventOverload_DelegatesToRawValues()
        {
            var evt = new SmsDeliveryReportEvent
            {
                EventId = VectorEventId,
                Attempt = VectorAttempt,
                EventCreateTsRaw = VectorTs,
                Sign = VectorSign
            };

            Assert.True(_verifier.Verify(evt, VectorSecret));
        }

        [Fact]
        public void Verify_NullEvent_ReturnsFalse()
        {
            Assert.False(_verifier.Verify(null!, VectorSecret));
        }
    }
}
