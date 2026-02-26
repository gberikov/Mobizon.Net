using System;
using Mobizon.Contracts.Models.Common;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class MobizonClientOptionsTests
    {
        [Fact]
        public void Validate_WithValidOptions_DoesNotThrow()
        {
            var options = new MobizonClientOptions
            {
                ApiKey = "test-key",
                ApiUrl = "https://api.mobizon.kz"
            };

            options.Validate();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithInvalidApiKey_ThrowsArgumentException(string? apiKey)
        {
            var options = new MobizonClientOptions
            {
                ApiUrl = "https://api.mobizon.kz"
            };
            if (apiKey != null) options.ApiKey = apiKey;

            Assert.Throws<ArgumentException>(() => options.Validate());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithInvalidApiUrl_ThrowsArgumentException(string? apiUrl)
        {
            var options = new MobizonClientOptions
            {
                ApiKey = "test-key"
            };
            if (apiUrl != null) options.ApiUrl = apiUrl;

            Assert.Throws<ArgumentException>(() => options.Validate());
        }

        [Fact]
        public void Defaults_AreCorrect()
        {
            var options = new MobizonClientOptions();

            Assert.Equal("v1", options.ApiVersion);
            Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
        }

        [Fact]
        public void ApiKey_SetNull_ThrowsArgumentNullException()
        {
            var options = new MobizonClientOptions();
            Assert.Throws<ArgumentNullException>(() => options.ApiKey = null!);
        }

        [Fact]
        public void ApiUrl_SetNull_ThrowsArgumentNullException()
        {
            var options = new MobizonClientOptions();
            Assert.Throws<ArgumentNullException>(() => options.ApiUrl = null!);
        }
    }
}
