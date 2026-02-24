using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.ContactCard;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class ContactCardService : IContactCardService
    {
        private const string ModuleName = "contactcard";
        private readonly MobizonApiClient _apiClient;

        public ContactCardService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<ContactCardListResponse>> ListAsync(
            ContactCardListRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Criteria != null)
                    for (var i = 0; i < request.Criteria.Count; i++)
                    {
                        var c = request.Criteria[i];
                        parameters[$"criteria[{i}][field]"] = c.Field;
                        parameters[$"criteria[{i}][operator]"] = c.Operator;
                        parameters[$"criteria[{i}][value]"] = c.Value;
                    }

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = request.Pagination.CurrentPage.ToString();
                    parameters["pagination[pageSize]"] = request.Pagination.PageSize.ToString();
                }

                if (request.Sort != null)
                    parameters[$"sort[{request.Sort.Field}]"] = request.Sort.Direction.ToString();
            }

            return _apiClient.SendAsync<ContactCardListResponse>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<ContactCardData>> GetAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<ContactCardData>(
                ModuleName, "get",
                new { id },
                cancellationToken);
        }

        public Task<MobizonResponse<string>> CreateAsync(
            CreateContactCardRequest request,
            CancellationToken cancellationToken = default)
        {
            var fields = BuildCardFields(request.Title, request.Name, request.Surname,
                request.MobileValue, request.MobileType, request.Email,
                request.Viber, request.Whatsapp, request.Landline,
                request.Skype, request.Telegram, request.BirthDate,
                request.Gender, request.CompanyName, request.CompanyUrl, request.Info);

            return _apiClient.SendMultipartAsync<string>(
                ModuleName, "create", fields,
                request.Photo, request.PhotoFileName,
                cancellationToken);
        }

        public Task<MobizonResponse<bool>> UpdateAsync(
            UpdateContactCardRequest request,
            CancellationToken cancellationToken = default)
        {
            var fields = BuildCardFields(request.Title, request.Name, request.Surname,
                request.MobileValue, request.MobileType, request.Email,
                request.Viber, request.Whatsapp, request.Landline,
                request.Skype, request.Telegram, request.BirthDate,
                request.Gender, request.CompanyName, request.CompanyUrl, request.Info);

            fields["id"] = request.Id;

            return _apiClient.SendMultipartAsync<bool>(
                ModuleName, "update", fields,
                request.Photo, request.PhotoFileName,
                cancellationToken);
        }

        public Task<MobizonResponse<bool>> SetGroupsAsync(
            string id,
            IReadOnlyList<string> groupIds,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<bool>(
                ModuleName, "setgroups",
                new { id, groupIds },
                cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<ContactGroupRef>>> GetGroupsAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<IReadOnlyList<ContactGroupRef>>(
                ModuleName, "getgroups",
                new { id },
                cancellationToken);
        }

        private static Dictionary<string, string> BuildCardFields(
            string? title, string? name, string? surname,
            string? mobileValue, string? mobileType,
            string? email, string? viber, string? whatsapp, string? landline,
            string? skype, string? telegram,
            string? birthDate, string? gender, string? companyName, string? companyUrl,
            string? info)
        {
            return new Dictionary<string, string>
            {
                ["data[title]"]           = title       ?? string.Empty,
                ["data[name]"]            = name        ?? string.Empty,
                ["data[surname]"]         = surname     ?? string.Empty,
                ["data[mobile][value]"]   = mobileValue ?? string.Empty,
                ["data[mobile][type]"]    = mobileType  ?? string.Empty,
                ["data[email]"]           = email       ?? string.Empty,
                ["data[viber]"]           = viber       ?? string.Empty,
                ["data[whatsapp]"]        = whatsapp    ?? string.Empty,
                ["data[landline]"]        = landline    ?? string.Empty,
                ["data[skype]"]           = skype       ?? string.Empty,
                ["data[telegram]"]        = telegram    ?? string.Empty,
                ["data[birth_date]"]      = birthDate   ?? string.Empty,
                ["data[gender]"]          = gender      ?? string.Empty,
                ["data[company_name]"]    = companyName ?? string.Empty,
                ["data[company_url]"]     = companyUrl  ?? string.Empty,
                ["data[info]"]            = info        ?? string.Empty,
            };
        }
    }
}
