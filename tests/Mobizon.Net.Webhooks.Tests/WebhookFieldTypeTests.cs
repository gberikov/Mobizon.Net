using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class WebhookFieldTypeTests
    {
        [Theory]
        [InlineData("TEXT_STRING", WebhookFieldType.TextString)]
        [InlineData("EMAIL", WebhookFieldType.Email)]
        [InlineData("MOBILE", WebhookFieldType.Mobile)]
        public void FieldTypeKind_MapsDocumentedValues(string raw, WebhookFieldType expected)
        {
            Assert.Equal(expected, new WebhookFieldItem { FieldType = raw }.FieldTypeKind);
        }

        [Theory]
        [InlineData("DATE")]
        [InlineData("email")]
        [InlineData("")]
        public void FieldTypeKind_UnknownValue_IsNull_AndRawIsKept(string raw)
        {
            var item = new WebhookFieldItem { FieldType = raw };
            Assert.Null(item.FieldTypeKind);
            Assert.Equal(raw, item.FieldType);
        }

        [Fact]
        public void Parse_FormSubmission_ExposesTypedFieldKinds()
        {
            var evt = Assert.IsType<FormSubmissionEvent>(new WebhookParser().Parse(Payloads.Load(Payloads.FormSubmission)));

            Assert.Equal(WebhookFieldType.TextString, evt.Data.Items[0].FieldTypeKind);
            Assert.Equal(WebhookFieldType.Mobile, evt.Data.Items[1].FieldTypeKind);
        }

        [Fact]
        public void Parse_FormSubmission_UnknownFieldType_StillParsesEvent()
        {
            var body = Payloads.Load(Payloads.FormSubmission).Replace("\"TEXT_STRING\"", "\"FUTURE_TYPE\"");

            var evt = Assert.IsType<FormSubmissionEvent>(new WebhookParser().Parse(body));

            Assert.Null(evt.Data.Items[0].FieldTypeKind);
            Assert.Equal("FUTURE_TYPE", evt.Data.Items[0].FieldType);
        }
    }
}
