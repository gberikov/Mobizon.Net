using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Mobizon.Net.ApiCapture;
using Xunit;

namespace Mobizon.Net.Tests.Tools
{
    public class ApiCaptureTests
    {
        [Fact]
        public void BuildUrl_Composes_Service_Path_Without_Credentials()
        {
            var url = RawMobizonApi.BuildUrl("https://api.mobizon.kz/", "v1", "user", "getOwnBalance");

            Assert.Equal("https://api.mobizon.kz/service/user/getOwnBalance?output=json&api=v1", url);
            Assert.DoesNotContain("apiKey", url);
        }

        [Theory]
        [InlineData("http://api.mobizon.kz")]
        [InlineData("api.mobizon.kz")]
        public void Constructor_RejectsEndpointThatIsNotAbsoluteHttps(string apiUrl)
        {
            using var http = new HttpClient();
            Assert.Throws<ArgumentException>(() => new RawMobizonApi(http, apiUrl, "KEY"));
        }

        [Fact]
        public void Constructor_RequiresApiKey()
        {
            using var http = new HttpClient();
            Assert.Throws<ArgumentException>(() => new RawMobizonApi(http, "https://api.mobizon.kz", " "));
        }

        // ── Scrubbing ────────────────────────────────────────────────────────

        private static JsonElement Scrubbed(string json) =>
            JsonDocument.Parse(Sanitizer.Scrub(json)).RootElement;

        [Fact]
        public void Scrub_Masks_Phone_Fields()
        {
            var result = Scrubbed("{\"to\":\"77011234567\",\"mobile\":{\"value\":\"77019998877\"}}");

            Assert.DoesNotContain("77011234567", result.ToString());
            Assert.DoesNotContain("77019998877", result.ToString());
            Assert.Equal("70000000000", result.GetProperty("to").GetString());
            Assert.Equal("70000000000", result.GetProperty("mobile").GetProperty("value").GetString());
        }

        [Fact]
        public void Scrub_Masks_Balance_Value_And_Keeps_Currency()
        {
            var result = Scrubbed("{\"balance\":\"4043.0656\",\"currency\":\"KZT\"}");

            Assert.Equal("0.0000", result.GetProperty("balance").GetString());
            Assert.Equal("KZT", result.GetProperty("currency").GetString());
        }

        [Fact]
        public void Scrub_Masks_LatinPersonalData_And_OneTimeCodes()
        {
            // The regex-only scrubber left all of these in place: they are Latin, short, or both.
            var result = Scrubbed(
                "{\"email\":\"alice@example.com\",\"surname\":\"Smith\",\"text\":\"Your OTP is 8421\"}");
            var text = result.ToString();

            Assert.DoesNotContain("alice@example.com", text);
            Assert.DoesNotContain("Smith", text);
            Assert.DoesNotContain("8421", text);
        }

        [Fact]
        public void Scrub_Masks_Email_Inside_A_Field_It_Does_Not_Know()
        {
            var result = Scrubbed("{\"someFutureField\":\"write to alice@example.com today\"}");

            Assert.Equal("write to user@example.com today", result.GetProperty("someFutureField").GetString());
        }

        [Fact]
        public void Scrub_Replaces_NonLatin_Free_Text_Keeping_The_Separator()
        {
            var result = Scrubbed("{\"someFutureField\":\"\u0418\u0432\u0430\u043d\u043e\u0432 Smith\"}");

            Assert.Equal("REDACTED Smith", result.GetProperty("someFutureField").GetString());
        }

        [Fact]
        public void Scrub_Preserves_Numeric_Json()
        {
            // The regex scrubber rewrote this to the unquoted token 7000000XXXX, producing invalid JSON.
            var result = Scrubbed("{\"id\":12345678,\"nested\":{\"count\":7}}");

            Assert.Equal(JsonValueKind.Number, result.GetProperty("id").ValueKind);
            Assert.Equal(12345678, result.GetProperty("id").GetInt32());
            Assert.Equal(7, result.GetProperty("nested").GetProperty("count").GetInt32());
        }

        [Fact]
        public void Scrub_Keeps_The_Json_Type_Of_A_Numeric_Sensitive_Field()
        {
            var result = Scrubbed("{\"to\":77011234567}");

            Assert.Equal(JsonValueKind.Number, result.GetProperty("to").ValueKind);
            Assert.Equal(70000000000L, result.GetProperty("to").GetInt64());
        }

        [Fact]
        public void Scrub_Preserves_Envelope_Shape_And_Nulls()
        {
            var result = Scrubbed("{\"code\":0,\"data\":{\"items\":[],\"deletedTs\":null},\"message\":\"\"}");

            Assert.Equal(0, result.GetProperty("code").GetInt32());
            Assert.Equal(JsonValueKind.Array, result.GetProperty("data").GetProperty("items").ValueKind);
            Assert.Equal(JsonValueKind.Null, result.GetProperty("data").GetProperty("deletedTs").ValueKind);
        }

        [Fact]
        public void Scrub_Rejects_Input_That_Is_Not_Json()
        {
            Assert.ThrowsAny<JsonException>(() => Sanitizer.Scrub("<html>502 Bad Gateway</html>"));
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
