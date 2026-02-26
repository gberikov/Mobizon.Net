using Mobizon.Contracts.Models.Messages;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class SendSmsMessageRequestTests
    {
        [Theory]
        [InlineData("77001234567", "77001234567")]
        [InlineData("+77001234567", "77001234567")]
        [InlineData("+7 (700) 123-45-67", "77001234567")]
        [InlineData("7-700-123-45-67", "77001234567")]
        [InlineData("  +7 700 123 45 67  ", "77001234567")]
        public void Recipient_StripsNonDigits(string input, string expected)
        {
            var request = new SendSmsMessageRequest { Recipient = input };
            Assert.Equal(expected, request.Recipient);
        }

        [Fact]
        public void Recipient_WhenNull_ReturnsEmptyString()
        {
            var request = new SendSmsMessageRequest { Recipient = null! };
            Assert.Equal(string.Empty, request.Recipient);
        }

        [Fact]
        public void Recipient_WhenEmpty_ReturnsEmptyString()
        {
            var request = new SendSmsMessageRequest { Recipient = "" };
            Assert.Equal(string.Empty, request.Recipient);
        }
    }
}

