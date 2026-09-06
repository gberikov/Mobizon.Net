using Mobizon.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents the result returned by the <c>campaign/addRecipients</c> API method.
    /// <para>
    /// When recipients were loaded from a phone-number list or contact cards, <see cref="Entries"/>
    /// is populated with per-recipient results.
    /// When recipients were loaded from a contact group or file (asynchronous), <see cref="TaskId"/>
    /// contains the background task ID, <see cref="IsQueued"/> is <see langword="true"/> and the API
    /// response code was <see cref="MobizonResponseCode.BackgroundTask"/> (100). A queued load has
    /// <b>not</b> completed yet: poll <c>TaskQueue/GetStatus</c> before sending the campaign.
    /// </para>
    /// </summary>
    public class AddRecipientsResult
    {
        /// <summary>
        /// Gets or sets the ID of the background task created to process the add-recipients operation.
        /// Populated only for asynchronous loads (groups / file upload).
        /// </summary>
        public long? TaskId { get; set; }

        /// <summary>
        /// <see langword="true"/> when the API accepted the load as a background task (<see cref="TaskId"/> is set)
        /// rather than processing the recipients synchronously.
        /// </summary>
        [JsonIgnore]
        public bool IsQueued => TaskId.HasValue;

        /// <summary>
        /// Gets or sets the per-recipient processing results, in the order the recipients were submitted.
        /// Populated only for synchronous loads (phone numbers / contact cards).
        /// </summary>
        public IReadOnlyList<AddRecipientEntry>? Entries { get; set; }

        /// <summary>
        /// Gets or sets the top-level outcome of the operation, derived from the API response code(s).
        /// For a queued (asynchronous) load this is <see cref="AddRecipientsOutcome.AllAdded"/> because the API
        /// only acknowledged the request; check <see cref="IsQueued"/>. For multi-batch sends the outcome is
        /// aggregated across batches: any batch accepted plus any batch rejected yields
        /// <see cref="AddRecipientsOutcome.PartiallyAdded"/>.
        /// </summary>
        public AddRecipientsOutcome Outcome { get; set; }

        /// <summary>
        /// Merges entries from <paramref name="other"/> into this result by appending them
        /// to <see cref="Entries"/>.
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
    /// Progress of a multi-batch <c>campaign/addRecipients</c> call that failed part-way. Attached to the thrown
    /// exception (any type, including <see cref="OperationCanceledException"/>) and retrieved with
    /// <see cref="FromException"/>. Batches before the failing one were confirmed by the API; the failing batch's
    /// outcome is <b>unknown</b> when <see cref="PendingCount"/> is non-zero (the request may have been applied
    /// even though no response arrived), so do not blindly resend the whole list.
    /// </summary>
    public sealed class AddRecipientsProgress
    {
        /// <summary>Key under which the progress is stored in <see cref="Exception.Data"/>.</summary>
        public const string DataKey = "Mobizon.AddRecipientsProgress";

        /// <summary>Creates a progress record.</summary>
        public AddRecipientsProgress(AddRecipientsResult confirmed, int confirmedCount, int pendingCount)
        {
            Confirmed = confirmed ?? throw new ArgumentNullException(nameof(confirmed));
            ConfirmedCount = confirmedCount;
            PendingCount = pendingCount;
        }

        /// <summary>Aggregated result of the batches the API confirmed before the failure.</summary>
        public AddRecipientsResult Confirmed { get; }

        /// <summary>
        /// Number of submitted recipients (phone entries first, then contact IDs, in submission order) covered by
        /// <see cref="Confirmed"/>. The recipient at this index is the first one not confirmed.
        /// </summary>
        public int ConfirmedCount { get; }

        /// <summary>
        /// Size of the batch that was in flight when the failure happened, starting at index
        /// <see cref="ConfirmedCount"/>; its outcome is unknown. <c>0</c> when the failure (e.g. cancellation)
        /// occurred between batches and nothing was in flight.
        /// </summary>
        public int PendingCount { get; }

        /// <summary>
        /// Returns the progress attached to an exception thrown by a multi-batch add-recipients call, or
        /// <see langword="null"/> when the call failed before anything was sent or was a single-batch call.
        /// </summary>
        public static AddRecipientsProgress? FromException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            return exception.Data.Contains(DataKey) ? exception.Data[DataKey] as AddRecipientsProgress : null;
        }

        /// <summary>Stores this progress in <paramref name="exception"/>'s <see cref="Exception.Data"/>.</summary>
        public void AttachTo(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            exception.Data[DataKey] = this;
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
        public long? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the original phone number value as submitted by the caller
        /// (populated when a phone number was passed).
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets the contact ID that was passed in <c>recipientContacts</c>
        /// (populated when a contact reference was used).
        /// </summary>
        public long? Contact { get; set; }

        /// <summary>
        /// Gets or sets the recipient type returned by the API (e.g. <c>"number"</c>, <c>"contact"</c>).
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
