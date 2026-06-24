using System;
using System.IO;
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

        [Fact]
        public void Scrub_Masks_Phone_Like_Digit_Runs()
        {
            var outp = Sanitizer.Scrub("{\"to\":\"77011234567\"}");
            Assert.DoesNotContain("77011234567", outp);
            Assert.Contains("7000000XXXX", outp);
        }

        [Fact]
        public void Scrub_Masks_Balance_Value()
        {
            var outp = Sanitizer.Scrub("{\"balance\":\"4043.0656\",\"currency\":\"KZT\"}");
            Assert.DoesNotContain("4043.0656", outp);
            Assert.Contains("\"currency\":\"KZT\"", outp);
        }

        [Fact]
        public void SanitizeRunner_Scrubs_Files_From_In_To_Out()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "mbz-sanitize-" + Guid.NewGuid().ToString("N"));
            var inDir = Path.Combine(baseDir, "in");
            var outDir = Path.Combine(baseDir, "out");
            Directory.CreateDirectory(inDir);
            File.WriteAllText(Path.Combine(inDir, "user.getOwnBalance.json"),
                "{\"code\":0,\"data\":{\"balance\":\"4043.0656\",\"currency\":\"KZT\"},\"message\":\"\"}");

            var count = SanitizeRunner.Run(inDir, outDir);

            Assert.Equal(1, count);
            var outText = File.ReadAllText(Path.Combine(outDir, "user.getOwnBalance.json"));
            Assert.DoesNotContain("4043.0656", outText);
            Assert.Contains("\"currency\":\"KZT\"", outText);

            Directory.Delete(baseDir, recursive: true);
        }
    }
}
