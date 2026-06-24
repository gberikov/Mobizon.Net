using Xunit;

namespace Mobizon.Net.Tests
{
    public class FixturesTests
    {
        [Fact]
        public void Load_Returns_Fixture_Content()
        {
            var json = Fixtures.Load("user.getOwnBalance.json");
            Assert.Contains("\"currency\"", json);
        }
    }
}
