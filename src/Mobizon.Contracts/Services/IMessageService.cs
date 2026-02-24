using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for sending and querying SMS messages via the Mobizon API.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Sends a single SMS message to the specified recipient.
        /// </summary>
        /// <param name="request">The message details including recipient, text, and optional sender name.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="SendSmsResult"/> with the
        /// campaign ID, message ID, and initial delivery status.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        /// <example>
        /// <code>
        /// var response = await client.Messages.SendSmsMessageAsync(new SendSmsMessageRequest
        /// {
        ///     Recipient = "79991234567",
        ///     Text = "Hello from Mobizon!",
        ///     From = "MyCompany"
        /// });
        ///
        /// if (response.Code == MobizonResponseCode.Success)
        ///     Console.WriteLine($"Message ID: {response.Data.MessageId}");
        /// </code>
        /// </example>
        Task<MobizonResponse<SendSmsResult>> SendSmsMessageAsync(
            SendSmsMessageRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the current delivery status of one or more SMS messages by their IDs.
        /// </summary>
        /// <param name="ids">An array of message IDs to query.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a list of <see cref="SmsStatusResult"/> entries,
        /// one for each requested message ID.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<IReadOnlyList<SmsStatusResult>>> GetSmsStatusAsync(
            int[] ids,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a paginated list of messages, optionally filtered by sender or status.
        /// </summary>
        /// <param name="request">Optional filter, pagination, and sort criteria. Pass <see langword="null"/> to use API defaults.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a list of <see cref="MessageInfo"/> items.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<IReadOnlyList<MessageInfo>>> ListAsync(
            MessageListRequest? request = null,
            CancellationToken cancellationToken = default);
    }
}
