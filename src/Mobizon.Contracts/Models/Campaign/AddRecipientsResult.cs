using System.Collections.Generic;
using System.Linq;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the result returned by the <c>campaign/addRecipients</c> API method.
    /// <para>
    /// When recipients were loaded from a phone-number list or contact cards, <see cref="Entries"/>
    /// is populated with per-recipient results.
    /// When recipients were loaded from a contact group or file (asynchronous), <see cref="TaskId"/>
    /// contains the background task ID and the API response code will be
    /// <see cref="MobizonResponseCode.BackgroundTask"/> (100).
    /// </para>
    /// </summary>
    public class AddRecipientsResult
    {
        /// <summary>
        /// Gets or sets the ID of the background task created to process the add-recipients operation.
        /// Populated only for asynchronous loads (groups / file upload).
        /// </summary>
        public int? TaskId { get; set; }

        /// <summary>
        /// Gets or sets the per-recipient processing results.
        /// Populated only for synchronous loads (phone numbers / contact cards).
        /// </summary>
        public IReadOnlyList<AddRecipientEntry>? Entries { get; set; }

        /// <summary>
        /// Merges entries from <paramref name="other"/> into this result by appending them
        /// to <see cref="Entries"/>. Used when batching large recipient lists.
        /// </summary>
        public void MergeEntries(AddRecipientsResult other)
        {
            if (other.Entries == null || other.Entries.Count == 0)
                return;

            Entries = Entries == null
                ? other.Entries
                : (IReadOnlyList<AddRecipientEntry>)Entries.Concat(other.Entries).ToList();
        }
    }

    /// <summary>
    /// Represents the result for a single recipient processed by <c>campaign/addRecipients</c>.
    /// </summary>
    public class AddRecipientEntry
    {
        /// <summary>
        /// Gets or sets the phone number that was added to the campaign.
        /// May be <see langword="null"/> when a contact card or contact was not found.
        /// </summary>
        public string? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the result code for this recipient:
        /// <list type="bullet">
        ///   <item><term>0</term><description>Successfully added.</description></item>
        ///   <item><term>1</term><description>Phone number absent or empty in the data.</description></item>
        ///   <item><term>2</term><description>Phone number not found in the data (malformed).</description></item>
        ///   <item><term>3</term><description>Number does not match international format.</description></item>
        ///   <item><term>4</term><description>Duplicate — number already added to the campaign.</description></item>
        ///   <item><term>5</term><description>Number is in a stop-list.</description></item>
        ///   <item><term>6</term><description>Sending to the destination country is restricted by account settings.</description></item>
        ///   <item><term>7</term><description>Cannot determine the operator or destination country.</description></item>
        ///   <item><term>8</term><description>No route available for this operator; contact support.</description></item>
        ///   <item><term>20</term><description>Missing placeholder values (when placeholdersFlag = 3).</description></item>
        ///   <item><term>30</term><description>Contact card not found in the address book.</description></item>
        ///   <item><term>31</term><description>Contact card has no mobile number.</description></item>
        ///   <item><term>32</term><description>Contact not found in the address book.</description></item>
        ///   <item><term>51</term><description>Cannot create a short link at this time.</description></item>
        ///   <item><term>99</term><description>System error while adding the recipient.</description></item>
        /// </list>
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message created for this recipient.
        /// Available when the recipient was successfully added (<see cref="Code"/> == 0).
        /// </summary>
        public int? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the original phone number value as submitted by the caller
        /// (populated when a phone number was passed).
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets the contact ID that was passed in <c>recipientContacts</c>
        /// (populated when a contact reference was used).
        /// </summary>
        public int? Contact { get; set; }
    }
}

