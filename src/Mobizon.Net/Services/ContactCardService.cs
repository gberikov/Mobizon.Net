using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class ContactCardService
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
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            return _apiClient.SendAsync<ContactCardData>(
                HttpMethod.Post, ModuleName, "get", parameters, cancellationToken);
        }

        public Task<MobizonResponse<string>> CreateAsync(
            CreateContactCardRequest request,
            CancellationToken cancellationToken = default)
        {
            var fields = BuildCardFields(request.Title, request.Name, request.Surname,
                request.MobileValue, request.MobileType, request.Email,
                request.Viber, request.WhatsApp, request.Landline,
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
                request.Viber, request.WhatsApp, request.Landline,
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
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            for (var i = 0; i < groupIds.Count; i++)
                parameters[$"groupIds[{i}]"] = groupIds[i];

            return _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "setgroups", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<ContactGroupRef>>> GetGroupsAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            return _apiClient.SendAsync<IReadOnlyList<ContactGroupRef>>(
                HttpMethod.Post, ModuleName, "getgroups", parameters, cancellationToken);
        }

        public Task<MobizonResponse<bool>> RemoveAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = id };
            return _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken);
        }

        private static Dictionary<string, string> BuildCardFields(
            string? title, string? name, string? surname,
            string? mobileValue, ContactType? mobileType,
            string? email, string? viber, string? whatsapp, string? landline,
            string? skype, string? telegram,
            DateTime? birthDate, string? gender, string? companyName, string? companyUrl,
            string? info)
        {
            return new Dictionary<string, string>
            {
                ["data[title]"]           = title       ?? string.Empty,
                ["data[name]"]            = name        ?? string.Empty,
                ["data[surname]"]         = surname     ?? string.Empty,
                ["data[mobile][value]"]   = mobileValue ?? string.Empty,
                ["data[mobile][type]"]    = mobileType?.ToString().ToUpperInvariant() ?? string.Empty,
                ["data[email]"]           = email       ?? string.Empty,
                ["data[viber]"]           = viber       ?? string.Empty,
                ["data[whatsapp]"]        = whatsapp    ?? string.Empty,
                ["data[landline]"]        = landline    ?? string.Empty,
                ["data[skype]"]           = skype       ?? string.Empty,
                ["data[telegram]"]        = telegram    ?? string.Empty,
                ["data[birth_date]"]      = birthDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                ["data[gender]"]          = gender      ?? string.Empty,
                ["data[company_name]"]    = companyName ?? string.Empty,
                ["data[company_url]"]     = companyUrl  ?? string.Empty,
                ["data[info]"]            = info        ?? string.Empty,
            };
        }
    }
}
