using System;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;
using Xunit;

namespace Mobizon.Net.Tests.Extensions
{
    public class MobizonResilienceTests
    {
        [Fact]
        public void AddMobizonResilience_WithDefaults_ReturnsBuilder()
        {
            var services = new ServiceCollection();
            var builder = services.AddHttpClient("MobizonApi");

            var result = builder.AddMobizonResilience();

            Assert.NotNull(result);
        }

        [Fact]
        public void AddMobizonResilience_WithCustomOptions_ReturnsBuilder()
        {
            var services = new ServiceCollection();
            var builder = services.AddHttpClient("MobizonApi");

            var result = builder.AddMobizonResilience(options =>
            {
                options.RetryCount = 5;
                options.RetryBaseDelay = TimeSpan.FromSeconds(2);
                options.CircuitBreakerFailureThreshold = 10;
                options.CircuitBreakerDuration = TimeSpan.FromMinutes(1);
            });

            Assert.NotNull(result);
        }

        [Fact]
        public void MobizonResilienceOptions_Defaults_AreCorrect()
        {
            var options = new MobizonResilienceOptions();

            Assert.Equal(3, options.RetryCount);
            Assert.Equal(TimeSpan.FromSeconds(1), options.RetryBaseDelay);
            Assert.Equal(5, options.CircuitBreakerFailureThreshold);
            Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreakerDuration);
        }

        [Fact]
        public void MobizonResilienceOptions_CustomValues_AreApplied()
        {
            var options = new MobizonResilienceOptions
            {
                RetryCount = 7,
                RetryBaseDelay = TimeSpan.FromMilliseconds(500),
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerDuration = TimeSpan.FromMinutes(2)
            };

            Assert.Equal(7, options.RetryCount);
            Assert.Equal(TimeSpan.FromMilliseconds(500), options.RetryBaseDelay);
            Assert.Equal(3, options.CircuitBreakerFailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(2), options.CircuitBreakerDuration);
        }
    }
}
