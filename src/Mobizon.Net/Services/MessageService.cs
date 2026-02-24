using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class MessageService : IMessageService
    {
        private readonly MobizonApiClient _apiClient;

        public MessageService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<SendSmsResult>> SendSmsMessageAsync(
            SendSmsMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["recipient"] = request.Recipient,
                ["text"] = request.Text
            };

            if (request.From != null)
                parameters["from"] = request.From;

            if (request.Validity.HasValue)
                parameters["params[validity]"] = request.Validity.Value.ToString();

            return _apiClient.SendAsync<SendSmsResult>(
                HttpMethod.Post, "message", "sendsmsmessage", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<SmsStatusResult>>> GetSmsStatusAsync(
            int[] ids,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ids[i].ToString();
            }

            return _apiClient.SendAsync<IReadOnlyList<SmsStatusResult>>(
                HttpMethod.Post, "message", "getsmsstatus", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<MessageInfo>>> ListAsync(
            MessageListRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.From != null)
                    parameters["criteria[from]"] = request.From;

                if (request.Status.HasValue)
                    parameters["criteria[status]"] = request.Status.Value.ToString();

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = request.Pagination.CurrentPage.ToString();
                    parameters["pagination[pageSize]"] = request.Pagination.PageSize.ToString();
                }

                if (request.Sort != null)
                {
                    parameters[$"sort[{request.Sort.Field}]"] = request.Sort.Direction.ToString();
                }
            }

            return _apiClient.SendAsync<IReadOnlyList<MessageInfo>>(
                HttpMethod.Post, "message", "list", parameters, cancellationToken);
        }
    }
}
