using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class FormEventParseTests
    {
        private readonly WebhookParser _parser = new WebhookParser();

        [Fact]
        public void Parse_FormSubmission_AllFields()
        {
            var evt = Assert.IsType<FormSubmissionEvent>(_parser.Parse(Payloads.Load(Payloads.FormSubmission)));
            var data = evt.Data;

            Assert.Equal(846, data.FormId);
            Assert.Equal(3680, data.SubmissionId);
            Assert.Equal(2, data.Items.Count);

            var name = data.Items[0];
            Assert.Equal(12303, name.SubmissionDataId);
            Assert.Equal(3744, name.FieldId);
            Assert.Equal("TEXT_STRING", name.FieldType);
            Assert.Equal("Name", name.FieldName);
            Assert.Equal("Ivan", name.Value);
            Assert.False(name.ConfirmationRequired);

            var mobile = data.Items[1];
            Assert.Equal("MOBILE", mobile.FieldType);
            Assert.True(mobile.ConfirmationRequired);
        }

        [Fact]
        public void Parse_FormContactConfirmation_ItemWithConfirmationTs()
        {
            var evt = Assert.IsType<FormContactConfirmationEvent>(_parser.Parse(Payloads.Load(Payloads.FormContactConfirmation)));
            var data = evt.Data;

            Assert.Equal(846, data.FormId);
            Assert.Equal(3680, data.SubmissionId);
            Assert.Equal(12305, data.Item.SubmissionDataId);
            Assert.Equal("MOBILE", data.Item.FieldType);
            Assert.NotNull(data.Item.ConfirmationTs);
        }

        [Fact]
        public void Parse_FormContactUnsubscribe_EmptyConfirmationTs_IsNull()
        {
            var evt = Assert.IsType<FormContactUnsubscribeEvent>(_parser.Parse(Payloads.Load(Payloads.FormContactUnsubscribe)));
            var data = evt.Data;

            Assert.Equal(846, data.FormId);
            Assert.NotNull(data.UnsubscribeTs);
            Assert.Single(data.Items);

            var item = data.Items[0];
            Assert.Equal(3675, item.SubmissionId);
            Assert.Equal("EMAIL", item.FieldType);
            Assert.Equal("test@mobizon.com", item.Value);
            Assert.Null(item.ConfirmationTs); // empty string -> null
        }
    }
}
