using Mobizon.Net.ApiCapture;
using Xunit;

namespace Mobizon.Net.Tests.Tools
{
    public class ApiCaptureTests
    {
        [Fact]
        public void BuildUrl_Composes_Service_Path_With_Auth_Query()
        {
            var url = RawMobizonApi.BuildUrl("https://api.mobizon.kz/", "v1", "KEY", "user", "getOwnBalance");
            Assert.Equal(
                "https://api.mobizon.kz/service/user/getOwnBalance?output=json&api=v1&apiKey=KEY",
                url);
        }
    }
}
