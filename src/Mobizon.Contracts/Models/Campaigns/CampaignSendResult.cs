namespace Mobizon.Contracts
{
    /// <summary>
    /// Result of scheduling a campaign for sending via <c>campaign/send</c>.
    /// </summary>
    public class CampaignSendResult
    {
        /// <summary>
        /// True when the API queued the send as a background task (response code 100).
        /// When true, <see cref="Id"/> is the task id trackable via <c>TaskQueue/GetStatus</c>.
        /// </summary>
        public bool IsQueued { get; set; }

        /// <summary>
        /// The background task id when <see cref="IsQueued"/> is true; otherwise the
        /// synchronous send result returned by the API.
        /// </summary>
        public long Id { get; set; }
    }
}
