using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the parameters required to add recipients to an existing campaign.
    /// Only one recipient type (<see cref="Recipients"/>, <see cref="RecipientContacts"/>,
    /// or <see cref="RecipientGroups"/>) may be specified per request.
    /// </summary>
    public class AddRecipientsRequest
    {
        /// <summary>
        /// Gets or sets the ID of the campaign to which the recipients will be added.
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the list of recipient phone numbers to add to the campaign.
        /// Numbers must be in international format. Up to 500 numbers per request.
        /// For template campaigns each element may also carry placeholder values
        /// via <see cref="RecipientEntry"/>.
        /// </summary>
        public IReadOnlyList<RecipientEntry>? Recipients { get; set; }

        /// <summary>
        /// Gets or sets the list of contact-book card IDs (or <c>cardId:fieldKey</c> pairs)
        /// to add to the campaign.
        /// </summary>
        public IReadOnlyList<string>? RecipientContacts { get; set; }

        /// <summary>
        /// Gets or sets the list of contact-book group IDs whose members will be added
        /// to the campaign (asynchronous background task).
        /// </summary>
        public IReadOnlyList<string>? RecipientGroups { get; set; }

        /// <summary>
        /// Gets or sets additional processing options for this add-recipients request.
        /// </summary>
        public AddRecipientsParameters? Parameters { get; set; }
    }

    /// <summary>
    /// Represents a single recipient entry, optionally including placeholder values
    /// for template campaigns.
    /// </summary>
    public class RecipientEntry
    {
        private string _recipient = string.Empty;

        /// <summary>
        /// Gets or sets the recipient phone number in international format (e.g. <c>79991234567</c>).
        /// Only digits are retained; any non-digit characters (including a leading <c>+</c>) are stripped automatically.
        /// </summary>
        public string Recipient
        {
            get => _recipient;
            set => _recipient = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        }

        /// <summary>
        /// Gets or sets a dictionary of placeholder name → value pairs used in template campaigns.
        /// Keys must match placeholder names in the campaign text (without curly braces).
        /// </summary>
        public IDictionary<string, string>? Placeholders { get; set; }
    }

    /// <summary>
    /// Additional options for the AddRecipients API call.
    /// </summary>
    public class AddRecipientsParameters
    {
        /// <summary>
        /// Gets or sets whether to remove all previously added recipients before adding the new ones.
        /// <c>0</c> — append (default); <c>1</c> — replace.
        /// </summary>
        public int? Replace { get; set; }

        /// <summary>
        /// Gets or sets the behaviour when placeholder values are missing in a template campaign:
        /// <c>1</c> — keep placeholders as-is (default);
        /// <c>2</c> — remove placeholders from the text;
        /// <c>3</c> — reject the message with an error.
        /// </summary>
        public int? PlaceholdersFlag { get; set; }

        /// <summary>
        /// Gets or sets the encoding of the uploaded recipients file.
        /// Allowed values: <c>KOI8-R</c>, <c>CP866</c>, <c>WINDOWS-1252</c>, <c>WINDOWS-1251</c>,
        /// <c>UTF-8</c>, <c>ASCII</c>, <c>ISO-8859-1</c>, <c>UCS-2</c>. Default: <c>UTF-8</c>.
        /// </summary>
        public string? RecipientsFileEncoding { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the first line (header row) of the recipients file.
        /// <c>0</c> — start from line 1 (default); <c>1</c> — skip line 1.
        /// Always <c>1</c> for template campaigns.
        /// </summary>
        public int? RecipientsFileSkipHeader { get; set; }

        /// <summary>
        /// Gets or sets the column delimiter used in the recipients CSV file. Default: <c>,</c>.
        /// </summary>
        public string? RecipientsFileDelimiter { get; set; }

        /// <summary>
        /// Gets or sets the text enclosure character used in the recipients CSV file. Default: <c>'</c>.
        /// </summary>
        public string? RecipientsFileEnclosure { get; set; }
    }
}
